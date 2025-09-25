using System.Collections.Generic;
using System.ComponentModel;

namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface IGuiBindingList: IBindingList
    {
        string PropertyName { get; set; }
        event ListChangedEventHandler ListChanged;
        int Count { get; }
        int MaxSize { get; set; }
        public IGuiBindingList<bool> ListItemVisibility { get; set; }
    }

    public interface IGuiBindingList<T>: IGuiBindingList, IEnumerable<T>
    {

    }
}
