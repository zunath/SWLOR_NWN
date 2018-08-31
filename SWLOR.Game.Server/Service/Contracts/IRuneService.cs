using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IRuneService
    {
        RuneSlots GetRuneSlots(NWItem item);
        CustomItemPropertyType GetRuneType(NWItem item);
        bool IsRune(NWItem item);
        string PrismaticString();
        string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject);
        void OnModuleApplyDamage();
    }
}