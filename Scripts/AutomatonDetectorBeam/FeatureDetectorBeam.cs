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

        BeamCategory bcSpecial;
        BeamCategory bcCrates;
        BeamCategory bcHerbs;
        BeamCategory bcOther;

        List<BeamCategory> beamCategories;

        protected override void PrepareFeature(List<IProtoEntity> entityList, List<IProtoEntity> requiredItemList)
        {
            beamCategories = new List<BeamCategory>();

            bcHerbs = new BeamCategory("Herbs", hue: 0.33); // green
            bcHerbs.Add<ObjectSmallHerbGreen>();
            bcHerbs.Add<ObjectSmallHerbRed>();
            bcHerbs.Add<ObjectSmallHerbPurple>();
            bcHerbs.Add<ObjectSmallHerbBlue>();
            beamCategories.Add(bcHerbs);

            bcSpecial = new BeamCategory("Special", hue: 0.66); // blue
            bcSpecial.Add<ObjectMineralPragmiumSource>();
            bcSpecial.Add<ObjectMineralPragmiumNode>();
            bcSpecial.Add<ObjectSpaceDebris>();
            bcSpecial.Add<ObjectMeteorite>();
            beamCategories.Add(bcSpecial);

            bcCrates = new BeamCategory("Crates", hue: 0.0); // red
            bcCrates.Add<ObjectLootCrateFood>();
            bcCrates.Add<ObjectLootCrateHightech>();
            bcCrates.Add<ObjectLootCrateIndustrial>();
            bcCrates.Add<ObjectLootCrateMedical>();
            bcCrates.Add<ObjectLootCrateMilitary>();
            bcCrates.Add<ObjectLootCrateSupply>();
            beamCategories.Add(bcCrates);

            bcOther = new BeamCategory("Other", hue: 0.15); // yellow
            bcOther.Add<ObjectSmallMushroomPennyBun>();
            bcOther.Add<ObjectSmallMushroomRust>();
            bcOther.Add<ObjectSmallMushroomPink>();
            beamCategories.Add(bcOther);
        }

        public override void PrepareOptions(SettingsFeature settingsFeature)
        {
            AddOptionIsEnabled(settingsFeature);

            Options.Add(new OptionSeparator());

            foreach (var bc in beamCategories)
            {
                bc.PrepareEnabledOption(Options, settingsFeature);
            }

            foreach (var bc in beamCategories)
            {
                bc.PrepareOptions(Options, settingsFeature);
            }
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


            foreach (var bc in beamCategories)
            {
                bc.Execute();
            }
        }

        /// <summary>
        /// Called by client component every tick.
        /// </summary>
        public override void Update(double deltaTime)
        {
            timeSinceLastUpdate += deltaTime;
            if (timeSinceLastUpdate > 5)
            {
                Api.Logger.Important($"Update (Important)");
                foreach (var bc in beamCategories)
                {
                    bc.Report();
                }

                timeSinceLastUpdate = 0;
            }
        }

        double timeSinceLastUpdate = 0;

        /// <summary>
        /// Stop everything.
        /// </summary>
        public override void Stop()
        {
            foreach (var bc in beamCategories)
            {
                bc.Stop();
            }
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
