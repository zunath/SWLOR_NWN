using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            const float MaxDistance = 5.0f;
            var nth = 1;
            int amount;

            switch (perkLevel)
            {
                case 1:
                    amount = 5;
                    break;
                case 2:
                case 3:
                    amount = 10;
                    break;
                default: return;
            }

            // Penalize the caster
            var effect = EffectACDecrease(0);
            effect = EffectLinkEffects(effect, EffectAttackDecrease(amount));
            ApplyEffectToObject(DurationType.Temporary, effect, creature, 6.1f);


            NWCreature targetCreature = GetNearestCreature(CreatureType.IsAlive, 1, creature, nth);
            while (targetCreature.IsValid && GetDistanceBetween(creature, targetCreature) <= MaxDistance)
            {
                // Skip the caster, if they get picked up.
                if (targetCreature == creature)
                {
                    nth++;
                    targetCreature = GetNearestCreature(CreatureType.IsAlive, 1, creature, nth);
                    continue;
                }

                // Handle effects for differing spellTier values
                switch (perkLevel)
                {
                    case 1:
                        amount = 5;

                        if (GetIsReactionTypeHostile(targetCreature, creature) == true)
                        {
                            nth++;
                            targetCreature = GetNearestCreature(CreatureType.IsAlive, 1, creature, nth);
                            continue;        
                        }                            
                        break;
                    case 2:
                        amount = 10;

                        if (GetIsReactionTypeHostile(targetCreature, creature) == true)
                        {
                            nth++;
                            targetCreature = GetNearestCreature(CreatureType.IsAlive, 1, creature, nth);
                            continue;
                        }
                        break;
                    case 3:
                        amount = 10;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(perkLevel));
                }

                if (GetIsReactionTypeHostile(targetCreature, creature) == true)
                {
                    effect = EffectACDecrease(0);
                    effect = EffectLinkEffects(effect, EffectAttackDecrease(amount));
                }
                else
                {
                    effect = EffectACIncrease(0);
                    effect = EffectLinkEffects(effect, EffectAttackIncrease(amount));
                }

                ApplyEffectToObject(DurationType.Temporary, effect, targetCreature, 6.1f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Dur_Magic_Resistance), targetCreature);
                
                nth++;
                targetCreature = GetNearestCreature(CreatureType.IsAlive, 1, creature, nth);
            }
            
        }
    }
}
