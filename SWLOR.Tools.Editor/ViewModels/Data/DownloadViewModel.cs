using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(Download))]
    public class DownloadViewModel : DBObjectViewModelBase
    {
        public DownloadViewModel()
        {
            Name = string.Empty;
            Description = string.Empty;
            Url = string.Empty;
            IsActive = true;

            TrackProperty(this, x => Name);
            TrackProperty(this, x => Description);
            TrackProperty(this, x => Url);
            TrackProperty(this, x => IsActive);
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

        private string _url;
        [JsonProperty(nameof(Url))]
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                NotifyOfPropertyChange(() => Url);
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
