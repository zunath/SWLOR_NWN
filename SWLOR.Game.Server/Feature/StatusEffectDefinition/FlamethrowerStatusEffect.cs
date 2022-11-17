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
    public class FlameThrowerEffectDefinition : IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            FlameThrower();

            return _builder.Build();
        }

        private void FlameThrower()
        {
            _builder.Create(StatusEffectType.FlameThrower)
                .Name("Flame Thrower")
                .EffectIcon(EffectIconType.Burning)
                .TickAction((source, target, effectData) =>
                {
                    var perbonus = GetAbilityModifier(AbilityType.Perception, source);                   
                    var bonus = perbonus * 7;                    
                    var dmg = 15 +  bonus;

                    var attackerStat = GetAbilityScore(source, AbilityType.Perception);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var attack = Stat.GetAttack(source, AbilityType.Perception, SkillType.Devices);
                    var defense = Stat.GetDefense(target, CombatDamageType.Fire, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

                 
                    AssignCommand(source, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                    });

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Fire), target);

                });
        }
    }
}