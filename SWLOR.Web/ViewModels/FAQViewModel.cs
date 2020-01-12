using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class FAQViewModel: GameTopicBaseVM
    {
        public FAQViewModel(GameTopicCollection collection) 
            : base(collection, GameTopicCategory.FAQ)
        {
        }

        protected override int CategoryID => 4;
    }
}
