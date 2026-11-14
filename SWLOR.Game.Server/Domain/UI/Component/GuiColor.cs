using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.UI.Component
{
    public sealed record GuiColor
    {
        /// <summary>
        /// The amount of red to use. 0-255
        /// </summary>
        public byte R { get; init; }

        /// <summary>
        /// The amount of green to use. 0-255
        /// </summary>
        public byte G { get; init; }

        /// <summary>
        /// The amount of blue to use. 0-255
        /// </summary>
        public byte B { get; init; }

        /// <summary>
        /// The amount of alpha (transparency) to use. 0-255
        /// </summary>
        public byte Alpha { get; init; }

        public GuiColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            R = red;
            G = green;
            B = blue;
            Alpha = alpha;
        }

        public Json ToJson()
        {
            return Nui.Color(R, G, B, Alpha);
        }

        public static readonly GuiColor Green = new(0, 255, 0);
        public static readonly GuiColor Red = new(255, 0, 0);
        public static readonly GuiColor Cyan = new(0, 255, 255);
        public static readonly GuiColor White = new(255, 255, 255);
        public static readonly GuiColor Grey = new(169, 169, 169);

        public static readonly GuiColor HPColor = new(139, 0, 0);
        public static readonly GuiColor FPColor = new(0, 138, 250);
        public static readonly GuiColor STMColor = new(0, 139, 0);
    }
}
