using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk
{
    public interface IPerkBehaviour
    {
        bool CanCastSpell(NWPlayer oPC, NWObject oTarget);
        string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget);
        int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID);
        float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID);
        float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID);
        int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID);
        void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID);
        void OnPurchased(NWPlayer oPC, int newLevel);
        void OnRemoved(NWPlayer oPC);
        void OnItemEquipped(NWPlayer oPC, NWItem oItem);
        void OnItemUnequipped(NWPlayer oPC, NWItem oItem);
        void OnCustomEnmityRule(NWPlayer oPC, int amount);
        bool IsHostile();
    }
}
