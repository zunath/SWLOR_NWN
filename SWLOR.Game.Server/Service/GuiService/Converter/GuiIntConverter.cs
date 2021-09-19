using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public class GuiIntConverter: IGuiBindConverter<int>
    {
        public int ToObject(Json json)
        {
            return JsonGetInt(json);
        }

        public Json ToJson(int obj)
        {
            return JsonInt(obj);
        }
    }
}
