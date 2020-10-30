using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class SneakAttack: IPerkHandler
    {
        public PerkType PerkType => PerkType.SneakAttack;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            var weapon = oPC.RightHand;
            
            if (weapon.CustomItemType != CustomItemType.FinesseVibroblade)
                return "You must be equipped with a finesse blade to use that ability";

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
            var perkRank = PerkService.GetCreaturePerkLevel(oPC, PerkType.SneakAttack);
            var cooldown = baseCooldownTime;

            if (perkRank == 2)
            {
                cooldown -= 30f;
            }
            else if (perkRank > 2)
            {
                cooldown -= 60f;
            }

            return cooldown;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            var minimum = creature.Facing - 20;
            var maximum = creature.Facing + 20;

            if (target.Facing >= minimum &&
                target.Facing <= maximum)
            {
                // Mark the player as committing a sneak attack.
                // This is later picked up in the OnApplyDamage event.
                creature.SetLocalInt("SNEAK_ATTACK_ACTIVE", 1);
            }
            else
            {
                creature.SetLocalInt("SNEAK_ATTACK_ACTIVE", 2);
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
            throw new NotImplementedException();
        }
    }
}
