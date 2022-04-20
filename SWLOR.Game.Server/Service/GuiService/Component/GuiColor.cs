using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColor
    {
        /// <summary>
        /// The amount of red to use. 0-255
        /// </summary>
        public int Red { get; set; }

        /// <summary>
        /// The amount of green to use. 0-255
        /// </summary>
        public int Green { get; set; }

        /// <summary>
        /// The amount of blue to use. 0-255
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// The amount of alpha (transparency) to use. 0-255
        /// </summary>
        public int Alpha { get; set; }

        public GuiColor(int red, int green, int blue, int alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public Json ToJson()
        {
            return Nui.Color(Red, Green, Blue, Alpha);
        }
    }
}
