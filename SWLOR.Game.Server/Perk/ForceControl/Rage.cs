﻿using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class Rage: IPerkHandler
    {
        public PerkType PerkType => PerkType.Rage;
        public string Name => "Rage";
        public bool IsActive => true;
        public string Description => "Increases STR and CON at the cost of AC and HP damage each round.  At higher ranks grants additional attacks, that do not stack with Force Speed.";
        public PerkCategoryType Category => PerkCategoryType.ForceControl;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Rage;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 10;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Dark;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
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
            ApplyEffect(creature, target, spellTier);
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
            ApplyEffect(creature, target, spellTier);
        }

        private void ApplyEffect(NWCreature creature, NWObject target, int spellTier)
        {
            int strBonus;
            int conBonus;
            int acPenalty;
            int hpPenalty;
            int attacks;

            // Figure out what the bonuses are for this spell tier.
            switch (spellTier)
            {
                case 1:
                    strBonus = 2;
                    conBonus = 2;
                    acPenalty = 2;
                    hpPenalty = 2;
                    attacks = 0;
                    break;
                case 2:
                    strBonus = 4;
                    conBonus = 4;
                    acPenalty = 2;
                    hpPenalty = 4;
                    attacks = 0;
                    break;
                case 3:
                    strBonus = 6;
                    conBonus = 6;
                    acPenalty = 4;
                    hpPenalty = 6;
                    attacks = 1;
                    break;
                case 4:
                    strBonus = 8;
                    conBonus = 8;
                    acPenalty = 4;
                    hpPenalty = 8;
                    attacks = 1;
                    break;
                case 5:
                    strBonus = 10;
                    conBonus = 10;
                    acPenalty = 6;
                    hpPenalty = 10;
                    attacks = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            // If creature can't afford the HP hit for this tick, bail out early.
            if (target.CurrentHP < hpPenalty)
            {
                AbilityService.EndConcentrationEffect(creature);
                creature.SendMessage("Concentration effect has ended because you do not have enough HP to maintain it.");
                return;
            }

            // Build a linked effect which handles applying these bonuses and penalties.
            Effect visualEffect = _.EffectVisualEffect(Vfx.Vfx_Dur_Aura_Red);
            Effect strEffect = _.EffectAbilityIncrease(Ability.Strength, strBonus);
            Effect conEffect = _.EffectAbilityIncrease(Ability.Constitution, conBonus);
            Effect acEffect = _.EffectACDecrease(acPenalty);
            Effect attackEffect = _.EffectModifyAttacks(attacks);
            Effect finalEffect = _.EffectLinkEffects(strEffect, conEffect);
            finalEffect = _.EffectLinkEffects(finalEffect, acEffect);
                       
            // Only apply the attack effect if this spell tier increases it.
            if (attacks > 0)
            {
                finalEffect = _.EffectLinkEffects(finalEffect, attackEffect);
            }
            finalEffect = _.TagEffect(finalEffect, "FORCE_ABILITY_RAGE");

            Effect damageEffect = _.EffectDamage(hpPenalty);

            // Apply both effects.
            creature.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, damageEffect, creature.Object);
                _.ApplyEffectToObject(DurationType.Temporary, finalEffect, creature.Object, 6.1f);
                _.ApplyEffectToObject(DurationType.Temporary, visualEffect, creature.Object, 6.1f);
            });
        }
    }
}
