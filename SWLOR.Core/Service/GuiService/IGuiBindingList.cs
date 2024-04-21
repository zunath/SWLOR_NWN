using System.ComponentModel;

namespace SWLOR.Core.Service.GuiService
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
