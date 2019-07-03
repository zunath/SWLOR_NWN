using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class ForceInsight: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceInsight;
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
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
                int abamount = 0;
                int acamount = 0;

            // Handle effects for differing spellTier values
            switch (perkLevel)
                {
                    case 1:
                        abamount = 3;
                        acamount = 0;   
                        break;
                    case 2:
                        abamount = 5;
                        acamount = 2;
                        break;
                    case 3:
                        abamount = 5;
                        acamount = 4;
                        break;
                default:
                        throw new ArgumentOutOfRangeException(nameof(perkLevel));
                }

                Effect effect = new Effect();

                effect = _.EffectACIncrease(acamount);
                effect = _.EffectLinkEffects(effect, _.EffectAttackIncrease(abamount));

                creature.AssignCommand(() =>
                {
                    _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, creature, 6.1f);
                });           
        }
    }
}
