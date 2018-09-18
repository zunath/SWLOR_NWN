using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IDurabilityService
    {
        float GetDurability(NWItem item);
        float GetMaxDurability(NWItem item);
        void OnHitCastSpell(NWPlayer oTarget);
        void OnModuleEquip();
        string OnModuleExamine(string existingDescription, NWObject examinedObject);
        void RunItemDecay(NWPlayer player, NWItem item);
        void RunItemDecay(NWPlayer player, NWItem item, float reduceAmount);
        void RunItemRepair(NWPlayer oPC, NWItem oItem, float amount);
        void SetDurability(NWItem item, float value);
        void SetMaxDurability(NWItem item, float value);
    }
}