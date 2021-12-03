using AtomicTorch.CBND.CoreMod.Characters;
using AtomicTorch.CBND.CoreMod.Helpers.Client;
using AtomicTorch.CBND.CoreMod.Systems.Weapons;
using AtomicTorch.CBND.GameApi.Data.Characters;
using AtomicTorch.CBND.GameApi.Data.Physics;
using AtomicTorch.CBND.GameApi.Resources;
using AtomicTorch.CBND.GameApi.Scripting;
using AtomicTorch.CBND.GameApi.Scripting.ClientComponents;
using AtomicTorch.CBND.GameApi.ServicesClient.Components;
using AtomicTorch.CBND.GameApi.ServicesClient.Rendering;
using AtomicTorch.GameEngine.Common.Primitives;
using CryoFall.Automaton.Features;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace AutomatonDetectorBeam.Scripts.AutomatonDetectorBeam
{
    public class DetectorBeam : ClientComponent
    {
        private Color DefaultColor = Color.FromArgb(255, 255, 0, 0);

        private ComponentBeam componentBeam;

        public static Vector2D? TargetPosition = null;

        private static DetectorBeam Instance;

        public static DetectorBeam GetInstance()
        {
            if (Instance == null)
            {
                var character = ClientCurrentCharacterHelper.Character;
                if (character != null)
                {

                    var sceneObject = character.ClientSceneObject;
                    Instance = sceneObject.AddComponent<DetectorBeam>();
                }
            }

            return Instance;
        }

        public DetectorBeam() : base(isLateUpdateEnabled: true)
        {
            // Api.Logger.Important($"TestComponent: Component being created()");
        }

        protected override void OnDisable()
        {
            this.componentBeam.Destroy();
            this.componentBeam = null;
        }

        protected override void OnEnable()
        {
            this.componentBeam = this.SceneObject.AddComponent<ComponentBeam>(isEnabled: false);
        }

        public override void LateUpdate(double deltaTime)
        {
            var player = ClientCurrentCharacterHelper.Character;
            if (player == null)
            {
                return;
            }

            var characterPublicState = player.GetPublicState<ICharacterPublicState>();
            if (characterPublicState.IsDead || characterPublicState is PlayerCharacterPublicState { IsOnline: false })
            {
                this.componentBeam.IsEnabled = false;
                return;
            }

            var clientState = player.GetClientState<BaseCharacterClientState>();
            if (clientState.SkeletonRenderer is null || !clientState.SkeletonRenderer.IsReady)
            {
                this.componentBeam.IsEnabled = false;
                return;
            }

            if (TargetPosition == null)
            {
                // no target, disable beam
                this.componentBeam.IsEnabled = false;
                return;
            }

            CastLine(player,
                     customTargetPosition: TargetPosition,
                     rangeMax: 100,
                     toPosition: out var toPosition);

            var sourcePosition = Vector2D.Zero;
            var beamEndPosition = toPosition;

            // fade-out if too close to prevent visual glitches
            var distance = (beamEndPosition - sourcePosition).Length;
            double beamOpacity = 1.0;

            var color = FeatureDetectorBeam.Instance.BeamColor;
            sourcePosition = player.Position;

            this.componentBeam.IsEnabled = true;
            this.componentBeam.Refresh(
                sourcePosition: sourcePosition,
                sourcePositionOffset: 0.1,
                targetPosition: beamEndPosition,
                beamWidth: FeatureDetectorBeam.Instance.BeamWidth,
                beamColor: color,
                spotColor: color,
                beamOpacity: beamOpacity,
                // determine whether the beam should end with a bright spot (when pointing on something)
                hasTarget: false);
        }

        private static void CastLine(
                    ICharacter character,
                    Vector2D? customTargetPosition,
                    double rangeMax,
                    out Vector2D toPosition)
        {
            var clientState = character.GetClientState<BaseCharacterClientState>();
            var characterRotationAngleRad = clientState.LastInterpolatedRotationAngleRad.HasValue
                                                ? clientState.LastInterpolatedRotationAngleRad.Value
                                                : ((IProtoCharacterCore)character.ProtoCharacter)
                                                .SharedGetRotationAngleRad(character);

            WeaponSystem.SharedCastLine(character,
                                        isMeleeWeapon: false,
                                        rangeMax,
                                        characterRotationAngleRad,
                                        customTargetPosition: customTargetPosition,
                                        fireSpreadAngleOffsetDeg: 0,
                                        collisionGroup: null,
                                        toPosition: out toPosition,
                                        tempLineTestResults: out var tempLineTestResults,
                                        sendDebugEvent: false);
        }

        private class ComponentBeam : ClientComponent
        {
            private const double SpotScale = 4.0;

            private static readonly EffectResource BeamEffectResource
                = new("AdditiveColorEffect");

            private static readonly TextureResource TextureResourceBeam
                = new("FX/WeaponTraces/BeamLaser.png");

            private static readonly TextureResource TextureResourceSpot
                = new("FX/Special/LaserSightSpot.png");

            private readonly RenderingMaterial renderingMaterial
                = RenderingMaterial.Create(BeamEffectResource);

            private IComponentSpriteRenderer spriteRendererLine;

            public void Refresh(
                Vector2D sourcePosition,
                double sourcePositionOffset,
                Vector2D targetPosition,
                double beamWidth,
                Color beamColor,
                Color spotColor,
                double beamOpacity,
                bool hasTarget)
            {
                beamColor = Color.FromArgb((byte)(beamOpacity * beamColor.A),
                                           beamColor.R,
                                           beamColor.G,
                                           beamColor.B);

                this.renderingMaterial.EffectParameters.Set("ColorAdditive", beamColor);
                this.spriteRendererLine.Color = beamColor;

                var deltaPos = targetPosition - sourcePosition;

                ComponentWeaponTrace.CalculateAngleAndDirection(deltaPos,
                                                                out var angleRad,
                                                                out var normalizedRay);
                sourcePosition += normalizedRay * sourcePositionOffset;
                deltaPos = targetPosition - sourcePosition;

                var sceneObjectPosition = this.spriteRendererLine.SceneObject.Position;
                this.spriteRendererLine.PositionOffset = sourcePosition - sceneObjectPosition;
                this.spriteRendererLine.RotationAngleRad = (float)angleRad;
                this.spriteRendererLine.Size = (ScriptingConstants.TileSizeVirtualPixels * deltaPos.Length,
                                                ScriptingConstants.TileSizeVirtualPixels * beamWidth);
            }

            protected override void OnDisable()
            {
                this.spriteRendererLine.Destroy();
            }

            protected override void OnEnable()
            {
                var sceneObject = this.SceneObject;
                this.spriteRendererLine = Api.Client.Rendering.CreateSpriteRenderer(
                    sceneObject,
                    textureResource: TextureResourceBeam,
                    spritePivotPoint: (0, 0.5),
                    drawOrder: DrawOrder.Light);
                this.spriteRendererLine.RenderingMaterial = this.renderingMaterial;
                this.spriteRendererLine.BlendMode = BlendMode.AdditivePremultiplied;
            }
        }
    }
}
