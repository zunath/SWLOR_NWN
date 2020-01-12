using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class SettingViewModel : GameTopicBaseVM
    {
        public SettingViewModel(GameTopicCollection collection)
            : base(collection, GameTopicCategory.Setting)
        {
        }

        protected override int CategoryID => 1;
    }
}
