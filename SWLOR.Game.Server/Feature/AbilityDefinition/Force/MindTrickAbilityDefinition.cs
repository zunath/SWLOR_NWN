using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Shared.Core.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class MindTrickAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public MindTrickAbilityDefinition(ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            MindTrick1(builder);
            MindTrick2(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
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

            var dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Will, 12);
            const string EffectTag = "StatusEffectType.MindTrick";
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                var effect = EffectConfused();
                effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S));
                effect = TagEffect(effect, EffectTag);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6f);
            }
            _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            _enmityService.ModifyEnmity(activator, target, 400);
        }

        private void MindTrick1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MindTrick1, PerkType.MindTrick)
                .Name("Mind Trick I")
                .Level(1)
                .HasRecastDelay(RecastGroup.MindTrick, 60f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .UsesAnimation(Animation.LoopingConjure1)
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
                .HasRecastDelay(RecastGroup.MindTrick, 60f)
                .HasMaxRange(15.0f)
                .RequirementFP(5)
                .UsesAnimation(Animation.LoopingConjure1)
                .HasCustomValidation(Validation)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float Radius = RadiusSize.Medium;
                    ApplyMindTrick(activator, target);
                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, Radius, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (GetIsReactionTypeHostile(targetCreature, activator) &&
                            target != targetCreature)
                        {
                            ApplyMindTrick(activator, targetCreature);
                        }
                        targetCreature = GetNextObjectInShape(Shape.Sphere, Radius, GetLocation(target), true);
                    }
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
    }
}
