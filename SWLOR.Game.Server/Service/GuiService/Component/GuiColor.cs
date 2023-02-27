using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColor
    {
        /// <summary>
        /// The amount of red to use. 0-255
        /// </summary>
        public int R { get; set; }

        /// <summary>
        /// The amount of green to use. 0-255
        /// </summary>
        public int G { get; set; }

        /// <summary>
        /// The amount of blue to use. 0-255
        /// </summary>
        public int B { get; set; }

        /// <summary>
        /// The amount of alpha (transparency) to use. 0-255
        /// </summary>
        public int Alpha { get; set; }

        public GuiColor(int red, int green, int blue, int alpha = 255)
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

        public static GuiColor Green => new(0, 255, 0);
        public static GuiColor Red => new(255, 0, 0);
        public static GuiColor Cyan => new(0, 255, 255);
        public static GuiColor White => new(255, 255, 255);
        public static GuiColor Grey => new(169, 169, 169);

        public static GuiColor HPColor = new(139, 0, 0);
        public static GuiColor FPColor = new(0, 138, 250);
        public static GuiColor STMColor = new(0, 139, 0);
    }
}
