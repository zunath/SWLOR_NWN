using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class Rage: IPerkHandler
    {
        public PerkType PerkType => PerkType.Rage;
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
            int hpPenalty;
            int attacks;

            // Figure out what the bonuses are for this spell tier.
            switch (spellTier)
            {
                case 1:
                    strBonus = 2;
                    conBonus = 2;
                    hpPenalty = 2;
                    attacks = 0;
                    break;
                case 2:
                    strBonus = 4;
                    conBonus = 4;
                    hpPenalty = 4;
                    attacks = 0;
                    break;
                case 3:
                    strBonus = 6;
                    conBonus = 6;
                    hpPenalty = 6;
                    attacks = 1;
                    break;
                case 4:
                    strBonus = 8;
                    conBonus = 8;
                    hpPenalty = 8;
                    attacks = 1;
                    break;
                case 5:
                    strBonus = 10;
                    conBonus = 10;
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
            var visualEffect = NWScript.EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Red);
            var strEffect = NWScript.EffectAbilityIncrease(AbilityType.Strength, strBonus);
            var conEffect = NWScript.EffectAbilityIncrease(AbilityType.Constitution, conBonus);
            var acEffect = NWScript.EffectACDecrease(0);
            var attackEffect = NWScript.EffectModifyAttacks(attacks);
            var finalEffect = NWScript.EffectLinkEffects(strEffect, conEffect);
            finalEffect = NWScript.EffectLinkEffects(finalEffect, acEffect);
                       
            // Only apply the attack effect if this spell tier increases it.
            if (attacks > 0)
            {
                finalEffect = NWScript.EffectLinkEffects(finalEffect, attackEffect);
            }
            finalEffect = NWScript.TagEffect(finalEffect, "FORCE_ABILITY_RAGE");

            var damageEffect = NWScript.EffectDamage(hpPenalty, DamageType.Divine);

            // Apply both effects.
            creature.AssignCommand(() =>
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, damageEffect, creature.Object);
                NWScript.ApplyEffectToObject(DurationType.Temporary, finalEffect, creature.Object, 6.1f);
                NWScript.ApplyEffectToObject(DurationType.Temporary, visualEffect, creature.Object, 6.1f);
            });
        }
    }
}
