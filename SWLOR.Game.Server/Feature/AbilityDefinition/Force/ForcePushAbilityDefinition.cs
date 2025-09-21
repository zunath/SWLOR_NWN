using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForcePushAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public ForcePushAbilityDefinition(ICombatService combatService, IAbilityService abilityService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForcePush1(builder);
            ForcePush2(builder);
            ForcePush3(builder);
            ForcePush4(builder);

            return builder.Build();
        }
        
        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            const float BaseDuration = 2f;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dc = 8;
                    break;
                case 2:
                    dc = 12;
                    break;
                case 3:
                    dc = 14;
                    break;
                case 4:
                    dc = 16;
                    break;
            }

            var willpowerBonus = 0.5f * GetAbilityModifier(AbilityType.Willpower, activator);

            dc = _combatService.CalculateSavingThrowDC(activator, dc, 0, 0);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            var duration = BaseDuration + willpowerBonus;

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, duration);

                _abilityService.ApplyTemporaryImmunity(target, duration, ImmunityType.Knockdown);
            }
            else if (checkResult == SavingThrowResultType.Success)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, duration);
            }

            _enmityService.ModifyEnmityOnAll(activator, level * 150);

            _combatPointService.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void ForcePush1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush1, PerkType.ForcePush)
                .Name("Force Push I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void ForcePush2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush2, PerkType.ForcePush)
                .Name("Force Push II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void ForcePush3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush3, PerkType.ForcePush)
                .Name("Force Push III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void ForcePush4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush4, PerkType.ForcePush)
                .Name("Force Push IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }
    }
}
