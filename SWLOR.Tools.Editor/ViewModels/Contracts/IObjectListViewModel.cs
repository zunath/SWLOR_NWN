using System;
using System.Collections.ObjectModel;
using SWLOR.Tools.Editor.ValueObjects;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IObjectListViewModel<T>
    {
        ObservableCollection<T> DataObjects { get; set; }
        string DisplayName { get; set; }
        void New();
        void Rename();
        void Delete();
    }
}
