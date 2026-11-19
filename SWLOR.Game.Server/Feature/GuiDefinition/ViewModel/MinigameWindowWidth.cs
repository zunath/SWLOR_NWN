using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The crafting and gathering minigame windows wrap their sentences to the window's width, and each text box is tall enough for
    /// its lines at the window's default width. A narrower window would need more lines than a box holds and cut the text off, so
    /// these windows never get narrower than their default width (Settings keeps its minimum size the same way).
    /// </summary>
    public static class MinigameWindowWidth
    {
        /// <summary>The geometry widened to <paramref name="minimumWidth"/>, or null when it is already wide enough.</summary>
        public static GuiRectangle Widened(GuiRectangle current, float minimumWidth)
        {
            if (current == null || current.Width >= minimumWidth)
                return null;

            return new GuiRectangle(current.X, current.Y, minimumWidth, current.Height);
        }
    }
}
