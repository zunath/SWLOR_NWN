using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class CraftingViewModel: GameTopicBaseVM
    {
        public CraftingViewModel(GameTopicCollection collection) 
            : base(collection, GameTopicCategory.Crafting)
        {
        }

        protected override int CategoryID => 5;
    }
}
