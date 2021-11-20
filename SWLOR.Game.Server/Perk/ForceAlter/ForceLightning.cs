﻿using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForceLightning: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceLightning;
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
            int amount;

            switch (spellTier)
            {
                case 1:
                    amount = 2 + (int)(creature.IntelligenceModifier);
                    break;
                case 2:
                    amount = 4 + (int)(creature.IntelligenceModifier * 1.15);
                    break;
                case 3:
                    amount = 6 + (int)(creature.IntelligenceModifier * 1.25);
                    break;
                case 4:
                    amount = 8 + (int)(creature.IntelligenceModifier * 1.5);
                    break;
                case 5:
                    amount = 10 + (int)(creature.IntelligenceModifier * 2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);
            
            creature.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(amount, DamageType.Electrical), target);
            });

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S), target);
        }
    }
}
