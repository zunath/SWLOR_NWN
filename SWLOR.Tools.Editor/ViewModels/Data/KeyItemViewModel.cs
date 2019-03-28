using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(KeyItem))]
    public class KeyItemViewModel : DBObjectViewModelBase
    {
        public KeyItemViewModel()
        {
            TrackProperty(this, x => x.Name);
            TrackProperty(this, x => x.Description);
            TrackProperty(this, x => x.IsActive);
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get => _name;
            set
            {
                _name = value;
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
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        private string _description;
        [JsonProperty(nameof(Description))]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                NotifyOfPropertyChange(() => Description);
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
    }
}
