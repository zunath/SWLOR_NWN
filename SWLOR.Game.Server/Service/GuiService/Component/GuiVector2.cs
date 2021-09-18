using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiVector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public GuiVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Json ToJson()
        {
            return Nui.Vec(X, Y);
        }
    }
}
