using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public class GuiBoolConverter: IGuiBindConverter<bool>
    {
        public bool ToObject(Json json)
        {
            return JsonGetInt(json) == 1;
        }

        public Json ToJson(bool obj)
        {
            return JsonBool(obj);
        }
    }
}
