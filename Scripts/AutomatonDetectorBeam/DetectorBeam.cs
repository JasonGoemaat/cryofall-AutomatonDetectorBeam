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
        public Color Color { get; set; }

        public double Width { get; set; }

        private ComponentBeam componentBeam;

        public Vector2D? TargetPosition { get; set; }

        public DetectorBeam() : base(isLateUpdateEnabled: true)
        {
            Api.Logger.Important($"DetectorBeam: Component being created");
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
                if (this.componentBeam.IsEnabled)
                {
                    this.componentBeam.IsEnabled = false;
                    Api.Logger.Info("Disabling Beam");
                }
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

            sourcePosition = player.Position;

            if (!this.componentBeam.IsEnabled && TargetPosition != null)
            {
                var tp = TargetPosition ?? Vector2D.Zero;
                Api.Logger.Warning($"Enabling Beam to {tp.X}, {tp.Y}");
            }
            this.componentBeam.IsEnabled = true;

            this.componentBeam.Refresh(
                sourcePosition: sourcePosition,
                sourcePositionOffset: 0.1,
                targetPosition: beamEndPosition,
                color: this.Color,
                width: this.Width);
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
            private static readonly EffectResource BeamEffectResource
                = new("AdditiveColorEffect");

            private static readonly TextureResource TextureResourceBeam
                = new("FX/WeaponTraces/BeamLaser.png");

            private readonly RenderingMaterial renderingMaterial
                = RenderingMaterial.Create(BeamEffectResource);

            private IComponentSpriteRenderer spriteRendererLine;

            public void Refresh(
                Vector2D sourcePosition,
                double sourcePositionOffset,
                Vector2D targetPosition,
                Color color,
                double width)
            {
                this.renderingMaterial.EffectParameters.Set("ColorAdditive", color);
                this.spriteRendererLine.Color = color;

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
                                                ScriptingConstants.TileSizeVirtualPixels * width);
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
