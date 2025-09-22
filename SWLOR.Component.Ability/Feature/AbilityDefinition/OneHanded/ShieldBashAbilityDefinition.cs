//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class ShieldBashAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public ShieldBashAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, IAbilityService abilityService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ShieldBash1(builder);
            ShieldBash2(builder);
            ShieldBash3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.LeftHand, activator);
            var leftHandType = GetBaseItemType(weapon);
            
            if (_itemService.ShieldBaseItemTypes.Contains(leftHandType))
            {
                return string.Empty;
            }
            else
                return "A shield must be equipped in your left hand to use this ability.";
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            const float Duration = 3f;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    dc = 12;
                    break;
                case 2:
                    dmg = 16;
                    dc = 14;
                    break;
                case 3:
                    dmg = 24;
                    dc = 16;
                    break;
            }


            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            _combatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var might = GetAbilityScore(activator, AbilityType.Might);
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = _combatService.CalculateDamage(attack, dmg, might, defense, vitality, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Will, dc, AbilityType.Might);
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectDazed(), target, Duration);
                _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Dazed);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.ShieldWall));

            _enmityService.ModifyEnmity(activator, target, 400 * level + damage);
        }

        private void ShieldBash1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShieldBash1, PerkType.ShieldBash)
                .Name("Shield Bash I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void ShieldBash2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShieldBash2, PerkType.ShieldBash)
                .Name("Shield Bash II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void ShieldBash3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShieldBash3, PerkType.ShieldBash)
                .Name("Shield Bash III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ShieldBash, 60f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
