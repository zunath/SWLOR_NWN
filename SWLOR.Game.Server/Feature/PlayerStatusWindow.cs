using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerStatusWindow
    {
        private static Gui.IdReservation _idReservation;

        [NWNEventHandler("mod_load")]
        public static void ReserveIds()
        {
            _idReservation = Gui.ReserveIds(nameof(PlayerStatusWindow), 14);
        }

        /// <summary>
        /// On module heartbeat, draws all GUI elements on every player's screen.
        /// </summary>
        [NWNEventHandler("interval_pc_1s")]
        public static void DrawGuiElements()
        {
            DrawStatusComponent(OBJECT_SELF);
        }

        /// <summary>
        /// Draws the HP, MP, and STM status information on the player's screen.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawStatusComponent(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            var currentHP = GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            var currentMP = dbPlayer.MP;
            var maxMP = Stat.GetMaxMP(player, dbPlayer);
            var currentSTM = dbPlayer.Stamina;
            var maxSTM = Stat.GetMaxStamina(player, dbPlayer);


            var backgroundBar = BuildBar(1, 1, 22);
            var hpBar = BuildBar(currentHP, maxHP, 22);
            var mpBar = BuildBar(currentMP, maxMP, 22);
            var stmBar = BuildBar(currentSTM, maxSTM, 22);

            const int windowX = 1;
            const int windowY = 5;
            const int windowWidth = 25;
            const ScreenAnchor Anchor = ScreenAnchor.BottomRight;

            // Draw order is backwards. The top-most layer needs to be drawn first.
            var centerWindowX = Gui.CenterStringInWindow(backgroundBar, windowX, windowWidth);

            // Draw the text
            var hpText = "HP:".PadRight(5, ' ') + $"{currentHP.ToString().PadLeft(4, ' ')} / {maxHP.ToString().PadLeft(4, ' ')}";
            var mpText = "MP:".PadRight(5, ' ') + $"{currentMP.ToString().PadLeft(4, ' ')} / {maxMP.ToString().PadLeft(4, ' ')}";
            var stmText = "STM:".PadRight(5, ' ') + $"{currentSTM.ToString().PadLeft(4, ' ')} / {maxSTM.ToString().PadLeft(4, ' ')}";

            PostString(player, hpText, centerWindowX + 8, windowY + 3, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + 2, Gui.TextName);
            PostString(player, mpText, centerWindowX + 8, windowY + 2, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + 1, Gui.TextName);
            PostString(player, stmText, centerWindowX + 8, windowY + 1, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId, Gui.TextName);

            // Draw the bars
            PostString(player, hpBar, centerWindowX + 2, windowY + 3, Anchor, 0.0f, Gui.ColorHealthBar, Gui.ColorHealthBar, _idReservation.StartId + 3, Gui.FontName);
            PostString(player, mpBar, centerWindowX + 2, windowY + 2, Anchor, 0.0f, Gui.ColorManaBar, Gui.ColorManaBar, _idReservation.StartId + 4, Gui.FontName);
            PostString(player, stmBar, centerWindowX + 2, windowY + 1, Anchor, 0.0f, Gui.ColorStaminaBar, Gui.ColorStaminaBar, _idReservation.StartId + 5, Gui.FontName);

            // Draw the backgrounds
            if (!GetLocalBool(player, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN"))
            {
                PostString(player, backgroundBar, centerWindowX + 2, windowY + 3, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 6, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, windowY + 2, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 7, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, windowY + 1, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 8, Gui.FontName);

                Gui.DrawWindow(player, _idReservation.StartId + 9, Anchor, windowX, windowY, windowWidth-2, 3);
            }
        }

        [NWNEventHandler("mod_exit")]
        public static void RemoveTempVariables()
        {
            var exiting = GetExitingObject();
            DeleteLocalBool(exiting, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN");
        }

        /// <summary>
        /// Builds a bar for display with the PostString call.
        /// </summary>
        /// <param name="current">The current value to display.</param>
        /// <param name="maximum">The maximum value to display.</param>
        /// <param name="width"></param>
        /// <returns></returns>
        private static string BuildBar(int current, int maximum, int width)
        {
            if (current <= 0) return string.Empty;

            var unitsPerWidth = (maximum / (float)width);
            var currentNumber = Math.Ceiling(current / unitsPerWidth);
            string bar = string.Empty;

            // When the anchor is at the top-right, the drawing is backwards.
            // We still need to add spaces to the end of the bar to ensure it's showing the
            // empty space.
            for(var x = 0; x < width; x++)
            {
                if (x < currentNumber)
                {
                    bar += Gui.BlankWhite;
                }
                else
                {
                    bar += " ";
                }
            }

            return bar;
        }
    }
}
