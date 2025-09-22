using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class KnockdownAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public KnockdownAbilityDefinition(ICombatService combatService, IAbilityService abilityService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Knockdown(builder);

            return builder.Build();
        }

        private void Knockdown(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Knockdown, PerkType.Knockdown)
                .Name("Knockdown")
                .HasRecastDelay(RecastGroup.Knockdown, 60f)
                .IsWeaponAbility()
                .RequirementStamina(6)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    const float Duration = 4f;

                    var dc = _combatService.CalculateSavingThrowDC(activator, 12, 0, 0);
                    var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                    if (checkResult == SavingThrowResultType.Failed)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);
                        _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
                    }

                    _combatPointService.AddCombatPoint(activator, target, SkillType.MartialArts, 3);
                    _enmityService.ModifyEnmity(activator, target, 670);
                });
        }
    }
}
