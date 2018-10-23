using System.Collections.ObjectModel;
using Caliburn.Micro;
using SWLOR.Tools.Editor.ValueObjects;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ObjectListViewModel: PropertyChangedBase, IObjectListViewModel
    {
        private ObservableCollection<ObjectListItem> _dataObjects;

        public ObservableCollection<ObjectListItem> DataObjects
        {
            get => _dataObjects;
            set
            {
                _dataObjects = value;
                NotifyOfPropertyChange(() => DataObjects);
            }
        }

    }
}
