//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        private static void MindTrick1(AbilityBuilder builder)
        {
            builder.Create(FeatType.MindTrick1, PerkType.MindTrick)
                .Name("Mind Trick I")
                .HasRecastDelay(RecastGroup.MindTrick, 60f)
                .HasActivationDelay(2.0f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .UsesAnimation(Animation.LoopingConjure1)
                .HasCustomValidation(Validation)
                .IsConcentrationAbility(StatusEffectType.MindTrick1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating();
        }

        private static void MindTrick2(AbilityBuilder builder)
        {
            builder.Create(FeatType.MindTrick2, PerkType.MindTrick)
                .Name("Mind Trick II")
                .HasRecastDelay(RecastGroup.MindTrick, 60f)
                .HasActivationDelay(2.0f)
                .HasMaxRange(15.0f)
                .RequirementFP(5)
                .UsesAnimation(Animation.LoopingConjure1)
                .HasCustomValidation(Validation)
                .IsConcentrationAbility(StatusEffectType.MindTrick2)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating();
        }
    }
}