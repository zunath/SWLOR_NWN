using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class LegSweepAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IAbilityService _abilityService;
        private readonly CombatPoint _combatPoint;
        private readonly IEnmityService _enmityService;

        public LegSweepAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, IAbilityService abilityService, CombatPoint combatPoint, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _abilityService = abilityService;
            _combatPoint = combatPoint;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            LegSweep1(builder);
            LegSweep2(builder);
            LegSweep3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.StaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a staff ability.";
            }
            else 
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            int dmg;
            int dc;
            const float Duration = 6f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 4;
                    dc = 5;
                    break;
                case 2:
                    dmg = 13;
                    dc = 8;
                    break;
                case 3:
                    dmg = 20;
                    dc = 10;
                    break;
            }

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            _enmityService.ModifyEnmityOnAll(activator, 250 * level);
            _combatPoint.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = _combatService.GetPerkAdjustedAbilityScore(activator);
            int attack;

            if(GetHasFeat(FeatType.FlurryStyle, activator))
            {
                attack = _statService.GetAttack(activator, AbilityType.Perception, SkillType.MartialArts);
            } 
            else
            {
                attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            }
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);
                _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
            }
        }

        private void LegSweep1(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep1, PerkType.LegSweep)
                .Name("Leg Sweep I")
                .Level(1)
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void LegSweep2(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep2, PerkType.LegSweep)
                .Name("Leg Sweep II")
                .Level(2)
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void LegSweep3(AbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep3, PerkType.LegSweep)
                .Name("Leg Sweep III")
                .Level(3)
                .HasRecastDelay(RecastGroup.LegSweep, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}