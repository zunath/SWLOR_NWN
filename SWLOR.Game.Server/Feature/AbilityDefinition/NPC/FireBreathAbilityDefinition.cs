using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class FireBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FireBreath();

            return _builder.Build();
        }

        private void FireBreath()
        {
            _builder.Create(FeatType.FireBreath, PerkType.Invalid)
                .Name("Fire Breath")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.FireBreath, 60f)
                .IsCastedAbility()
                .IsHostileAbility()
                .RequirementStamina(6)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var dmg = 3;
                    
                    var coneTarget = GetFirstObjectInShape(Shape.SpellCone, 14.0f, location);
                    while (GetIsObjectValid(coneTarget))
                    {
                        if (GetIsEnemy(coneTarget, activator))
                        {
                            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                            var defenderStat = GetAbilityScore(coneTarget, AbilityType.Vitality);
                            var damage = Combat.CalculateDamage(
                                attack, 
                                dmg, 
                                attackerStat, 
                                defense, 
                                defenderStat, 
                                0);

                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Hit_Fire), coneTarget);
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), coneTarget);
                        }

                        coneTarget = GetNextObjectInShape(Shape.SpellCone, 14.0f, location);
                    }

                });
        }
    }
}
