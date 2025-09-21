using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class KnockdownAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly CombatPoint _combatPoint;
        private readonly ICombatService _combatService;
        private readonly IAbilityService _abilityService;

        public KnockdownAbilityDefinition(CombatPoint combatPoint, ICombatService combatService, IAbilityService abilityService)
        {
            _combatPoint = combatPoint;
            _combatService = combatService;
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Knockdown();

            return _builder.Build();
        }

        private void Knockdown()
        {
            _builder.Create(FeatType.Knockdown, PerkType.Knockdown)
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

                    _combatPoint.AddCombatPoint(activator, target, SkillType.MartialArts, 3);
                    Enmity.ModifyEnmity(activator, target, 670);
                });
        }
    }
}
