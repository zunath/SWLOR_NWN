using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Bioware;
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

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ThrowLightsaberAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ThrowLightsaberAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ThrowLightsaber1(builder);
            ThrowLightsaber2(builder);
            ThrowLightsaber3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var distance = GetDistanceBetween(activator, target);

            var validWeapon = GetIsObjectValid(weapon) &&
                                 (ItemService.LightsaberBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                                  ItemService.VibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                                  ItemService.FinesseVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                                  ItemService.SaberstaffBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                                  ItemService.ThrowingWeaponBaseItemTypes.Contains(GetBaseItemType(weapon)));

            if (distance > 15)
                return "You must be within 15 meters of your target.";
            if (!validWeapon)
                return "You cannot force throw your currently held object.";
            else return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            const float Range = 15.0f;
            var count = 1;
            var delay = GetDistanceBetween(activator, target) / 10.0f;
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);



            // Make the activator face their target.
            ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, activator);

            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.SaberThrow, 2));
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);
            var perBonus = GetAbilityScore(activator, AbilityType.Perception);

            switch (level)
            {
                case 1:
                    dmg = (willBonus + perBonus) / 2;
                    break;
                case 2:
                    dmg = 20 + ((willBonus + perBonus) * 3 / 4);
                    break;
                case 3:
                    dmg = 40 + (willBonus + perBonus);
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Force);
            var attack = StatService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            CombatPointService.AddCombatPoint(activator, target, SkillType.Force, 3);

            // apply to target
            DelayCommand(delay, () =>
            {
                var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
                var damage = CombatService.CalculateDamage(
                    attack,
                    dmg,
                    attackerStat,
                    defense,
                    defenderStat,
                    0);
                ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffectType.Vfx_Imp_Sonic), EffectDamage(damage, DamageType.Sonic)), target);
                EnmityService.ModifyEnmity(activator, target, damage + 200 * level);
            });

            // apply to next nearest creature in the spellcylinder
            var nearby = GetFirstObjectInShape(ShapeType.SpellCylinder, Range, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            while (GetIsObjectValid(nearby) && count < level)
            {
                if (nearby != target && nearby != activator)
                {
                    delay = GetDistanceBetween(activator, nearby) / 10.0f;
                    var nearbyCopy = nearby;
                    DelayCommand(delay, () =>
                    {
                        var defense = StatService.GetDefense(nearbyCopy, CombatDamageType.Physical, AbilityType.Vitality);
                        var defenderStat = GetAbilityModifier(AbilityType.Willpower, nearbyCopy);
                        var damage = CombatService.CalculateDamage(
                            attack,
                            dmg,
                            attackerStat,
                            defense,
                            defenderStat,
                            0);
                        ApplyEffectToObject(DurationType.Instant, EffectLinkEffects(EffectVisualEffect(VisualEffectType.Vfx_Imp_Sonic), EffectDamage(damage, DamageType.Sonic)), nearbyCopy);
                        CombatPointService.AddCombatPoint(activator, nearbyCopy, SkillType.Force, 3);
                        EnmityService.ModifyEnmity(activator, nearbyCopy, damage + 200 * level);
                    });

                    count++;
                }
                nearby = GetNextObjectInShape(ShapeType.SpellCylinder, Range, GetLocation(target), true, ObjectType.Creature, GetPosition(activator));
            }

        }

        private void ThrowLightsaber1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber1, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ThrowLightsaber, 18f)
                .HasActivationDelay(1.5f)
                .HasMaxRange(15.0f)
                .RequirementFP(1)
                .RequirementStamina(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .UnaffectedByHeavyArmor()
                .HasImpactAction(ImpactAction);
        }
        private void ThrowLightsaber2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber2, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ThrowLightsaber, 18f)
                .HasActivationDelay(1.5f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .RequirementStamina(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .UnaffectedByHeavyArmor()
                .HasImpactAction(ImpactAction);
        }
        private void ThrowLightsaber3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowLightsaber3, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ThrowLightsaber, 18f)
                .HasActivationDelay(1.5f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .RequirementStamina(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .UnaffectedByHeavyArmor()
                .HasImpactAction(ImpactAction);
        }
    }
}
