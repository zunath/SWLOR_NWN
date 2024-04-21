using System.ComponentModel;

namespace SWLOR.Core.Service.GuiService
{
    public class GuiBindingList<T> : BindingList<T>, IGuiBindingList
    {
        public string PropertyName { get; set; }
        public int MaxSize { get; set; }
        public GuiBindingList<bool> ListItemVisibility { get; set; }
    }
}
