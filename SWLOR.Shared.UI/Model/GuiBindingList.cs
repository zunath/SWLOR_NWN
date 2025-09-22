using System.ComponentModel;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Shared.Core.Models
{
    public class GuiBindingList<T> : BindingList<T>, IGuiBindingList
    {
        public string PropertyName { get; set; }
        public int MaxSize { get; set; }
        public GuiBindingList<bool> ListItemVisibility { get; set; }
    }
}
