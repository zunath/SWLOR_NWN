using System.ComponentModel;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiBindingList
    {
        string PropertyName { get; set; }
        event ListChangedEventHandler ListChanged;
        int Count { get; }
    }
}
