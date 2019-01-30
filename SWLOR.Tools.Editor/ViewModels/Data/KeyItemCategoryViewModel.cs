using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(KeyItemCategory))]
    public class KeyItemCategoryViewModel : DBObjectViewModelBase
    {
        public KeyItemCategoryViewModel()
        {
            KeyItems = new ObservableCollection<KeyItemViewModel>();
            Name = "New Key Item Category";

            TrackProperty(this, x => x.Name);
            TrackProperty(this, x => x.IsActive);
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

        private ObservableCollection<KeyItemViewModel> _keyItems;
        [JsonProperty(nameof(KeyItems))]
        public ObservableCollection<KeyItemViewModel> KeyItems
        {
            get => _keyItems;
            set
            {
                _keyItems = value;
                NotifyOfPropertyChange(() => KeyItems);
            }
        }
    }
}
