using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Ranged
{
    public class TranquilizerShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public TranquilizerShotAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            TranquilizerShot1(builder);
            TranquilizerShot2(builder);
            TranquilizerShot3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.RifleBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a rifle ability.";
            }
            else
                return string.Empty;
        }

        private void ApplyEffect(uint activator, uint target, int level, float duration)
        {
            var effectTag = $"StatusEffectType.Tranquilize";
            var enmity = level * 500;

            var vfx = EffectVisualEffect(VisualEffectType.Vfx_Dur_Iounstone_Blue);
            vfx = TagEffect(vfx, effectTag);
            var sleep = TagEffect(EffectSleep(), effectTag);

            ApplyEffectToObject(DurationType.Temporary, sleep, target, duration);
            ApplyEffectToObject(DurationType.Temporary, vfx, target, duration);
            AbilityService.ApplyTemporaryImmunity(target, duration, ImmunityType.Sleep);

            EnmityService.ModifyEnmity(activator, target, enmity);
            CombatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            
            switch (level)
            {
                default:
                case 1:
                    ApplyEffect(activator, target, 1, 12f);
                    break;
                case 2:
                    ApplyEffect(activator, target, 2, 24f);
                    break;
                case 3:
                    var count = 0;
                    var creature = GetFirstObjectInShape(ShapeType.SpellCone, RadiusSize.Colossal, GetLocation(target), true);
                    while (GetIsObjectValid(creature) && count < 3)
                    {
                        if(creature != activator) 
                        {
                            ApplyEffect(activator, creature, 3, 12f);

                            count++;
                        }
                        creature = GetNextObjectInShape(ShapeType.SpellCone, RadiusSize.Colossal, GetLocation(target), true);
                    }
                    break;
            }
        }

        private void TranquilizerShot1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot1, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.TranquilizerShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void TranquilizerShot2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.TranquilizerShot, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void TranquilizerShot3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.TranquilizerShot3, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.TranquilizerShot, 300f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
