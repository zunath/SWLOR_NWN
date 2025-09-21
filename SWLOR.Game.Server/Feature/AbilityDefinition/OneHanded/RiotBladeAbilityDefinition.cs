//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service;


using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class RiotBladeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public RiotBladeAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            RiotBlade1(builder);
            RiotBlade2(builder);
            RiotBlade3(builder);

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
            var dmg = 0;



            switch (level)
            {
                case 1:
                    dmg = 10;
                    break;
                case 2:
                    dmg = 20;
                    break;
                case 3:
                    dmg = 30;
                    break;
                default:
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

            AssignCommand(activator, () => ActionPlayAnimation(Animation.RiotBlade));

            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void RiotBlade1(AbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade1, PerkType.RiotBlade)
                .Name("Riot Blade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void RiotBlade2(AbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade2, PerkType.RiotBlade)
                .Name("Riot Blade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void RiotBlade3(AbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade3, PerkType.RiotBlade)
                .Name("Riot Blade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
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
