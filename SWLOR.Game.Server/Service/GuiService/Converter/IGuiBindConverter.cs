using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.GuiService.Converter
{
    public interface IGuiBindConverter<T>
    {
        T ToObject(Json json);
        Json ToJson(T obj);
    }
}
