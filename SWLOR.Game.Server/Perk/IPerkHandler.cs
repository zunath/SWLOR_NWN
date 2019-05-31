using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk
{
    public interface IPerkHandler
    {
        PerkType PerkType { get; }
        string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier);
        int FPCost(NWPlayer oPC, int baseFPCost, int spellTier);
        float CastingTime(NWPlayer oPC, float baseCastingTime, int spellTier);
        float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellTier);
        int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellTier);
        void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellTier);
        void OnConcentrationTick(NWPlayer player, NWObject target, int spellTier, int tick);
        void OnPurchased(NWPlayer oPC, int newLevel);
        void OnRemoved(NWPlayer oPC);
        void OnItemEquipped(NWPlayer oPC, NWItem oItem);
        void OnItemUnequipped(NWPlayer oPC, NWItem oItem);
        void OnCustomEnmityRule(NWPlayer oPC, int amount);
        bool IsHostile();
    }
}
