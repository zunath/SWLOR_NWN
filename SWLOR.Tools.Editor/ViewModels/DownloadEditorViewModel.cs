using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DownloadEditorViewModel: 
        BaseEditorViewModel<DownloadViewModel>,
        IDownloadEditorViewModel
    {
        public DownloadEditorViewModel(
            IEventAggregator eventAggregator, 
            IObjectListViewModel<DownloadViewModel> objListVM) 
            : base(eventAggregator, objListVM)
        {
        }

        protected override DownloadViewModel CreateNew()
        {
            DownloadViewModel download = new DownloadViewModel();
            return download;
        }
    }
}
