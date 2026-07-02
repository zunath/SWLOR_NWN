using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class RampartAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Rampart(builder);

            return builder.Build();
        }

        private static void Rampart(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Rampart1, PerkType.Rampart)
                .Name("Rampart")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ShieldWall)
                .HasRecastDelay(RecastGroup.Rampart, 60f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ApplyStatusToNearbyParty(
                        activator,
                        typeof(RampartStatusEffect),
                        60f,
                        true,
                        VisualEffect.Vfx_Imp_Ac_Bonus);
                })
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }
    }
}
