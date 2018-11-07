using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;
using System.Collections.ObjectModel;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(LootTable))]
    public class LootTableViewModel : DBObjectViewModelBase
    {
        public LootTableViewModel()
        {
            LootTableItems = new ObservableCollection<LootTableItemViewModel>();
            Name = "New Loot Table";

            TrackProperty(this, x => x.Name);
            TrackProperty(this, x => x.ID);
            TrackProperty(this, x => x.LootTableItems);
        }

        private ObservableCollection<LootTableItemViewModel> _lootTableItems;

        [JsonProperty(nameof(LootTableItems))]
        public ObservableCollection<LootTableItemViewModel> LootTableItems
        {
            get => _lootTableItems;
            set
            {
                _lootTableItems = value;
                NotifyOfPropertyChange(() => LootTableItems);
            }
        }

        private int _id;
        [JsonProperty(nameof(ID))]
        public int ID
        {
            get => _id;
            set
            {
                _id = value;
                NotifyOfPropertyChange(() => ID);
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

        public override void RefreshTrackedProperties()
        {
            base.RefreshTrackedProperties();

            foreach (var item in LootTableItems)
            {
                item.RefreshTrackedProperties();
            }
        }
    }
}
