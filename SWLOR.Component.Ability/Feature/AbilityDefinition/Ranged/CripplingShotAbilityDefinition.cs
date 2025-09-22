//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Ranged
{
    public class CripplingShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;

        public CripplingShotAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CripplingShot1(builder);
            CripplingShot2(builder);
            CripplingShot3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.RifleBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a rifle ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            const float Duration = 6f;
            int dc;

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

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.Ranged);

            _combatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            var attackerStat = _combatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statService.GetAttack(activator, AbilityType.Perception, SkillType.Ranged);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            dc = _combatService.CalculateSavingThrowDC(activator, dc, 0, 0);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(99), target, Duration);
            }

            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private void CripplingShot1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot1, PerkType.CripplingShot)
                .Name("Crippling Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CripplingShot2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot2, PerkType.CripplingShot)
                .Name("Crippling Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CripplingShot3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot3, PerkType.CripplingShot)
                .Name("Crippling Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CripplingShot, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
