using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder("LootTable")]
    public class LootTableViewModel: DBObjectViewModelBase
    {
        public LootTableViewModel()
        {
            LootTableItems = new ObservableCollection<LootTableItemViewModel>();

            TrackProperty(this, x => x.LootTableID);
            TrackProperty(this, x => x.Name);
        }
        
        [JsonProperty(nameof(LootTableItems))]
        public ObservableCollection<LootTableItemViewModel> LootTableItems { get; set; }

        private int _lootTableID;
        [JsonProperty(nameof(LootTableID))]
        public int LootTableID
        {
            get => _lootTableID;
            set
            {
                _lootTableID = value;
                NotifyOfPropertyChange(() => LootTableID);
                IsDirty = true;
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
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => DisplayName);
                IsDirty = true;
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => Name);
                IsDirty = true;
            }
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();

            foreach (var item in LootTableItems)
            {
                item.DiscardChanges();
            }
        }

        public override void SetTrackedValues()
        {
            base.SetTrackedValues();

            foreach (var item in LootTableItems)
            {
                item.SetTrackedValues();
            }
        }
    }
}
