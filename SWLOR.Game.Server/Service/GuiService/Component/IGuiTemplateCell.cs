namespace SWLOR.Game.Server.Service.GuiService.Component
{
    /// <summary>
    /// Non-generic view of a list template cell, used by layout validation to
    /// inspect cell sizing without knowing the view model type.
    /// </summary>
    public interface IGuiTemplateCell
    {
        /// <summary>
        /// Whether the cell may grow to fill remaining space.
        /// </summary>
        bool IsVariable { get; }
    }
}
