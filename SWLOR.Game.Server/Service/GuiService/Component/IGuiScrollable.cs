using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    /// <summary>
    /// Non-generic view of a widget's scroll mode, used by layout validation. A
    /// scrollable container decouples its content's size requirements from its
    /// parent's constraints - overflowing content scrolls instead of failing the
    /// layout solve.
    /// </summary>
    public interface IGuiScrollable
    {
        /// <summary>
        /// The scroll mode declared on the widget.
        /// </summary>
        NuiScrollbars Scrollbars { get; }
    }
}
