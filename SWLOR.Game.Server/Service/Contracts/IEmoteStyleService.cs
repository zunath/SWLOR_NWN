using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IEmoteStyleService
    {
        EmoteStyle GetEmoteStyle(NWObject obj);
        void SetEmoteStyle(NWObject obj, EmoteStyle style);
    }
}