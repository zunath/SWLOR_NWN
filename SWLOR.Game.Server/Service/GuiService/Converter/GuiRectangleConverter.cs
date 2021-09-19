using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public class GuiRectangleConverter : IGuiBindConverter<GuiRectangle>
    {
        public GuiRectangle ToObject(Json json)
        {
            var rect = new GuiRectangle(
                JsonGetFloat(JsonObjectGet(json, "x")),
                JsonGetFloat(JsonObjectGet(json, "y")),
                JsonGetFloat(JsonObjectGet(json, "w")),
                JsonGetFloat(JsonObjectGet(json, "h"))
            );

            return rect;
        }

        public Json ToJson(GuiRectangle obj)
        {
            return Nui.Rect(obj.X, obj.Y, obj.Width, obj.Height);
        }
    }
}
