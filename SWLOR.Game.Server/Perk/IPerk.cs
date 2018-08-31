using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk
{
    public interface IPerk
    {
        bool CanCastSpell(NWPlayer oPC, NWObject oTarget);
        string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget);
        int ManaCost(NWPlayer oPC, int baseManaCost);
        float CastingTime(NWPlayer oPC, float baseCastingTime);
        float CooldownTime(NWPlayer oPC, float baseCooldownTime);
        void OnImpact(NWPlayer oPC, NWObject oTarget);
        void OnPurchased(NWPlayer oPC, int newLevel);
        void OnRemoved(NWPlayer oPC);
        void OnItemEquipped(NWPlayer oPC, NWItem oItem);
        void OnItemUnequipped(NWPlayer oPC, NWItem oItem);
        void OnCustomEnmityRule(NWPlayer oPC, int amount);
        bool IsHostile();
    }
}
