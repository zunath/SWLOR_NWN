using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// Draws the achievement unlocked notice at the top right of the screen: the logic that used to sit in
    /// <c>AchievementService</c>, moved here unchanged so Character no longer names the GUI service.
    /// </summary>
    public class AchievementNotificationWindow : IAchievementNotification
    {
        // The name the GUI id reservation has always been made under (nameof of the former facade, Service.Achievement).
        private const string GuiReservationName = "Achievement";

        private readonly IGuiService _gui;
        private IdReservation _idReservation;

        public AchievementNotificationWindow(IGuiService gui)
        {
            _gui = gui;
        }

        public void ReserveIds()
        {
            _idReservation = _gui.ReserveIds(GuiReservationName, 6);
        }

        public void Display(uint player, string name)
        {
            const int WindowX = 4;
            const int WindowY = 4;
            const int WindowWidth = 26;

            var centerWindowX = _gui.CenterStringInWindow(name, WindowX, WindowWidth);
            PostString(player, "Achievement Unlocked", centerWindowX + 2, WindowY+1, ScreenAnchor.TopRight, 10.0f, GuiService.ColorWhite, GuiService.ColorYellow, _idReservation.StartId,GuiService.TextName);
            PostString(player, " " + name, centerWindowX + 4, WindowY+3, ScreenAnchor.TopRight, 10.0f, GuiService.ColorWhite, GuiService.ColorYellow, _idReservation.StartId + 1, GuiService.TextName);
            _gui.DrawWindow(player, _idReservation.StartId + 2, ScreenAnchor.TopRight, WindowX, WindowY, WindowWidth, 4);
        }
    }
}
