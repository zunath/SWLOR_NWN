using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class MissileLauncherModuleDefinition : IShipModuleListDefinition    
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();
        private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();
        private ICombatMessagingService CombatMessagingService => _serviceProvider.GetRequiredService<ICombatMessagingService>();

        public MissileLauncherModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MissileLauncher("msl_launch_b", "Basic Missile Launcher", "B. Msl Launch", "Requires a missile. Deals 12 explosive DMG to your target. Bonus damage on unshielded targets.", 1, 12);
            MissileLauncher("msl_launch_1", "Missile Launcher I", "Msl Launch I", "Requires a missile. Deals 24 explosive DMG to your target. Bonus damage on unshielded targets.", 2, 24);
            MissileLauncher("msl_launch_2", "Missile Launcher II", "Msl Launch II", "Requires a missile. Deals 36 explosive DMG to your target. Bonus damage on unshielded targets.", 3, 36);
            MissileLauncher("msl_launch_3", "Missile Launcher III", "Msl Launch III", "Requires a missile. Deals 48 explosive DMG  to your target. Bonus damage on unshielded targets.", 4, 48);
            MissileLauncher("msl_launch_4", "Missile Launcher IV", "Msl Launch IV", "Requires a missile. Deals 60 explosive DMG to your target. Bonus damage on unshielded targets.", 5, 60);

            return _builder.Build();
        }

        private const string MissileItemResref = "ship_missile";

        private void PerformAttack(uint activator, uint target, int dmg, int attackBonus, bool? hitOverride)
        {
            var targetShipStatus = SpaceService.GetShipStatus(target);
            if (targetShipStatus == null)
                return;

            var chanceToHit = SpaceService.CalculateChanceToHit(activator, target);
            var roll = Random.D100(1);
            var isHit = hitOverride ?? roll <= chanceToHit;

            var attackerStat = SpaceService.GetAttackStat(activator);
            var attack = SpaceService.GetShipAttack(activator, attackBonus);

            if (isHit)
            {
                var defenseBonus = targetShipStatus.ExplosiveDefense * 2;
                var defense = SpaceService.GetShipDefense(target, defenseBonus);
                var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

                // Determine attacker stat type (Willpower or Perception based on Intuitive Piloting feat)
                var wil = GetAbilityScore(activator, AbilityType.Willpower);
                var per = GetAbilityScore(activator, AbilityType.Perception);
                var attackerStatType = (GetHasFeat(FeatType.IntuitivePiloting, activator) && wil > per)
                    ? AbilityType.Willpower
                    : AbilityType.Perception;

                var damage = CombatCalculationService.CalculateAbilityDamage(
                    activator,
                    target,
                    dmg,
                    CombatDamageType.Explosive,
                    SkillType.Piloting,
                    attackerStatType,
                    AbilityType.Vitality);

                SpaceService.ApplyShipDamage(activator, target, damage);
                EnmityService.ModifyEnmity(activator, target, damage);
            }

            var attackId = isHit ? 1 : 4;
            var combatLogMessage = CombatMessagingService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
            MessagingService.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
            CombatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
        }

        private void MissileLauncher(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_089")
                .Type(ShipModuleType.Missile)
                .MaxDistance(55f)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(12f)
                .ValidationAction((activator, _, _, _, _) =>
                {
                    var item = GetItemPossessedBy(activator, MissileItemResref);
                    var stackSize = GetItemStackSize(item);
                    if(stackSize <= 0 && GetIsPC(activator) == true)
                    {
                        return "You need a missile to fire this weapon.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var moduleDamage = dmg + moduleBonus;
                    // Missiles do 25% more damage to unshielded targets. Due to shield recharge starting instantly, allow for up to 10 shield points to be considered "unshielded".
                    if (targetShipStatus.Shield <= 10)
                    {
                        moduleDamage += moduleDamage / 4;
                    }
                    var item = GetItemPossessedBy(activator, MissileItemResref);
                    var stackSize = GetItemStackSize(item);
                    if (stackSize <= 1)
                    {
                        DestroyObject(item);
                    }
                    else
                    {
                        SetItemStackSize(item, stackSize - 1);
                    }

                    var targetDistance = GetDistanceBetween(activator, target);
                    var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                    var chanceToHit = SpaceService.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;

                    var attackBonus = moduleBonus * 2 + activatorShipStatus.ExplosiveDamage;

                    // Shoot some missiles out to the target.
                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Ship_Trp), activator);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Mirv_Torpedo, !isHit), target);
                    });

                    // Display an explosion at the target location in a few seconds (based on travel distance of the initial missile graphic)
                    // Then apply damage on target and those nearby.
                    DelayCommand(delay, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Fnf_Fireball, !isHit, 0.5f), target);
                        PerformAttack(activator, target, moduleDamage, attackBonus, isHit);
                    });
                });
        }
    }
}




