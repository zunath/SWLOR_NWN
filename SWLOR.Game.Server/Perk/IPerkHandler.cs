using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk
{
    public interface IPerkHandler
    {
        PerkType PerkType { get; }
        string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellFeatID);
        int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID);
        float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID);
        float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID);
        int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID);
        void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID);
        void OnConcentrationTick(NWPlayer player, int perkLevel, int tick);
        void OnPurchased(NWPlayer oPC, int newLevel);
        void OnRemoved(NWPlayer oPC);
        void OnItemEquipped(NWPlayer oPC, NWItem oItem);
        void OnItemUnequipped(NWPlayer oPC, NWItem oItem);
        void OnCustomEnmityRule(NWPlayer oPC, int amount);
        bool IsHostile();
    }
}
