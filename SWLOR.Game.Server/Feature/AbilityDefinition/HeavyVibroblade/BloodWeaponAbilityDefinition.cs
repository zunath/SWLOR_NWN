using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class BloodWeaponAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BloodWeapon(builder);

            return builder.Build();
        }

        private static void BloodWeapon(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BloodWeapon1, PerkType.BloodWeapon)
                .Name("Blood Weapon")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BloodWeapon, 120f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(BloodWeaponStatusEffect), 20f);
                })
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(14);
        }
    }
}
