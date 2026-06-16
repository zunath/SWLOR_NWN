using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class TauntingDeflectionAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.TauntingDeflection1, PerkType.TauntingDeflection)
                .Name("Taunting Deflection")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.TauntingDeflection, 30f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(TauntingDeflectionStatusEffect), 30f);
                    foreach (var hostile in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(activator), 5f, 0))
                    {
                        Enmity.ModifyEnmity(activator, hostile, 850);
                    }
                })
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);

            return builder.Build();
        }
    }
}
