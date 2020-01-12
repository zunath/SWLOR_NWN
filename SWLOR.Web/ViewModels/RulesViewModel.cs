using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class RulesViewModel: GameTopicBaseVM
    {
        public RulesViewModel(GameTopicCollection collection) 
            : base(collection, GameTopicCategory.Rules)
        {
        }

        protected override int CategoryID => 7;
    }
}
