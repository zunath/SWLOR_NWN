using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class CharacterBackgroundViewModel : GameTopicBaseVM
    {
        public CharacterBackgroundViewModel(GameTopicCollection collection)
            : base(collection, GameTopicCategory.CharacterBackgrounds)
        {
        }

        protected override int CategoryID => 3;
    }
}
