using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// A layout currently applied to a group element nested inside a window's main view.
    /// Exactly one of <see cref="PartialName"/> (a registered partial view) or
    /// <see cref="Layout"/> (a layout generated at runtime) is set.
    /// </summary>
    public sealed class GuiNestedLayout
    {
        public string ElementId { get; }
        public string PartialName { get; }
        public Json Layout { get; }

        public GuiNestedLayout(string elementId, string partialName, Json layout)
        {
            ElementId = elementId;
            PartialName = partialName;
            Layout = layout;
        }
    }
}
