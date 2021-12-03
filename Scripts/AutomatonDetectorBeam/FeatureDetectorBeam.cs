using AtomicTorch.CBND.CoreMod.Helpers.Client;
using AtomicTorch.CBND.CoreMod.StaticObjects.Loot;
using AtomicTorch.CBND.CoreMod.StaticObjects.Minerals;
using AtomicTorch.CBND.CoreMod.StaticObjects.Misc.Events;
using AtomicTorch.CBND.CoreMod.StaticObjects.Vegetation.SmallGatherables;
using AtomicTorch.CBND.GameApi.Data;
using AtomicTorch.CBND.GameApi.Data.World;
using AtomicTorch.CBND.GameApi.Scripting;
using AtomicTorch.CBND.GameApi.Scripting.ClientComponents;
using AtomicTorch.GameEngine.Common.Primitives;
using AutomatonDetectorBeam.Scripts.AutomatonDetectorBeam;
using CryoFall.Automaton.ClientSettings;
using CryoFall.Automaton.ClientSettings.Options;
using CryoFall.Automaton.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace AutomatonDetectorBeam.Scripts
{
    public class FeatureDetectorBeam : ProtoFeature<FeatureDetectorBeam>
    {
        private FeatureDetectorBeam() { }

        public override string Name => "Detector Beam";

        public override string Description => "Point a beam from player's feet to the nearest object of the selected types";

        public List<IProtoEntity> ObjectList { get; set; }

        public List<IProtoEntity> EnabledObjectList { get; set; }

        public Color BeamColor { get; set; }

        private double hue;

        public double Hue {
            get
            {
                return hue;
            }
            set
            {
                hue = value;
                var hsl = new HslColor(hue * 359.9, 1, 0.5, 1);
                BeamColor = hsl.ToRgb();
                Api.Logger.Important($"H {hue}, S 1.0, L 0.5, A 1.0 is RGBA {BeamColor.R}, {BeamColor.G}, {BeamColor.B}, {BeamColor.A}");
            }
        }

        private double beamWidth;

        public double BeamWidth
        {
            get 
            {
                return beamWidth;
            }
            set
            {
                beamWidth = value;
            }
        }

        protected override void PrepareFeature(List<IProtoEntity> entityList, List<IProtoEntity> requiredItemList)
        {
            ObjectList = new List<IProtoEntity>();
            ObjectList.AddRange(Api.FindProtoEntities<ObjectMineralPragmiumSource>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectMineralPragmiumNode>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSpaceDebris>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectMeteorite>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectMineralCoal>());

            ObjectList.AddRange(Api.FindProtoEntities<ObjectLootCrateFood>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectLootCrateHightech>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectLootCrateIndustrial>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectLootCrateMedical>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectLootCrateMilitary>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectLootCrateSupply>());

            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallHerbPurple>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallHerbRed>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallHerbGreen>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallHerbBlue>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallMushroomPennyBun>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallMushroomPink>());
            ObjectList.AddRange(Api.FindProtoEntities<ObjectSmallMushroomRust>());
        }


        public override void PrepareOptions(SettingsFeature settingsFeature)
        {
            AddOptionIsEnabled(settingsFeature);

            Options.Add(new OptionSlider(
                parentSettings: settingsFeature,
                id: "Hue",
                label: "Hue",
                defaultValue: 0.0,
                valueChangedCallback: value => Hue = value));

            Options.Add(new OptionSlider(
                parentSettings: settingsFeature,
                id: "BeamWidth",
                label: "Beam Width",
                defaultValue: 1.0,
                valueChangedCallback: value => BeamWidth = value));

            Options.Add(new OptionSeparator());

            Options.Add(new OptionEntityList(
                parentSettings: settingsFeature,
                id: "DetectorBeamObjectList",
                entityList: ObjectList.OrderBy(entity => entity.Id),
                defaultEnabledList: new List<string>(),
                onEnabledListChanged: enabledList => EnabledObjectList = enabledList));
        }

        private bool IsValidObject(IStaticWorldObject staticWorldObject)
        {
            return EnabledObjectList.Contains(staticWorldObject.ProtoStaticWorldObject);
        }

        /// <summary>
        /// Called by client component on specific time interval.
        /// </summary>
        public override void Execute()
        {
            if (!(IsEnabled && CheckPrecondition()))
            {
                return;
            }

            var component = DetectorBeam.GetInstance();
            if (component == null)
            {
                return;
            }

            var target =
                Api.Client.World.GetStaticWorldObjectsOfProto<IProtoStaticWorldObject>()
                    .Where(IsValidObject)
                    .OrderBy(o => CurrentCharacter.Position.DistanceTo(o.TilePosition.ToVector2D()))
                    .FirstOrDefault();

            if (target == null)
            {
                DetectorBeam.TargetPosition = null;
                return;
            }

            var player = ClientCurrentCharacterHelper.Character;

            // some magic to better center the location, default location for space debris is like 3 tiles sw
            var sum = target.OccupiedTilePositions.Aggregate(Vector2D.Zero, (a, b) => a + b.ToVector2D());
            var targetPosition = new Vector2D(sum.X / target.OccupiedTilePositions.Count(), sum.Y / target.OccupiedTilePositions.Count());
            targetPosition += new Vector2D(0.5, 0.5); // center of a tile

            DetectorBeam.TargetPosition = targetPosition;
        }


        /// <summary>
        /// Called by client component every tick.
        /// </summary>
        public override void Update(double deltaTime)
        {
        }


        /// <summary>
        /// Stop everything.
        /// </summary>
        public override void Stop()
        {
            DetectorBeam.TargetPosition = null;
        }

        /// <summary>
        /// Setup any of subscriptions
        /// </summary>
        public override void SetupSubscriptions(ClientComponent parentComponent)
        {
            base.SetupSubscriptions(parentComponent);
        }

        /// <summary>
        /// Init on component enabled.
        /// </summary>
        public override void Start(ClientComponent parentComponent)
        {
            base.Start(parentComponent);
        }
    }
}
