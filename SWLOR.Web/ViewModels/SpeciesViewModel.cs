using SWLOR.Web.Models;
using SWLOR.Web.ViewModels.BaseViewModels;

namespace SWLOR.Web.ViewModels
{
    public class SpeciesViewModel : GameTopicBaseVM
    {
        public SpeciesViewModel(GameTopicCollection collection)
            : base(collection, GameTopicCategory.Species)
        {
        }

        protected override int CategoryID => 2;
    }
}
