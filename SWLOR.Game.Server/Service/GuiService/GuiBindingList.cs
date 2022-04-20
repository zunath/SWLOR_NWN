using System.ComponentModel;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiBindingList<T> : BindingList<T>, IGuiBindingList
    {
        public string PropertyName { get; set; }
    }
}
