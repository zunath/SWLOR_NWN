using System.ComponentModel;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.UI.Model
{
    public class GuiBindingList<T> : BindingList<T>, IGuiBindingList<T>
    {
        public string PropertyName { get; set; }
        public int MaxSize { get; set; }
        public IGuiBindingList<bool> ListItemVisibility { get; set; }
    }
}
