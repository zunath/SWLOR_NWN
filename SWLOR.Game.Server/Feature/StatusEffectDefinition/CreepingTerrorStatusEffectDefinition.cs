using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class CreepingTerrorStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

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
                var attack = Stat.GetAttack(source, AbilityType.Willpower, SkillType.Force);
                var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
                var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

                AssignCommand(source, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                });

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), target);

                var enmity = level * 50 + damage + 6;
                Enmity.ModifyEnmity(source, target, enmity);
                CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
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


                    dc = Combat.CalculateSavingThrowDC(source, SavingThrow.Will, dc);
                    var checkResult = WillSave(target, dc, SavingThrowType.None, source);

                    if (checkResult == SavingThrowResultType.Failed)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectEntangle(), target, Duration);
                        Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Entangle);
                    }

                    ApplyDamage(source, target, level);
                    Enmity.ModifyEnmity(source, target, 350);
                })
                .TickAction((source, target, effectData) =>
                {
                    var level = (int)effectData;
                    ApplyDamage(source, target, level);
                });
        }
    }
}