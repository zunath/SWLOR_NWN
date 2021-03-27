using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Gui
    {
        public class IdReservation
        {
            public int Count { get; set; }
            public int StartId { get; set; }
            public int EndId { get; set; }
        }

        /// <summary>
        /// Name of the texture used for the GUI elements.
        /// </summary>
        public const string FontName = "fnt_es_gui";

        /// <summary>
        /// Name of the texture used for the GUI text.
        /// </summary>
        public const string TextName = "fnt_es_text";

        // The following letters represent the character locations of the elements on the GUI spritesheet.
        public const string WindowTopLeft = "a";
        public const string WindowTopMiddle = "b";
        public const string WindowTopRight = "c";
        public const string WindowMiddleLeft = "d";
        public const string WindowMiddleRight = "f";
        public const string WindowMiddleBlank = "i";
        public const string WindowBottomLeft = "h";
        public const string WindowBottomRight = "g";
        public const string WindowBottomMiddle = "e";
        public const string Arrow = "j";
        public const string BlankWhite = "k";

        // The following hex codes correspond to colors used on GUI elements.
        // Color tokens won't work on Gui elements.
        public static int ColorTransparent = Convert.ToInt32("0xFFFFFF00", 16);
        public static int ColorWhite = Convert.ToInt32("0xFFFFFFFF", 16);
        public static int ColorSilver = Convert.ToInt32("0xC0C0C0FF", 16);
        public static int ColorGray = Convert.ToInt32("0x808080FF", 16);
        public static int ColorDarkGray = Convert.ToInt32("0x303030FF", 16);
        public static int ColorBlack = Convert.ToInt32("0x000000FF", 16);
        public static int ColorRed = Convert.ToInt32("0xFF0000FF", 16);
        public static int ColorMaroon = Convert.ToInt32("0x800000FF", 16);
        public static int ColorOrange = Convert.ToInt32("0xFFA500FF", 16);
        public static int ColorYellow = Convert.ToInt32("0xFFFF00FF", 16);
        public static int ColorOlive = Convert.ToInt32("0x808000FF", 16);
        public static int ColorLime = Convert.ToInt32("0x00FF00FF", 16);
        public static int ColorGreen = Convert.ToInt32("0x008000FF", 16);
        public static int ColorAqua = Convert.ToInt32("0x00FFFFFF", 16);
        public static int ColorTeal = Convert.ToInt32("0x008080FF", 16);
        public static int ColorBlue = Convert.ToInt32("0x0000FFFF", 16);
        public static int ColorNavy = Convert.ToInt32("0x000080FF", 16);
        public static int ColorFuschia = Convert.ToInt32("0xFF00FFFF", 16);
        public static int ColorPurple = Convert.ToInt32("0x800080FF", 16);

        public static int ColorHealthBar = Convert.ToInt32("0x8B0000FF", 16);
        public static int ColorManaBar = Convert.ToInt32("0x00008BFF", 16);
        public static int ColorStaminaBar = Convert.ToInt32("0x008B00FF", 16);

        public static int ColorShieldsBar = Convert.ToInt32("0x00AAE4FF", 16);
        public static int ColorHullBar = Convert.ToInt32("0x8B0000FF", 16);
        public static int ColorCapacitorBar = Convert.ToInt32("0xFFA500FF", 16);

        private static int ReservedIdCount;

        private static readonly Dictionary<string, IdReservation> _systemReservedIdCount = new Dictionary<string, IdReservation>();

        /// <summary>
        /// Reserves the specified number of Ids for a given system.
        /// </summary>
        /// <param name="systemName">The name of the system. This should be unique across Id sets.</param>
        /// <param name="amount">The number of Ids to reserve.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public static IdReservation ReserveIds(string systemName, int amount)
        {
            // Ids haven't been reserved yet. Do that now.
            if (!_systemReservedIdCount.ContainsKey(systemName))
            {
                _systemReservedIdCount[systemName] = new IdReservation();
                _systemReservedIdCount[systemName].Count = amount;
                _systemReservedIdCount[systemName].StartId = ReservedIdCount;
                _systemReservedIdCount[systemName].EndId = ReservedIdCount + amount - 1;

                ReservedIdCount += amount;
            }

            return _systemReservedIdCount[systemName];
        }

        /// <summary>
        /// Retrieves a system's Id reservation.
        /// Throws an exception if the system has not yet been registered.
        /// </summary>
        /// <param name="systemName">The system to retrieve by.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public static IdReservation GetSystemReservation(string systemName)
        {
            return _systemReservedIdCount[systemName];
        }

        public static void DrawWindow(uint player, int startId, ScreenAnchor anchor, int x, int y, int width, int height, float lifeTime = 10.0f)
        {
            string top = WindowTopLeft;
            string middle = WindowMiddleLeft;
            string bottom = WindowBottomLeft;

            for (int i = 0; i < width; i++)
            {
                top += WindowTopMiddle;
                middle += WindowMiddleBlank;
                bottom += WindowBottomMiddle;
            }

            top += WindowTopRight;
            middle += WindowMiddleRight;
            bottom += WindowBottomRight;

            if (anchor == ScreenAnchor.BottomRight)
            {
                Draw(player, bottom, x, y, anchor, startId++, lifeTime);
            }
            else
            {
                Draw(player, top, x, y, anchor, startId++, lifeTime);
            }
            
            
            for (var i = 0; i < height; i++)
            {
                Draw(player, middle, x, ++y, anchor, startId++, lifeTime);
            }

            if (anchor == ScreenAnchor.BottomRight)
            {
                Draw(player, top, x, ++y, anchor, startId, lifeTime);
            }
            else
            {
                Draw(player, bottom, x, ++y, anchor, startId, lifeTime);
            }

        }

        private static void Draw(uint player, string message, int x, int y, ScreenAnchor anchor, int id, float lifeTime = 10.0f)
        {
            PostString(player, message, x, y, anchor, lifeTime, ColorWhite, ColorWhite, id, FontName);
        }

        /// <summary>
        /// Gets the modified X coordinate of where to place a string within the center of a window.
        /// </summary>
        /// <param name="text">The text to place in the center.</param>
        /// <param name="windowX">The X position of the window.</param>
        /// <param name="windowWidth">The width of the window.</param>
        /// <returns>The X coordinate to place a string so that it will be in the center of the window.</returns>
        public static int CenterStringInWindow(string text, int windowX, int windowWidth)
        {
            return (windowX + (windowWidth / 2)) - ((text.Length + 2) / 2);
        }
    }
}
