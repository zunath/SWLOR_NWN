using System;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public class GuiColorConverter: IGuiBindConverter<GuiColor>
    {
        public GuiColor ToObject(Json json)
        {
            // Using JSON.NET to parse this data. NWScript's methods were giving me recursion errors
            // during Client UI watch updates
            var jsonDump = JsonDump(json);
            var data = JObject.Parse(jsonDump);

            var color = new GuiColor(
                    Convert.ToInt32(data["r"]),
                    Convert.ToInt32(data["g"]),
                    Convert.ToInt32(data["b"]),
                    Convert.ToInt32(data["a"]));
            
            return color;
        }

        public Json ToJson(GuiColor obj)
        {
            return Nui.Color(obj.Red, obj.Green, obj.Blue, obj.Alpha);
        }
    }
}
