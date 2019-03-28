using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(Plant))]
    public class PlantViewModel : DBObjectViewModelBase
    {
        public PlantViewModel()
        {
            Name = "New Plant";
            BaseTicks = 600;
            Resref = "resref";
            WaterTicks = 300;
            Level = 1;
            SeedResref = "seed_resref";


            TrackProperty(this, x => Name);
            TrackProperty(this, x => BaseTicks);
            TrackProperty(this, x => Resref);
            TrackProperty(this, x => WaterTicks);
            TrackProperty(this, x => Level);
            TrackProperty(this, x => SeedResref);
        }

        private string _displayName;

        public override string DisplayName
        {
            get => _displayName;
            set
            {
                _name = value;
                _displayName = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        private string _name;
        [JsonProperty(nameof(Name))]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _displayName = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        private int _baseTicks;
        [JsonProperty(nameof(BaseTicks))]
        public int BaseTicks
        {
            get => _baseTicks;
            set
            {
                _baseTicks = value;
                NotifyOfPropertyChange(() => BaseTicks);
            }
        }

        private string _resref;
        [JsonProperty(nameof(Resref))]
        public string Resref
        {
            get => _resref;
            set
            {
                _resref = value;
                NotifyOfPropertyChange(() => Resref);
            }
        }

        private int _waterTicks;
        [JsonProperty(nameof(WaterTicks))]
        public int WaterTicks
        {
            get => _waterTicks;
            set
            {
                _waterTicks = value;
                NotifyOfPropertyChange(() => WaterTicks);
            }
        }
        private int _level;
        [JsonProperty(nameof(Level))]
        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                NotifyOfPropertyChange(() => Level);
            }
        }
        private string _seedResref;
        [JsonProperty(nameof(SeedResref))]
        public string SeedResref
        {
            get => _seedResref;
            set
            {
                _seedResref = value;
                NotifyOfPropertyChange(() => SeedResref);
            }
        }

    }
}
