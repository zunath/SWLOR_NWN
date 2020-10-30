using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForceSpark : IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceSpark;
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
            int amount;

            switch (spellTier)
            {
                case 1:
                    amount = 5 + (int)(creature.IntelligenceModifier);
                    break;
                case 2:
                    amount = 5 + (int)(creature.IntelligenceModifier * 1.15);
                    break;
                case 3:
                    amount = 10 + (int)(creature.IntelligenceModifier * 1.25);
                    break;
                case 4:
                    amount = 10 + (int)(creature.IntelligenceModifier * 1.5);
                    break;
                case 5:
                    amount = 15 + (int)(creature.IntelligenceModifier * 2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

            // +/- percent change based on resistance
            var delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);

            creature.AssignCommand(() =>
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectDamage(amount, DamageType.Electrical), target);
            });

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
            }

            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S), target);
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
        }
    }
}
