//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceBurstAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceBurst1(builder);
            ForceBurst2(builder);
            ForceBurst3(builder);
            ForceBurst4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0.0f;

            switch (level)
            {
                case 1:
                    dmg = 6.0f;
                    break;
                case 2:
                    dmg = 8.5f;
                    break;
                case 3:
                    dmg = 12.0f;
                    break;
                case 4:
                    dmg = 13.5f;
                    break;
            }
            
            var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
            var defense = Stat.GetDefense(target, CombatDamageType.Force);
            var targetWillpower = GetAbilityModifier(AbilityType.Willpower, target);
            var damage = Combat.CalculateDamage(dmg, willpower, defense, targetWillpower, false);
            var delay = GetDistanceBetweenLocations(GetLocation(activator), targetLocation) / 18.0f + 0.35f;

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                PlaySound("plr_force_blast");
                DoFireball(target);
            });

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Silence), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.VFX_IMP_KIN_L), target);
            });

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceBurst1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst1, PerkType.ForceBurst)
                .Name("Force Burst I")
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .RequirementFP(2)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBurst2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst2, PerkType.ForceBurst)
                .Name("Force Burst II")
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .RequirementFP(3)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBurst3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst3, PerkType.ForceBurst)
                .Name("Force Burst III")
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .RequirementFP(4)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBurst4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst4, PerkType.ForceBurst)
                .Name("Force Burst IV")
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .RequirementFP(5)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }
        private static void DoFireball(uint target)
        {
            var missile = EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball);
            ApplyEffectToObject(DurationType.Instant, missile, target);
        }
    }
}