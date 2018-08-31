using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IMapPinService
    {
        void AddWaypointMapPin(NWPlayer oPC, string waypointTag, string text, string mapPinTag);
        void DeleteMapPin(NWPlayer oPC, int index);
        void DeleteMapPin(NWPlayer oPC, string pinTag);
        MapPin GetMapPin(NWPlayer oPC, int index);
        MapPin GetMapPin(NWPlayer oPC, string pinTag);
        int GetNumberOfMapPins(NWPlayer oPC);
        void OnModuleClientEnter();
        void OnModuleClientLeave();
        void SetMapPin(NWPlayer oPC, string text, float positionX, float positionY, NWArea area);
        void SetMapPin(NWPlayer oPC, string text, float positionX, float positionY, NWArea area, string tag);
    }
}
