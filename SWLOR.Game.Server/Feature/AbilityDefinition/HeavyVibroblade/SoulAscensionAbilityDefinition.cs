using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulAscensionAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulAscension(builder);

            return builder.Build();
        }

        private static void SoulAscension(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulAscension1, PerkType.SoulAscension)
                .Name("Soul Ascension")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulAscension, 1800f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulAscensionStatusEffect), 20f);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }
    }
}
