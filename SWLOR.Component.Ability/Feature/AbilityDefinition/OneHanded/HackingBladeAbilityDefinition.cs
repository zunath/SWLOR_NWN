using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class HackingBladeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IStatusEffectService _statusEffectService;

        public HackingBladeAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            HackingBlade1(builder);
            HackingBlade2(builder);
            HackingBlade3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (_itemService.VibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;

            }
            else
                return "A vibroblade must be equipped in your right hand to use this ability.";
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 6;
                    dc = 10;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    break;
                case 3:
                    dmg = 22;
                    dc = 20;
                    break;
            }

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            _combatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                _statusEffectService.Apply(activator, target, StatusEffectType.Bleed, 60f);
            }
            
            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void HackingBlade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade1, PerkType.HackingBlade)
                .Name("Hacking Blade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void HackingBlade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade2, PerkType.HackingBlade)
                .Name("Hacking Blade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void HackingBlade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade3, PerkType.HackingBlade)
                .Name("Hacking Blade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.HackingBlade, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
