using Newtonsoft.Json;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    public class LootTableItemViewModel: DBObjectViewModelBase
    {
        public LootTableItemViewModel()
        {
            Weight = 1;
            Resref = "resref";
            SpawnRule = string.Empty;
            IsActive = true;
            MaxQuantity = 1;

            TrackProperty(this, x => x.Weight);
            TrackProperty(this, x => x.Resref);
            TrackProperty(this, x => x.SpawnRule);
            TrackProperty(this, x => x.IsActive);
            TrackProperty(this, x => x.MaxQuantity);
        }
        private int _weight;
        
        [JsonProperty(nameof(Weight))]
        public int Weight
        {
            get => _weight;
            set
            {
                _weight = value;
                NotifyOfPropertyChange(() => Weight);
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

        private string _spawnRule;

        [JsonProperty(nameof(SpawnRule))]
        public string SpawnRule
        {
            get => _spawnRule;
            set
            {
                _spawnRule = value;
                NotifyOfPropertyChange(() => SpawnRule);
            }
        }

        private bool _isActive;

        [JsonProperty(nameof(IsActive))]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                NotifyOfPropertyChange(() => IsActive);
            }
        }

        private int _maxQuantity;

        [JsonProperty(nameof(MaxQuantity))]
        public int MaxQuantity
        {
            get => _maxQuantity;
            set
            {
                _maxQuantity = value;
                NotifyOfPropertyChange(() => MaxQuantity);
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get => Resref;
            set
            {
                _resref = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => Resref);
            }
        }
    }
}
