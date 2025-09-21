using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class TalonAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;

        public TalonAbilityDefinition(ICombatService combatService, IStatService statService)
        {
            _combatService = combatService;
            _statService = statService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Talon(builder);

            return builder.Build();
        }

        private void Talon(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Talon, PerkType.Invalid)
                .Name("Talon")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.Talon, 40f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const int DMG = 1;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                    var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        DMG, 
                        attackerStat, 
                        defense, 
                        defenderStat, 
                        0);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Spark_Medium), target);
                });
        }
    }
}
