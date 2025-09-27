using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class CreepingTerrorStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();
        private readonly ICombatService _combatService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IStatService _statService;
        private readonly IEnmityService _enmityService;

        public CreepingTerrorStatusEffectDefinition(
            ICombatService combatService, 
            IAbilityService abilityService, 
            ICombatPointService combatPointService,
            IStatService statService,
            IEnmityService enmityService)
        {
            _combatService = combatService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _statService = statService;
            _enmityService = enmityService;
        }

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            CreepingTerror();

            return _builder.Build();
        }

        private void CreepingTerror()
        {
            void ApplyDamage(uint source, uint target, int level)
            {
                int dmg;
                var willBonus = GetAbilityScore(source, AbilityType.Willpower);

                switch (level)
                {
                    default:
                    case 1:
                        dmg = willBonus * 1 / 2;
                        break;
                    case 2:
                        dmg = willBonus;
                        break;
                    case 3:
                        dmg = willBonus * 3 / 2;
                        break;
                }

                var attackerStat = GetAbilityScore(source, AbilityType.Willpower);
                var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
                var attack = _statService.GetAttack(source, AbilityType.Willpower, SkillType.Force);
                var defense = _statService.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
                var damage = _combatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

                AssignCommand(source, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                });

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Poison_S), target);

                var enmity = level * 50 + damage + 6;
                _enmityService.ModifyEnmity(source, target, enmity);
                _combatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
            }

            _builder.Create(StatusEffectType.CreepingTerror)
                .Name("Creeping Terror")
                .EffectIcon(EffectIconType.Curse)
                .GrantAction((source, target, length, effectData) =>
                {
                    var level = (int)effectData;
                    const float Duration = 6f;
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
                    }


                    dc = _combatService.CalculateSavingThrowDC(source, SavingThrowCategoryType.Will, dc);
                    var checkResult = WillSave(target, dc, SavingThrowType.None, source);

                    if (checkResult == SavingThrowResultType.Failed)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectEntangle(), target, Duration);
                        _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Entangle);
                    }

                    ApplyDamage(source, target, level);
                    _enmityService.ModifyEnmity(source, target, 350);
                })
                .TickAction((source, target, effectData) =>
                {
                    var level = (int)effectData;
                    ApplyDamage(source, target, level);
                });
        }
    }
}
