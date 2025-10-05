using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class SoothePetAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SoothePetAbilityDefinition(IServiceProvider serviceProvider) 
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SoothePet(builder);

            return builder.Build();
        }

        private void SoothePet(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SoothePet, PerkType.SoothePet)
                .Name("Soothe Pet")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Tame, 60f * 3)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
                    {
                        return "Only players may use this ability.";
                    }

                    var beast = GetAssociate(AssociateType.Henchman, activator);
                    if (!BeastMastery.IsPlayerBeast(beast))
                    {
                        return "You do not have an active beast.";
                    }

                    if (GetDistanceBetween(beast, activator) >= 15f)
                    {
                        return "Your beast is too far away.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, activator);

                    RemoveEffect(beast, 
                        EffectScriptType.Disease, 
                        EffectScriptType.Poison, 
                        EffectScriptType.Confused,
                        EffectScriptType.Paralyze,
                        EffectScriptType.Stunned,
                        EffectScriptType.Sleep,
                        EffectScriptType.Slow);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Healing_G), beast);
                    EnmityService.ModifyEnmityOnAll(activator, 500);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}
