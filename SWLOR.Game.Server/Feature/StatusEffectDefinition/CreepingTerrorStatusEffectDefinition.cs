using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

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

                switch (level)
                {
                    default:
                    case 1:
                        dmg = 8;
                        break;
                    case 2:
                        dmg = 12;
                        break;
                    case 3:
                        dmg = 16;
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

                var enmity = level * 120 + damage + 6;
                Enmity.ModifyEnmity(source, target, enmity);
                CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
            }

            _builder.Create(StatusEffectType.CreepingTerror)
                .Name("Creeping Terror")
                .EffectIcon(EffectIconType.Curse)
                .GrantAction((source, target, length, effectData) =>
                {
                    var level = (int)effectData;
                    const float Duration = 2f;
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
                        ApplyEffectToObject(DurationType.Temporary, EffectParalyze(), target, Duration);
                        Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Paralysis);
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
