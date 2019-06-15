using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class BattleMeditation: IPerkHandler
    {
        public PerkType PerkType => PerkType.BattleMeditation;
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

        public void OnConcentrationTick(NWCreature creature1, NWObject target, int perkLevel, int tick)
        {
            float radiusSize = _.RADIUS_SIZE_SMALL;

            NWCreature targetCreature = _.GetFirstObjectInShape(_.SHAPE_SPHERE, radiusSize, creature1.Location, 1, _.OBJECT_TYPE_CREATURE);
            while (targetCreature.IsValid)
            {
                int amount = 0;

                // Handle effects for differing spellTier values
                switch (perkLevel)
                {
                    case 1:
                        amount = 5;

                        if (_.GetIsReactionTypeHostile(targetCreature, creature1) == 1)
                        {
                            continue;        
                        }                            
                        break;
                    case 2:
                        amount = 10;

                        if (_.GetIsReactionTypeHostile(targetCreature, creature1) == 1)
                        {
                            continue;
                        }
                        break;
                    case 3:
                        amount = 10;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(perkLevel));
                }

                Effect effect = new Effect();

                if (_.GetIsReactionTypeHostile(targetCreature, creature1) == 1)
                {
                    effect = _.EffectACDecrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackDecrease(amount));
                }
                else
                {
                    effect = _.EffectACIncrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackIncrease(amount));
                }

                var creature = targetCreature; // VS recommends copying to another var due to modified closure.
                creature1.AssignCommand(() =>
                {
                    _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, creature, 6.1f);
                });

                targetCreature = _.GetNextObjectInShape(_.SHAPE_SPHERE, radiusSize, creature1.Location, 1, _.OBJECT_TYPE_CREATURE);
            }
            
        }
    }
}
