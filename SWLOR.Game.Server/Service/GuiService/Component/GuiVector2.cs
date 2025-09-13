using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Core.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiVector2
    {
        /// <summary>
        /// The X value
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The Y value
        /// </summary>
        public float Y { get; set; }

        public GuiVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Builds the vector 2.
        /// </summary>
        /// <returns>Json representing the vector 2 element.</returns>
        public Json ToJson()
        {
            return Nui.Vec(X, Y);
        }
    }
}
