using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk
{
    public interface IPerkHandler
    {
        PerkType PerkType { get; }
        string CanCastSpell(NWCreature creature, NWObject oTarget, int spellTier);
        int FPCost(NWCreature creature, int baseFPCost, int spellTier);
        float CastingTime(NWCreature creature, float baseCastingTime, int spellTier);
        float CooldownTime(NWCreature creature, float baseCooldownTime, int spellTier);
        int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier);
        void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier);
        void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick);
        void OnPurchased(NWCreature creature, int newLevel);
        void OnRemoved(NWCreature creature);
        void OnItemEquipped(NWCreature creature, NWItem oItem);
        void OnItemUnequipped(NWCreature creature, NWItem oItem);
        void OnCustomEnmityRule(NWCreature creature, int amount);
        bool IsHostile();
    }
}
