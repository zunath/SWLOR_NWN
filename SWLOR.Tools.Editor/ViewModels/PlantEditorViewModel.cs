using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class PlantEditorViewModel : BaseEditorViewModel<PlantViewModel>, IPlantEditorViewModel
    {
        public PlantEditorViewModel(
            IEventAggregator eventAggregator,
            IObjectListViewModel<PlantViewModel> objListVM)
            : base(eventAggregator, objListVM)
        {
        }

        protected override PlantViewModel CreateNew()
        {
            var obj = new PlantViewModel();
            return obj;
        }
    }
}
