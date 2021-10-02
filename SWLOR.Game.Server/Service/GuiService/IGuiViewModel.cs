using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiViewModel
    {
        /// <summary>
        /// The window geometry. This is automatically bound for all windows.
        /// </summary>
        public GuiRectangle Geometry { get; set; }

        /// <summary>
        /// Binds a player and window with the associated view model.
        /// </summary>
        /// <param name="player">The player to bind.</param>
        /// <param name="windowToken">The window token to bind.</param>
        void Bind(uint player, int windowToken);

        /// <summary>
        /// Handles updating the view model with changes received from the player's client.
        /// </summary>
        /// <param name="propertyName">The name of the property to update.</param>
        void UpdatePropertyFromClient(string propertyName);
    }
}
