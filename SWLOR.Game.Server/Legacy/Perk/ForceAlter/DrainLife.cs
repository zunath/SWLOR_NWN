using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Perk.ForceAlter
{
    public class DrainLife: IPerkHandler
    {
        public PerkType PerkType => PerkType.DrainLife;
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!oTarget.IsCreature)
                return "This ability can only be used on living creatures.";
            NWCreature targetCreature = oTarget.Object;
            if (targetCreature.RacialType == RacialType.Robot)
                return "This ability cannot be used on droids.";

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
                    amount = 5;
                    break;
                case 2:
                    amount = 6;
                    break;
                case 3:
                    amount = 7;
                    break;
                case 4:
                    amount = 8;
                    break;
                case 5:
                    amount = 10;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

            // +/- percent change based on resistance
            var delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);

            if (target.GetLocalInt("FORCE_DRAIN_IMMUNITY") == 1)
            {
                amount = 0;
            }

            creature.AssignCommand(() =>
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectDamage(amount, DamageType.Negative), target);
            });

            // Only apply a heal if caster is not at max HP. Otherwise they'll get unnecessary spam.
            if (creature.CurrentHP < creature.MaxHP)
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(amount), creature);
            }

            if(creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
            }

            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectVisualEffect(VisualEffect.Vfx_Com_Hit_Negative), target);
        }
    }
}
