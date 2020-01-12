using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class GameplayInfoViewModel: GameTopicBaseVM
    {
        public GameplayInfoViewModel(GameTopicCollection collection) 
            : base(collection, GameTopicCategory.OtherGameplayInfo)
        {
        }

        protected override int CategoryID => 6;
    }
}
