//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded
{
    public class CrossCutAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public CrossCutAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CrossCut1(builder);
            CrossCut2(builder);
            CrossCut3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.TwinBladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a twin-blade ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;
            int acLoss;
            const float Duration = 60f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    acLoss = 2;
                    dc = 10;
                    break;
                case 2:
                    dmg = 17;
                    acLoss = 4;
                    dc = 15;
                    break;
                case 3:
                    dmg = 25;
                    acLoss = 6;
                    dc = 20;
                    break;
            }

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
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

            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                RemoveEffectByTag(target, "CROSS_CUT");
                var breach = TagEffect(EffectACDecrease(acLoss), "CROSS_CUT");
                ApplyEffectToObject(DurationType.Temporary, breach, target, Duration);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.CrossCut));
            DelayCommand(0.2f, () =>
            {
                AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleStrike));
            });

            _combatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void CrossCut1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut1, PerkType.CrossCut)
                .Name("Cross Cut I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private void CrossCut2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut2, PerkType.CrossCut)
                .Name("Cross Cut II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private void CrossCut3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut3, PerkType.CrossCut)
                .Name("Cross Cut III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}
