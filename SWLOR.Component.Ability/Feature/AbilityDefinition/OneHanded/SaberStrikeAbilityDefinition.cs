using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class SaberStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SaberStrikeAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SaberStrike1(builder);
            SaberStrike2(builder);
            SaberStrike3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (ItemService.LightsaberBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A lightsaber must be equipped in your right hand to use this ability.";
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;
            float breachTime;
            const string EffectTag = "SABER_STRIKE";

            switch (level)
            {
                default:
                case 1:
                    dmg = 6;
                    dc = 10;
                    breachTime = 30f;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    breachTime = 60f;
                    break;
                case 3:
                    dmg = 22;
                    dc = 20;
                    breachTime = 60f;
                    break;
            }

            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);
            EnmityService.ModifyEnmity(activator, target, 650);

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            var stat = AbilityType.Perception;
            if (AbilityService.IsAbilityToggled(activator, AbilityToggleType.StrongStyleLightsaber))
            {
                stat = AbilityType.Might;
            }

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            var attack = StatService.GetAttack(activator, stat, SkillType.OneHanded);
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

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                RemoveEffectByTag(target, EffectTag);
                var eBreach = TagEffect(EffectACDecrease(2), EffectTag);
                ApplyEffectToObject(DurationType.Temporary, eBreach, target, breachTime);
            }
            
            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.RiotBlade));

            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void SaberStrike1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike1, PerkType.SaberStrike)
                .Name("Saber Strike I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.SaberStrike, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void SaberStrike2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike2, PerkType.SaberStrike)
                .Name("Saber Strike II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.SaberStrike, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void SaberStrike3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike3, PerkType.SaberStrike)
                .Name("Saber Strike III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.SaberStrike, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
