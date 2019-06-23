using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.TwoHanded
{
    public class Skewer : IPerkHandler
    {
        public PerkType PerkType => PerkType.Skewer;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.Polearm)
                return "You must be equipped with a Polearm in order to use this ability.";

            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int chance = perkLevel * 25;

            // Failed to interrupt.
            if(RandomService.D100(1) > chance)
            {
                creature.SendMessage("You fail to interrupt your target's concentration.");
                return;
            }
            
            NWCreature targetCreature = target.Object;
            var effect = AbilityService.GetActiveConcentrationEffect(targetCreature);
            if (effect.Type != PerkType.Unknown)
            {
                targetCreature.SendMessage("Your concentration effect has been interrupted by " + creature.Name + ".");
                AbilityService.EndConcentrationEffect(target.Object);
            }
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {

        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {

        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }
        
        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}
