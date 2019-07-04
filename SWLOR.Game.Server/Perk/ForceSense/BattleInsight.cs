using System;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class BattleInsight: IPerkHandler
    {
        public PerkType PerkType => PerkType.BattleInsight;
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
            //float radiusSize = _.RADIUS_SIZE_SMALL;
            int count = 1;
            const float MaxDistance = _.RADIUS_SIZE_SMALL;
            int nth = 1;

            NWCreature targetCreature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, creature, nth);
            while (targetCreature.IsValid && GetDistanceBetween(creature, targetCreature) <= MaxDistance)
            {                
                int amount;

                // Handle effects for differing spellTier values
                switch (perkLevel)
                {
                    case 1:
                        amount = 5;

                        if (_.GetIsReactionTypeHostile(targetCreature, creature) == 1)
                        {
                            nth++;
                            targetCreature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, creature, nth);
                            continue;        
                        }                            
                        break;
                    case 2:
                        amount = 10;

                        if (_.GetIsReactionTypeHostile(targetCreature, creature) == 1)
                        {
                            nth++;
                            targetCreature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, creature, nth);
                            continue;
                        }
                        break;
                    case 3:
                        amount = 10;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(perkLevel));
                }

                Effect effect;

                if (_.GetIsReactionTypeHostile(targetCreature, creature) == 1)
                {
                    effect = _.EffectACDecrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackDecrease(amount));
                }
                else
                {
                    effect = _.EffectACIncrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackIncrease(amount));
                }

                var tempCreature = targetCreature; // VS recommends copying to another var due to modified closure.
                creature.AssignCommand(() =>
                {
                    _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, tempCreature, 6.1f);
                    _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_DUR_MAGIC_RESISTANCE), tempCreature);
                });

                nth++;
                targetCreature = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, creature, nth);
            }
            
        }
    }
}
