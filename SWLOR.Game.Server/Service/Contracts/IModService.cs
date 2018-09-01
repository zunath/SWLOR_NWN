using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IModService
    {
        ModSlots GetModSlots(NWItem item);
        CustomItemPropertyType GetModType(NWItem item);
        bool IsRune(NWItem item);
        string PrismaticString();
        string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject);
        void OnModuleApplyDamage();
    }
}