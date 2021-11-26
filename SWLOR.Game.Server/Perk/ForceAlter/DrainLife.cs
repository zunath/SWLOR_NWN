using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
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
            float delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);

            if (target.GetLocalInt("FORCE_DRAIN_IMMUNITY") == 1)
            {
                amount = 0;
            }

            var targetLocation = _.GetLocation(creature);
            var delay = _.GetDistanceBetweenLocations(creature.Location, targetLocation) / 18.0f + 0.35f;

            creature.AssignCommand(() =>
            {
                _.PlaySound("plr_force_absorb");
                _.ApplyEffectToObject(DurationType.Temporary, _.EffectBeam(VisualEffect.Vfx_Beam_Drain, target, BodyNode.Hand), creature, 2.0F);
                _.ApplyEffectToObject(DurationType.Temporary, _.EffectBeam(VisualEffect.Vfx_Beam_Drain, creature, BodyNode.Hand), target, 2.0F);
                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);
                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Reduce_Ability_Score), target);
            });



            // Only apply a heal if caster is not at max HP. Otherwise they'll get unnecessary spam.
            if (creature.CurrentHP < creature.MaxHP)
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(amount), creature);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(amount, DamageType.Negative), target);

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
            }

        }
    }
}
