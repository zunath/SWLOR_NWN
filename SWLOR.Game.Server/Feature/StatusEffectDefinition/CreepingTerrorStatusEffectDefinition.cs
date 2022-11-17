using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.AbilityService;

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
            _builder.Create(StatusEffectType.CreepingTerror)
                .Name("Creeping Terror")
                .EffectIcon(EffectIconType.Curse)
                .GrantAction((source, target, length, effectData) =>
                {
                    var level = (int)effectData;
                    var immobilizeLength = level + 1;

                    ApplyEffectToObject(DurationType.Temporary, EffectParalyze(), target, immobilizeLength);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), target);

                    Enmity.ModifyEnmity(source, target, 350);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityModifier(AbilityType.Willpower, source);
                    var level = (int)effectData;
                    var dmg = level * 4 + (willBonus * 1); // (4/8/12)

                    var attackerStat = GetAbilityScore(source, AbilityType.Willpower);
                    var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
                    var attack = Stat.GetAttack(source, AbilityType.Willpower, SkillType.Force);
                    var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
                    var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

                     var player = OBJECT_SELF;
                     if(Ability.IsAbilityToggled(player, AbilityToggleType.ForceStance))
                     dmg = (int)(dmg * 1.60f);

                    AssignCommand(source, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                    });
                    
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), target);

                    var enmity = level * 120 + damage + 6;
                    Enmity.ModifyEnmity(source, target, enmity);
                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }
    }
}
