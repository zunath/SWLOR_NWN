using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public class GuiStringConverter: IGuiBindConverter<string>
    {
        public string ToObject(Json json)
        {
            return JsonGetString(json);
        }

        public Json ToJson(string obj)
        {
            return obj.ToJson();
        }
    }
}
