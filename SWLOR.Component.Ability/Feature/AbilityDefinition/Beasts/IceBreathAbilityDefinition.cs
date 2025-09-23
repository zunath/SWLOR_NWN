using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class IceBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly IEnmityService _enmityService;

        public IceBreathAbilityDefinition(
            ICombatService combatService, 
            IStatService statService, 
            IStatusEffectService statusEffectService, 
            IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _statusEffectService = statusEffectService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            IceBreath1(builder);
            IceBreath2(builder);
            IceBreath3(builder);
            IceBreath4(builder);
            IceBreath5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc, int level)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Icestorm), targetLocation);
            });

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.Invalid);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = _statService.GetDefense(target, CombatDamageType.Ice, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = _combatService.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Cold);
                    _enmityService.ModifyEnmity(activator, target, 220);

                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                        });

                        dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Reflex, baseDC);
                        var checkResult = ReflexSave(targetCopy, dc, SavingThrowType.None, activator);
                        if (checkResult == SavingThrowResultType.Failed)
                        {
                            _statusEffectService.Apply(activator, targetCopy, StatusEffectType.Freezing, 30f, level);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void IceBreath1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IceBreath1, PerkType.IceBreath)
                .Name("Ice Breath I")
                .Level(1)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, -1, level);
                });
        }
        private void IceBreath2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IceBreath2, PerkType.IceBreath)
                .Name("Ice Breath II")
                .Level(2)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, -1, level);
                });
        }
        private void IceBreath3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IceBreath3, PerkType.IceBreath)
                .Name("Ice Breath III")
                .Level(3)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, 8, level);
                });
        }
        private void IceBreath4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IceBreath4, PerkType.IceBreath)
                .Name("Ice Breath IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 20, 12, level);
                });
        }
        private void IceBreath5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IceBreath5, PerkType.IceBreath)
                .Name("Ice Breath V")
                .Level(5)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 24, 14, level);
                });
        }
    }
}
