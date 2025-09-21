using System.Collections.Generic;

namespace SWLOR.Shared.Abstractions.Models
{
    public class GuiBindingList<T> : List<T>
    {
        public GuiBindingList() : base() { }
        public GuiBindingList(IEnumerable<T> collection) : base(collection) { }
    }
}
