using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded
{
    public class CrescentMoonAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public CrescentMoonAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CrescentMoon1(builder);
            CrescentMoon2(builder);
            CrescentMoon3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!ItemService.HeavyVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a heavy vibroblade ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            int dmg;
            int dc;
            const float Duration = 3f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 12;
                    dc = 10;
                    break;
                case 2:
                    dmg = 21;
                    dc = 15;
                    break;
                case 3:
                    dmg = 34;
                    dc = 20;
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
            }
            
            CombatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            EnmityService.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private void CrescentMoon1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon1, PerkType.CrescentMoon)
                .Name("Crescent Moon I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CrescentMoon2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon2, PerkType.CrescentMoon)
                .Name("Crescent Moon II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CrescentMoon3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon3, PerkType.CrescentMoon)
                .Name("Crescent Moon III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
