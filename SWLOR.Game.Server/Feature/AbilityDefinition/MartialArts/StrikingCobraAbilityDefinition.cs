//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class StrikingCobraAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly IEnmityService _enmityService;

        public StrikingCobraAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IStatusEffectService statusEffectService, IEnmityService enmityService)
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
            StrikingCobra1(builder);
            StrikingCobra2(builder);
            StrikingCobra3(builder);

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
                    dmg = 6;
                    dc = 10;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 22;
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
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

            dc = _combatService.CalculateSavingThrowDC(activator, dc, 0, 0);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                _statusEffectService.Apply(activator, target, StatusEffectType.Poison, duration);
            }

            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void StrikingCobra1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra1, PerkType.StrikingCobra)
                .Name("Striking Cobra I")
                .Level(1)
                .HasRecastDelay(RecastGroup.StrikingCobra, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void StrikingCobra2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra2, PerkType.StrikingCobra)
                .Name("Striking Cobra II")
                .Level(2)
                .HasRecastDelay(RecastGroup.StrikingCobra, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void StrikingCobra3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra3, PerkType.StrikingCobra)
                .Name("Striking Cobra III")
                .Level(3)
                .HasRecastDelay(RecastGroup.StrikingCobra, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
