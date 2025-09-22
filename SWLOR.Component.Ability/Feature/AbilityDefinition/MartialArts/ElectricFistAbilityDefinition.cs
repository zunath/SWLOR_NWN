//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.MartialArts
{
    public class ElectricFistAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly IEnmityService _enmityService;

        public ElectricFistAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IStatusEffectService statusEffectService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _statusEffectService = statusEffectService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ElectricFist1(builder);
            ElectricFist2(builder);
            ElectricFist3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.KatarBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a katar ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;
            float duration;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    dc = 10;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 17;
                    dc = 15;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 24;
                    dc = 20;
                    duration = 60f;
                    break;
            }

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            _combatPointService.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);

            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                _statusEffectService.Apply(activator, target, StatusEffectType.Shock, duration);
            }

            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void ElectricFist1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ElectricFist1, PerkType.ElectricFist)
                .Name("Electric Fist I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ElectricFist, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void ElectricFist2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ElectricFist2, PerkType.ElectricFist)
                .Name("Electric Fist II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ElectricFist, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void ElectricFist3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ElectricFist3, PerkType.ElectricFist)
                .Name("Electric Fist III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ElectricFist, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
