using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class SnarlGrowlAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SnarlGrowlAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Snarl(builder);
            Growl(builder);

            return builder.Build();
        }

        private string Validation(uint activator)
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
        }

        private void Snarl(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Snarl, PerkType.Snarl)
                .Name("Snarl")
                .Level(1)
                .HasRecastDelay(RecastGroupType.SnarlGrowl, 30f)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, activator);
                    var masterEnmity = EnmityService.GetEnmityTowardsAllEnemies(activator);

                    foreach (var (enemy, amount) in masterEnmity)
                    {
                        var halfAmount = amount / 2;
                        EnmityService.ModifyEnmity(activator, enemy, -halfAmount);
                        EnmityService.ModifyEnmity(beast, enemy, halfAmount);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Blood_Crt_Red), activator);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Blood_Crt_Yellow), beast);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }

        private void Growl(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Growl, PerkType.Growl)
                .Name("Growl")
                .Level(1)
                .HasRecastDelay(RecastGroupType.SnarlGrowl, 30f)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, activator);
                    var beastEnmity = EnmityService.GetEnmityTowardsAllEnemies(beast);

                    foreach (var (enemy, amount) in beastEnmity)
                    {
                        var halfAmount = amount / 2;
                        EnmityService.ModifyEnmity(activator, enemy, halfAmount);
                        EnmityService.ModifyEnmity(beast, enemy, -halfAmount);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Blood_Crt_Red), beast);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Blood_Crt_Yellow), activator);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}
