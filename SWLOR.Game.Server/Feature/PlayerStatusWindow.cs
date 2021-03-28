using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerStatusWindow
    {
        private static Gui.IdReservation _characterIdReservation;
        private static Gui.IdReservation _spaceIdReservation;

        [NWNEventHandler("mod_load")]
        public static void ReserveIds()
        {
            // Reserve 20 Ids for the player's status
            _characterIdReservation = Gui.ReserveIds(nameof(PlayerStatusWindow) + "_CHARACTER", 20);

            // Reserve another 22 Ids for the target's status (if player is in space mode)
            _spaceIdReservation = Gui.ReserveIds(nameof(PlayerStatusWindow) + "_SPACE", 22);
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
                var shipStatus = Space.GetShipStatus(player);
                var (target, targetShipStatus) = Space.GetCurrentTarget(player);

                DrawSpaceStatusComponent(player, player, shipStatus);
                DrawSpaceStatusComponent(player, target, targetShipStatus, 4, GetName(target));
            }
            // Character UI elements
            else
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                var isStandard = dbPlayer.CharacterType == CharacterType.Standard;

                // Standard Characters
                if (isStandard)
                {
                    DrawStandardCharacterStatusComponent(player);
                }
                // Force Characters
                else
                {
                    DrawForceCharacterStatusComponent(player);
                }
            }
        }

        /// <summary>
        /// Draws the HP, FP, and STM status information on the player's screen.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawStandardCharacterStatusComponent(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            var currentHP = GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            var currentSTM = dbPlayer.Stamina;
            var maxSTM = Stat.GetMaxStamina(player, dbPlayer);

            var backgroundBar = BuildBar(1, 1, 22);
            var hpBar = BuildBar(currentHP, maxHP, 22);
            var stmBar = BuildBar(currentSTM, maxSTM, 22);

            const int WindowX = 1;
            const int WindowY = 5;
            const int WindowWidth = 25;
            const ScreenAnchor Anchor = ScreenAnchor.BottomRight;

            // Draw order is backwards. The top-most layer needs to be drawn first.
            var centerWindowX = Gui.CenterStringInWindow(backgroundBar, WindowX, WindowWidth);

            // Draw the text
            var hpText = "HP:".PadRight(5, ' ') + $"{currentHP.ToString().PadLeft(4, ' ')} / {maxHP.ToString().PadLeft(4, ' ')}";
            var stmText = "STM:".PadRight(5, ' ') + $"{currentSTM.ToString().PadLeft(4, ' ')} / {maxSTM.ToString().PadLeft(4, ' ')}";

            PostString(player, hpText, centerWindowX + 8, WindowY + 2, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _characterIdReservation.StartId + 2, Gui.TextName);
            PostString(player, stmText, centerWindowX + 8, WindowY + 1, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _characterIdReservation.StartId, Gui.TextName);

            // Draw the bars
            PostString(player, hpBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorHealthBar, Gui.ColorHealthBar, _characterIdReservation.StartId + 3, Gui.FontName);
            PostString(player, stmBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorStaminaBar, Gui.ColorStaminaBar, _characterIdReservation.StartId + 5, Gui.FontName);

            // Draw the backgrounds
            if (!GetLocalBool(player, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN"))
            {
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _characterIdReservation.StartId + 6, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _characterIdReservation.StartId + 8, Gui.FontName);

                Gui.DrawWindow(player, _characterIdReservation.StartId + 9, Anchor, WindowX, WindowY, WindowWidth - 2, 2);
            }
        }

        /// <summary>
        /// Draws the HP, FP, and STM status information on a force player's screen.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawForceCharacterStatusComponent(uint player)
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

            PostString(player, hpText, centerWindowX + 8, WindowY + 3, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _characterIdReservation.StartId + 2, Gui.TextName);
            PostString(player, fpText, centerWindowX + 8, WindowY + 2, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _characterIdReservation.StartId + 1, Gui.TextName);
            PostString(player, stmText, centerWindowX + 8, WindowY + 1, Anchor, 0.0f, Gui.ColorWhite, Gui.ColorWhite, _characterIdReservation.StartId, Gui.TextName);

            // Draw the bars
            PostString(player, hpBar, centerWindowX + 2, WindowY + 3, Anchor, 0.0f, Gui.ColorHealthBar, Gui.ColorHealthBar, _characterIdReservation.StartId + 3, Gui.FontName);
            PostString(player, fpBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorManaBar, Gui.ColorManaBar, _characterIdReservation.StartId + 4, Gui.FontName);
            PostString(player, stmBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorStaminaBar, Gui.ColorStaminaBar, _characterIdReservation.StartId + 5, Gui.FontName);

            // Draw the backgrounds
            if (!GetLocalBool(player, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN"))
            {
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 3, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _characterIdReservation.StartId + 6, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 2, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _characterIdReservation.StartId + 7, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, WindowY + 1, Anchor, 0.0f, Gui.ColorBlack, Gui.ColorBlack, _characterIdReservation.StartId + 8, Gui.FontName);
                
                Gui.DrawWindow(player, _characterIdReservation.StartId + 9, Anchor, WindowX, WindowY, WindowWidth - 2, 3);
            }
        }

        /// <summary>
        /// Draws the ship's Shield, Hull, and Capacitor status information on the player's screen.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        /// <param name="target">The target currently selected, or the player itself.</param>
        /// <param name="shipStatus">The ship details</param>
        /// <param name="yOffset">The window's Y position offset</param>
        /// <param name="targetName">The name of the target. If null or whitespace, it will be drawn at the player's position. Otherwise it will be drawn at the target's position on the player's screen.</param>
        private static void DrawSpaceStatusComponent(uint player, uint target, ShipStatus shipStatus, int yOffset = 0, string targetName = "")
        {
            if (!GetIsObjectValid(target) || shipStatus == null) return;

            var guiStartId = string.IsNullOrWhiteSpace(targetName) ? _characterIdReservation.StartId : _spaceIdReservation.StartId;
            var lifeSpan = string.IsNullOrWhiteSpace(targetName) ? 0.0f : 1.2f;

            var currentShields = shipStatus.Shield;
            var maxShields = shipStatus.MaxShield;
            var currentHull = shipStatus.Hull;
            var maxHull = shipStatus.MaxHull;
            var currentCapacitor = shipStatus.Capacitor;
            var maxCapacitor = shipStatus.MaxCapacitor;

            var backgroundBar = BuildBar(1, 1, 22);
            var shieldsBar = BuildBar(currentShields, maxShields, 22);
            var hullBar = BuildBar(currentHull, maxHull, 22);
            var capacitorBar = BuildBar(currentCapacitor, maxCapacitor, 22);

            const int WindowX = 1;
            var windowY = 5 + yOffset + (string.IsNullOrWhiteSpace(targetName) ? 0 : 1);
            const int WindowWidth = 25;
            const ScreenAnchor Anchor = ScreenAnchor.BottomRight;

            // Draw order is backwards. The top-most layer needs to be drawn first.
            var centerWindowX = Gui.CenterStringInWindow(backgroundBar, WindowX, WindowWidth);

            // Build the text
            var shieldsText = "SH:".PadRight(5, ' ') + $"{currentShields.ToString().PadLeft(4, ' ')} / {maxShields.ToString().PadLeft(4, ' ')}";
            var hullText = "HL:".PadRight(5, ' ') + $"{currentHull.ToString().PadLeft(4, ' ')} / {maxHull.ToString().PadLeft(4, ' ')}";
            var capacitorText = "CAP:".PadRight(5, ' ') + $"{currentCapacitor.ToString().PadLeft(4, ' ')} / {maxCapacitor.ToString().PadLeft(4, ' ')}";

            // Draw header, if applicable.
            if (!string.IsNullOrWhiteSpace(targetName))
            {
                PostString(player, targetName, centerWindowX + 8, windowY + 4, Anchor, lifeSpan, Gui.ColorWhite, Gui.ColorWhite, guiStartId + 9, Gui.TextName);
            }

            PostString(player, shieldsText, centerWindowX + 8, windowY + 3, Anchor, lifeSpan, Gui.ColorWhite, Gui.ColorWhite, guiStartId + 2, Gui.TextName);
            PostString(player, hullText, centerWindowX + 8, windowY + 2, Anchor, lifeSpan, Gui.ColorWhite, Gui.ColorWhite, guiStartId + 1, Gui.TextName);
            PostString(player, capacitorText, centerWindowX + 8, windowY + 1, Anchor, lifeSpan, Gui.ColorWhite, Gui.ColorWhite, guiStartId, Gui.TextName);

            // Draw the bars
            PostString(player, shieldsBar, centerWindowX + 2, windowY + 3, Anchor, lifeSpan, Gui.ColorShieldsBar, Gui.ColorShieldsBar, guiStartId + 3, Gui.FontName);
            PostString(player, hullBar, centerWindowX + 2, windowY + 2, Anchor, lifeSpan, Gui.ColorHullBar, Gui.ColorHullBar, guiStartId + 4, Gui.FontName);
            PostString(player, capacitorBar, centerWindowX + 2, windowY + 1, Anchor, lifeSpan, Gui.ColorCapacitorBar, Gui.ColorCapacitorBar, guiStartId + 5, Gui.FontName);

            // Draw the backgrounds
            if (!GetLocalBool(player, "PLAYERSTATUSWINDOW_BACKGROUND_DRAWN"))
            {
                PostString(player, backgroundBar, centerWindowX + 2, windowY + 3, Anchor, lifeSpan, Gui.ColorBlack, Gui.ColorBlack, guiStartId + 6, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, windowY + 2, Anchor, lifeSpan, Gui.ColorBlack, Gui.ColorBlack, guiStartId + 7, Gui.FontName);
                PostString(player, backgroundBar, centerWindowX + 2, windowY + 1, Anchor, lifeSpan, Gui.ColorBlack, Gui.ColorBlack, guiStartId + 8, Gui.FontName);

                var windowHeight = string.IsNullOrWhiteSpace(targetName) ? 3 : 4;
                Gui.DrawWindow(player, guiStartId + 10, Anchor, WindowX, windowY, WindowWidth - 2, windowHeight, lifeSpan);
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
            for (var x = 0; x < width; x++)
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
