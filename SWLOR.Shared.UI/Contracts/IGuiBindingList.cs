using System.ComponentModel;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Shared.UI.Contracts
{
    public interface IGuiBindingList
    {
        string PropertyName { get; set; }
        event ListChangedEventHandler ListChanged;
        int Count { get; }
        int MaxSize { get; set; }
        public GuiBindingList<bool> ListItemVisibility { get; set; }
    }
}
