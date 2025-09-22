using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class SaberStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public SaberStrikeAbilityDefinition(IItemService itemService, IAbilityService abilityService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _abilityService = abilityService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SaberStrike1(builder);
            SaberStrike2(builder);
            SaberStrike3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (_itemService.LightsaberBaseItemTypes.Contains(rightHandType))
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

            _combatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);
            _enmityService.ModifyEnmity(activator, target, 650);

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            var stat = AbilityType.Perception;
            if (_abilityService.IsAbilityToggled(activator, AbilityToggleType.StrongStyleLightsaber))
            {
                stat = AbilityType.Might;
            }

            var attackerStat = _combatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statService.GetAttack(activator, stat, SkillType.OneHanded);
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

            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                RemoveEffectByTag(target, EffectTag);
                var eBreach = TagEffect(EffectACDecrease(2), EffectTag);
                ApplyEffectToObject(DurationType.Temporary, eBreach, target, breachTime);
            }
            
            _combatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            AssignCommand(activator, () => ActionPlayAnimation(Animation.RiotBlade));

            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void SaberStrike1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SaberStrike1, PerkType.SaberStrike)
                .Name("Saber Strike I")
                .Level(1)
                .HasRecastDelay(RecastGroup.SaberStrike, 60f)
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
                .HasRecastDelay(RecastGroup.SaberStrike, 60f)
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
                .HasRecastDelay(RecastGroup.SaberStrike, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
