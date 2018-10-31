using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(CraftBlueprint))]
    public class CraftBlueprintViewModel : DBObjectViewModelBase
    {
        private string _displayName;

        public override string DisplayName
        {
            get => _displayName;
            set
            {
                _itemName = value;
                _displayName = value;
                NotifyOfPropertyChange(() => ItemName);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        private string _itemName;
        [JsonProperty(nameof(ItemName))]
        public string ItemName
        {
            get => _itemName;
            set
            {
                _itemName = value;
                _displayName = value;
                NotifyOfPropertyChange(() => ItemName);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }
    }
}
