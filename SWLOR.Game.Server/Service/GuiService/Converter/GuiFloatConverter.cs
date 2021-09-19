using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public class GuiFloatConverter : IGuiBindConverter<float>
    {
        public float ToObject(Json json)
        {
            return JsonGetFloat(json);
        }

        public Json ToJson(float obj)
        {
            return JsonFloat(obj);
        }
    }
}
