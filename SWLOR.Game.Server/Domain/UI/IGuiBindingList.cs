using System.ComponentModel;

namespace SWLOR.Game.Server.Domain.UI
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
