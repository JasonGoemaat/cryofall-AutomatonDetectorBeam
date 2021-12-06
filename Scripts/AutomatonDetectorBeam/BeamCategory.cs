using AtomicTorch.CBND.GameApi.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using AtomicTorch.CBND.GameApi.Scripting;
using CryoFall.Automaton.ClientSettings;
using CryoFall.Automaton.ClientSettings.Options;
using System.Windows.Media;
using AtomicTorch.CBND.GameApi.Data.World;
using AtomicTorch.CBND.CoreMod.Helpers.Client;
using AtomicTorch.GameEngine.Common.Primitives;

namespace AutomatonDetectorBeam.Scripts.AutomatonDetectorBeam
{
    public class BeamCategory
    {
        private bool isEnabled;

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }

            set
            {
                isEnabled = value;
                if (this.beam != null)
                {
                    this.beam.IsEnabled = value;
                }
            }
        }

        public string Name { get; set; }

        public List<IProtoEntity> ObjectList { get; set; }

        public List<IProtoEntity> EnabledObjectList { get; set; }

        private Color beamColor = Color.FromArgb(255, 255, 255, 255);

        public Color BeamColor { 
            get
            {
                return beamColor;
            }

            set
            {
                this.beamColor = value;
                if (this.beam != null)
                {
                    this.beam.Color = value;
                }
            }
        }

        private double hue;

        public double Hue
        {
            get
            {
                return hue;
            }
            set
            {
                hue = value;
                var hsl = new HslColor(hue * 359.9, 1, 0.5, 1);
                BeamColor = hsl.ToRgb();
                // Api.Logger.Important($"H {hue}, S 1.0, L 0.5, A 1.0 is RGBA {BeamColor.R}, {BeamColor.G}, {BeamColor.B}, {BeamColor.A}");
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
                if (this.beam != null)
                {
                    this.beam.Width = value;
                }
            }
        }

        private DetectorBeam beam;

        public BeamCategory(string name, double hue = 0.0, bool isEnabled = true)
        {
            this.Name = name;
            this.Hue = hue;
            this.IsEnabled = isEnabled;

            this.ObjectList = new List<IProtoEntity>();
        }

        public void Add<T>() where T : class, IProtoEntity
        {
            this.ObjectList.AddRange(Api.FindProtoEntities<T>());
        }

        public void PrepareEnabledOption(List<IOption> options, SettingsFeature settingsFeature)
        {
            options.Add(new OptionCheckBox(
                parentSettings: settingsFeature,
                id: $"{Name}Enabled",
                label: $"{Name} Enabled",
                defaultValue: true,
                valueChangedCallback: value => IsEnabled = value));
        }

        public void PrepareOptions(List<IOption> options, SettingsFeature settingsFeature)
        {
            options.Add(new OptionSeparator());

            options.Add(new OptionInformationText(Name));

            options.Add(new OptionSlider(
                parentSettings: settingsFeature,
                id: $"{Name}Hue",
                label: $"Hue",
                defaultValue: hue,
                valueChangedCallback: value => Hue = value));

            options.Add(new OptionSlider(
                parentSettings: settingsFeature,
                id: $"{Name}BeamWidth",
                label: $"Beam Width",
                defaultValue: 1.0,
                valueChangedCallback: value => BeamWidth = value));

            options.Add(new OptionEntityList(
                parentSettings: settingsFeature,
                id: $"{Name}ObjectList",
                entityList: ObjectList.OrderBy(entity => entity.Id),
                defaultEnabledList: new List<string>(),
                onEnabledListChanged: enabledList => EnabledObjectList = enabledList));
        }

        public void Execute()
        {
            // create beam if we don't have one
            var character = ClientCurrentCharacterHelper.Character;
            if (this.beam == null)
            {
                if (character != null)
                {
                    var sceneObject = character.ClientSceneObject;
                    this.beam = sceneObject.AddComponent<DetectorBeam>();
                    this.beam.Color = this.BeamColor;
                    this.beam.Width = this.BeamWidth;
                    this.beam.IsEnabled = this.IsEnabled;
                    //Api.Logger.Important($"BeamCategory.Execute() - created beam");
                    //Api.Logger.Important($"BeamCategory.Execute() - created beam (INFO)");
                }
            }
            
            // just return if beam creation failed
            if (this.beam == null)
            {
                return;
            }

            // find nearest valid target
            var target = Api.Client.World.GetStaticWorldObjectsOfProto<IProtoStaticWorldObject>()
                    .Where(IsValidObject)
                    .OrderBy(o => character.Position.DistanceTo(o.TilePosition.ToVector2D()))
                    .FirstOrDefault();

            // clear and return if there are no valid targets
            if (target == null)
            {
                this.beam.TargetPosition = null;
                return;
            }

            // some magic to better center the location, default location for space debris is like 3 tiles sw
            var sum = target.OccupiedTilePositions.Aggregate(Vector2D.Zero, (a, b) => a + b.ToVector2D());
            int count = target.OccupiedTilePositions.Count();
            var targetPosition = new Vector2D(sum.X / count, sum.Y / count) + new Vector2D(0.5, 0.5);
            var a = targetPosition.ToVector2Ushort();
            var b = (beam.TargetPosition ?? Vector2D.Zero).ToVector2Ushort();
            if (a.X != b.X || a.Y != b.Y)
            {
                Api.Logger.Important($"BeamCategory {Name} changing position from {b.X}, {b.Y} to {a.X}, {a.Y}: really {targetPosition.X}, {targetPosition.Y}");
                this.beam.TargetPosition = targetPosition;
            }
        }

        /// <summary>
        /// When stopping, clear the target position to hide beam
        /// </summary>
        public void Stop()
        {
            if (this.beam != null)
            {
                this.beam.TargetPosition = null;
            }
        }

        private bool IsValidObject(IStaticWorldObject staticWorldObject)
        {
            return EnabledObjectList.Contains(staticWorldObject.ProtoStaticWorldObject);
        }

        public void Report()
        {
            if (beam == null)
            {
                //Api.Logger.Important($"{Name}: Beam is null");
            }
            else
            {
                if (beam.IsEnabled)
                {
                    //Api.Logger.Important($"{Name}: Beam is enabled, hooray!");
                } else
                {
                    //Api.Logger.Important($"{Name}: Beam is created, but not enabled, boo!");
                }
            }
        }
    }
}