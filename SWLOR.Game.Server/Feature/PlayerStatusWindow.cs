using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
        /// Every second, draws all GUI elements on the player's screen.
        /// </summary>
        [NWNEventHandler("interval_pc_1s")]
        public static void DrawGuiElements()
        {
            var player = OBJECT_SELF;

            // Space UI elements
            if (Space.IsPlayerInSpaceMode(player))
            {
                DrawSpaceStatusComponent(player);
            }
            // Character UI elements
            else
            {
                DrawCharacterStatusComponent(player);
            }
        }

        /// <summary>
        /// Draws the HP, FP, and STM status information on the player's screen.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawCharacterStatusComponent(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            var currentHP = GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            var currentFP = dbPlayer.FP;
            var maxFP = Stat.GetMaxFP(player, dbPlayer);
            var currentSTM = dbPlayer.Stamina;
            var maxSTM = Stat.GetMaxStamina(player, dbPlayer);

            var backgroundBar = BuildBar(1, 1, 22);
            var hpBar = BuildBar(currentHP, maxHP, 22);
            var fpBar = BuildBar(currentFP, maxFP, 22);
            var stmBar = BuildBar(currentSTM, maxSTM, 22);

            const int WindowX = 1;
            const int WindowY = 5;
            const int WindowWidth = 25;
            const ScreenAnchor Anchor = ScreenAnchor.BottomRight;

            // Draw order is backwards. The top-most layer needs to be drawn first.
            var centerWindowX = Gui.CenterStringInWindow(backgroundBar, WindowX, WindowWidth);

            // Draw the text
            var hpText = "HP:".PadRight(5, ' ') + $"{currentHP.ToString().PadLeft(4, ' ')} / {maxHP.ToString().PadLeft(4, ' ')}";
            var fpText = "FP:".PadRight(5, ' ') + $"{currentFP.ToString().PadLeft(4, ' ')} / {maxFP.ToString().PadLeft(4, ' ')}";
            var stmText = "STM:".PadRight(5, ' ') + $"{currentSTM.ToString().PadLeft(4, ' ')} / {maxSTM.ToString().PadLeft(4, ' ')}";

            PostString(player, hpText, centerWindowX + 8, WindowY + 3, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + 2, Gui.TextName);
            PostString(player, fpText, centerWindowX + 8, WindowY + 2, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + 1, Gui.TextName);
            PostString(player, stmText, centerWindowX + 8, WindowY + 1, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId, Gui.TextName);

            // Draw the bars
            PostString(player, hpBar, centerWindowX + 2, WindowY + 3, Anchor, 0.0f, Gui.ColorHealthBar, Gui.ColorHealthBar, _idReservation.StartId + 3, Gui.FontName);
            PostString(player, fpBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorManaBar, Gui.ColorManaBar, _idReservation.StartId + 4, Gui.FontName);
            PostString(player, stmBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorStaminaBar, Gui.ColorStaminaBar, _idReservation.StartId + 5, Gui.FontName);

            // Draw the backgrounds
            if (!GetLocalBool(player, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN"))
            {
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 3, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 6, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 7, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 8, Gui.FontName);

                Gui.DrawWindow(player, _idReservation.StartId + 9, Anchor, WindowX, WindowY, WindowWidth-2, 3);
            }
        }

        /// <summary>
        /// Draws the ship's Shield, Hull, and Capacitor status information on the player's screen.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawSpaceStatusComponent(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();
            var shipId = dbPlayer.ActiveShipId;
            var dbPlayerShip = dbPlayer.Ships[shipId];
            var shipDetail = Space.GetShipDetailByType(dbPlayerShip.Type);

            var currentShields = dbPlayerShip.Shield;
            var maxShields = shipDetail.MaxShield + dbPlayerShip.MaxShieldBonus;
            var currentHull = dbPlayerShip.Hull;
            var maxHull = shipDetail.MaxHull + dbPlayerShip.MaxHullBonus;
            var currentCapacitor = dbPlayerShip.Capacitor;
            var maxCapacitor = shipDetail.MaxCapacitor + dbPlayerShip.MaxCapacitorBonus;

            var backgroundBar = BuildBar(1, 1, 22);
            var shieldsBar = BuildBar(currentShields, maxShields, 22);
            var hullBar = BuildBar(currentHull, maxHull, 22);
            var capacitorBar = BuildBar(currentCapacitor, maxCapacitor, 22);

            const int WindowX = 1;
            const int WindowY = 5;
            const int WindowWidth = 25;
            const ScreenAnchor Anchor = ScreenAnchor.BottomRight;

            // Draw order is backwards. The top-most layer needs to be drawn first.
            var centerWindowX = Gui.CenterStringInWindow(backgroundBar, WindowX, WindowWidth);

            // Draw the text
            var shieldsText = "SH:".PadRight(5, ' ') + $"{currentShields.ToString().PadLeft(4, ' ')} / {maxShields.ToString().PadLeft(4, ' ')}";
            var hullText = "HL:".PadRight(5, ' ') + $"{currentHull.ToString().PadLeft(4, ' ')} / {maxHull.ToString().PadLeft(4, ' ')}";
            var capacitorText = "CAP:".PadRight(5, ' ') + $"{currentCapacitor.ToString().PadLeft(4, ' ')} / {maxCapacitor.ToString().PadLeft(4, ' ')}";

            PostString(player, shieldsText, centerWindowX + 8, WindowY + 3, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + 2, Gui.TextName);
            PostString(player, hullText, centerWindowX + 8, WindowY + 2, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + 1, Gui.TextName);
            PostString(player, capacitorText, centerWindowX + 8, WindowY + 1, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId, Gui.TextName);

            // Draw the bars
            PostString(player, shieldsBar, centerWindowX + 2, WindowY + 3, Anchor, 0.0f, Gui.ColorShieldsBar, Gui.ColorShieldsBar, _idReservation.StartId + 3, Gui.FontName);
            PostString(player, hullBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorHullBar, Gui.ColorHullBar, _idReservation.StartId + 4, Gui.FontName);
            PostString(player, capacitorBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorCapacitorBar, Gui.ColorCapacitorBar, _idReservation.StartId + 5, Gui.FontName);

            // Draw the backgrounds
            if (!GetLocalBool(player, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN"))
            {
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 3, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 6, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 7, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _idReservation.StartId + 8, Gui.FontName);

                Gui.DrawWindow(player, _idReservation.StartId + 9, Anchor, WindowX, WindowY, WindowWidth - 2, 3);
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
