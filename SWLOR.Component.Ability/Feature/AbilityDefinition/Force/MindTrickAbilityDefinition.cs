using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class MindTrickAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public MindTrickAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            MindTrick1(builder);
            MindTrick2(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var race = GetRacialType(target);
            if (race == RacialType.Cyborg || 
                race == RacialType.Robot ||
                race == RacialType.Droid)
            {
                return "Mind trick does not work on this creature.";
            }
            
            return string.Empty;
        }

        private void ApplyMindTrick(uint activator, uint target)
        {
            var race = GetRacialType(target);
            if (activator == target ||
                race == RacialType.Cyborg ||
                race == RacialType.Robot ||
                race == RacialType.Droid)
            {
                return;
            }

            var dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Will, 12);
            const string EffectTag = "StatusEffectType.MindTrick";
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                var effect = EffectConfused();
                effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffectType.Vfx_Imp_Confusion_S));
                effect = TagEffect(effect, EffectTag);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6f);
            }
            CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            EnmityService.ModifyEnmity(activator, target, 400);
        }

        private void MindTrick1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MindTrick1, PerkType.MindTrick)
                .Name("Mind Trick I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.MindTrick, 60f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasCustomValidation(Validation)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyMindTrick(activator, target);
                });
        }

        private void MindTrick2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MindTrick2, PerkType.MindTrick)
                .Name("Mind Trick II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.MindTrick, 60f)
                .HasMaxRange(15.0f)
                .RequirementFP(5)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasCustomValidation(Validation)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float Radius = RadiusSize.Medium;
                    ApplyMindTrick(activator, target);
                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(ShapeType.Sphere, Radius, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (GetIsReactionTypeHostile(targetCreature, activator) &&
                            target != targetCreature)
                        {
                            ApplyMindTrick(activator, targetCreature);
                        }
                        targetCreature = GetNextObjectInShape(ShapeType.Sphere, Radius, GetLocation(target), true);
                    }
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
    }
}
