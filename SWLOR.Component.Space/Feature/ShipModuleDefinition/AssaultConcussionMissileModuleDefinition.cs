using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class AssaultConcussionMissileModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public AssaultConcussionMissileModuleDefinition(
            IServiceProvider serviceProvider,
            IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            AssaultConcussionMissile("acm_launch_1", "Assault Concussion Missile Launcher", "ACM Launcher", "Requires an assault concussion missile. Its massive warhead deals 200 DMG to enemy capital ships, but cannot target subcapital ships.", 200);

            return _builder.Build();
        }

        private const string ACMItemResref = "acm_ammo";

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
                var damage = CombatService.CalculateDamage(
                    attack,
                    dmg,
                    attackerStat,
                    defense,
                    defenderStat,
                    0);

                SpaceService.ApplyHullDamage(activator, target, damage);
                EnmityService.ModifyEnmity(activator, target, damage);
            }

            var attackId = isHit ? 1 : 4;
            var combatLogMessage = CombatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
            MessagingService.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
            CombatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
        }

        private void AssaultConcussionMissile(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_094")
                .Type(ShipModuleType.AssaultConcussionMissile)
                .MaxDistance(50f)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Recast(24f)
                .CapitalClassModule()
                .ValidationAction((activator, _, target, _, _) =>
                {
                    var item = GetItemPossessedBy(activator, ACMItemResref);
                    var stackSize = GetItemStackSize(item);
                    if (stackSize <= 0 && GetIsPC(activator) == true)
                    {
                        return "You need an assault concussion missile to fire this weapon.";
                    }

                    if (!SpaceService.GetShipStatus(target).CapitalShip)
                    {
                        return "This weapon can only target capital ships.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var moduleDamage = dmg + (moduleBonus * 3);
                    // Missiles do 25% more damage to unshielded targets. Due to shield recharge starting instantly, allow for up to 10 shield points to be considered "unshielded".
                    if (targetShipStatus.Shield <= 10)
                    {
                        moduleDamage += moduleDamage / 4;
                    }
                    var item = GetItemPossessedBy(activator, ACMItemResref);
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
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Screen_Shake, !isHit), target);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Summondragon, !isHit), target);
                        PerformAttack(activator, target, moduleDamage, attackBonus, isHit);
                    });
                });
        }
    }
}
