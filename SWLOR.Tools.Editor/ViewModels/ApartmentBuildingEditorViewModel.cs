using Caliburn.Micro;
using SWLOR.Tools.Editor.Enumeration;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ApartmentBuildingEditorViewModel: 
        BaseEditorViewModel<ApartmentBuildingViewModel>, 
        IApartmentBuildingEditorViewModel
    {
        public ApartmentBuildingEditorViewModel(
            IEventAggregator eventAggregator, 
            IObjectListViewModel<ApartmentBuildingViewModel> objListVM)
            : base(eventAggregator, objListVM)
        {
        }

        protected override ApartmentBuildingViewModel CreateNew()
        {
            var obj = new ApartmentBuildingViewModel();
            return obj;
        }

    }
}
