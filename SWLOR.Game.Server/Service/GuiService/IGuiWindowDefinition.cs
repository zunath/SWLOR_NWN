namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiWindowDefinition
    {
        /// <summary>
        /// Builds the window definition and view model, then registers it into the cache.
        /// </summary>
        /// <returns>A constructed window</returns>
        GuiConstructedWindow BuildWindow();
    }
}
