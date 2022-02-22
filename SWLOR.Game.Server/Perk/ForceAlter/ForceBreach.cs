using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForceBreach: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceBreach;
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
            int damage;
            int intMod = creature.IntelligenceModifier;

            switch (spellTier)
            {
                case 1:
                    damage = 10 + intMod;
                    break;
                case 2:
                    damage = 15 + intMod;
                    break;
                case 3:
                    damage = 20 + ((intMod * 15)/10);
                    break;
                case 4:
                    damage = 25 + ((intMod * 17) / 10);
                    break;
                case 5:
                    damage = 30 + (intMod * 2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, SkillType.ForceAlter, ForceBalanceType.Universal, true);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            damage = damage + (int)(damage * delta);
            var targetLocation = _.GetLocation(creature);
            var delay = _.GetDistanceBetweenLocations(creature.Location, targetLocation) / 18.0f + 0.35f;

            creature.AssignCommand(() =>
            {
                _.PlaySound("plr_force_blast");
                DoFireball(target);               
            });

            creature.DelayAssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage), target);
                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Silence), target);
                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.VFX_IMP_KIN_L), target);
            }, delay);


            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
            }
            
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
            
        }
        public void DoFireball(NWObject target)
        {
            var missile = _.EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball);
            _.ApplyEffectToObject(DurationType.Instant, missile, target);
        }

    }
}
