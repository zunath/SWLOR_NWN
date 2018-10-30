using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder("ApartmentBuilding")]
    public class ApartmentBuildingViewModel: DBObjectViewModelBase
    {
        
        public ApartmentBuildingViewModel()
        {
            Name = "New Apartment Building";

            TrackProperty(this, x => x.Name);
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
    }
}
