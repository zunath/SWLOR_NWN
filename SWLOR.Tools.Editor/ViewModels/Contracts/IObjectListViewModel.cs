using System;
using System.Collections.ObjectModel;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IObjectListViewModel<T>
    {
        ObservableCollection<T> DataObjects { get; set; }
        T SelectedDataObject { get; set; }
        void New();
        void Rename();
        void Delete();
    }
}
