using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class MindTrickAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            MindTrick1(builder);
            MindTrick2(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            if (GetRacialType(target) == RacialType.Cyborg || GetRacialType(target) == RacialType.Robot)
            {
                return "Mind trick does not work on this creature.";
            }
            else return string.Empty;
        }

        private static void ApplyMindTrick(uint activator, uint target)
        {
            if (activator == target ||
                GetRacialType(target) == RacialType.Cyborg &&
                GetRacialType(target) == RacialType.Robot)
            {
                return;
            }

            if (!Ability.GetAbilityResisted(activator, target, "Mind Trick", AbilityType.Willpower))
            {
                var effect = EffectConfused();
                effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S));
                effect = TagEffect(effect, "StatusEffectType.MindTrick");
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6f);
            }
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            Enmity.ModifyEnmity(activator, target, 190);
        }

        private static void MindTrick1(AbilityBuilder builder)
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

        private static void MindTrick2(AbilityBuilder builder)
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
                    const float radiusSize = RadiusSize.Medium;
                    ApplyMindTrick(activator, target);
                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if(GetIsReactionTypeHostile(targetCreature, activator))
                            ApplyMindTrick(activator, targetCreature);
                        targetCreature = GetNextObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    }
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
    }
}