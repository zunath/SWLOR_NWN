using System;
using Newtonsoft.Json.Linq;
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
            // Using JSON.NET to parse this data. NWScript's methods were giving me recursion errors
            // during Client UI watch updates
            var jsonDump = JsonDump(json);
            var data = JObject.Parse(jsonDump);
            
            var rect = new GuiRectangle(
                (float)Convert.ToDouble(data["x"]),
                (float)Convert.ToDouble(data["y"]),
                (float)Convert.ToDouble(data["w"]),
                (float)Convert.ToDouble(data["h"]));

            return rect;
        }

        public Json ToJson(GuiRectangle obj)
        {
            return Nui.Rect(obj.X, obj.Y, obj.Width, obj.Height);
        }
    }
}
