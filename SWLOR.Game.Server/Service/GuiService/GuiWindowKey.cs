namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// The ids NUI windows and their element events are registered under: pure functions of a window type and an
    /// element id, with no state. <c>Gui.BuildWindowId</c> and <c>Gui.BuildEventKey</c> forward to the GUI service, which
    /// calls these; the window builder calls them directly so a layout can be built without a container.
    /// </summary>
    public static class GuiWindowKey
    {
        /// <summary>
        /// Builds a window Id based on the type of window provided.
        /// </summary>
        /// <param name="windowType">The type of window.</param>
        /// <returns>A key using the window type.</returns>
        public static string BuildWindowId(GuiWindowType windowType)
        {
            return $"GUI_WINDOW_{windowType}";
        }

        /// <summary>
        /// Builds a key based on the window Id and element Id.
        /// This is used for event mapping.
        /// </summary>
        /// <param name="windowId">The Id of the Window</param>
        /// <param name="elementId">The Id of the Element</param>
        /// <returns>A key using the window Id and element Id.</returns>
        public static string BuildEventKey(string windowId, string elementId)
        {
            return windowId + "_" + elementId;
        }
    }
}
