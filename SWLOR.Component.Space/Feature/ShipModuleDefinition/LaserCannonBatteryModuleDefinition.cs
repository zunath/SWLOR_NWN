using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.Enums;
using SWLOR.Component.Space.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class LaserCannonBatteryModuleDefinition : IShipModuleListDefinition
    {
        private readonly IRandomService _random;
        private readonly ICombatService _combatService;
        private readonly ISpaceService _spaceService;
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IMessagingService _messagingService;
        private readonly IShipModuleBuilder _builder;

        public LaserCannonBatteryModuleDefinition(IRandomService random, ICombatService combatService, ISpaceService spaceService, IEnmityService enmityService, ICombatPointService combatPointService, IMessagingService messagingService, IShipModuleBuilder builder)
        {
            _random = random;
            _combatService = combatService;
            _spaceService = spaceService;
            _enmityService = enmityService;
            _combatPointService = combatPointService;
            _messagingService = messagingService;
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            LaserCannonBattery("lasbattery1", "Laser Cannon Battery I", "Las Cann Bat 1", "Fires at 75% of nearby targets for 30 DMG. Fires once every 2 seconds for 18 seconds.", 30);

            LaserCannonBattery("npcautolas1", "NPC Laser Cannon Battery 1", "npcautolas1", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 12);
            LaserCannonBattery("npcautolas2", "NPC Laser Cannon Battery 2", "npcautolas2", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 17);
            LaserCannonBattery("npcautolas3", "NPC Laser Cannon Battery 3", "npcautolas3", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 22);
            LaserCannonBattery("npcautolas4", "NPC Laser Cannon Battery 4", "npcautolas4", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 25);
            LaserCannonBattery("npcautolas5", "NPC Laser Cannon Battery 5", "npcautolas5", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 30);
            LaserCannonBattery("npcautolas6", "NPC Laser Cannon Battery 6", "npcautolas6", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 33);
            LaserCannonBattery("npcautolas7", "NPC Laser Cannon Battery 7", "npcautolas7", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 36);
            LaserCannonBattery("npcautolas8", "NPC Laser Cannon Battery 8", "npcautolas8", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 39);
            LaserCannonBattery("npcautolas9", "NPC Laser Cannon Battery 9", "npcautolas9", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 41);
            LaserCannonBattery("npcautolas10", "NPC Laser Cannon Battery 10", "npcautolas10", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 43);
            LaserCannonBattery("npcautolas11", "NPC Laser Cannon Battery 11", "npcautolas11", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 45);
            LaserCannonBattery("npcautolas12", "NPC Laser Cannon Battery 12", "npcautolas12", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 50);

            return _builder.Build();
        }

        private void LaserCannonBattery(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
        _builder.Create(itemTag)
            .Name(name)
            .ShortName(shortName)
            .Type(ShipModuleType.LaserBattery)
            .Texture("iit_ess8_088")
            .Description(description)
            .ValidTargetType(ObjectType.Creature)
            .MaxDistance(20f)
            .PowerType(ShipModulePowerType.High)
            .RequirePerk(PerkType.OffensiveModules, 5)
            .Recast(18f)
            .Capacitor(15)
            .CapitalClassModule()
            .CanTargetSelf()
            .ActivatedAction((activator, activatorShipStatus, target, _, moduleBonus) =>
            {
                var attackBonus = activatorShipStatus.ThermalDamage;
                var attackerStat = _spaceService.GetAttackStat(activator);
                var attack = _spaceService.GetShipAttack(activator, attackBonus);

                var moduleDMG = dmg + moduleBonus;
                var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);

                for (var i = 0; i < 9; i++)
                {
                    var delay = i * 2f;
                    DelayCommand(delay, () =>
                    {
                        if (!GetIsDead(activator))
                        {
                            var nearbyTarget = GetFirstObjectInShape(Shape.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature);
                            while (GetIsObjectValid(nearbyTarget))
                            {
                                if (nearbyTarget != activator && 
                                    _random.D4(1) != 1 && 
                                    GetIsEnemy(nearbyTarget, activator) && 
                                    _spaceService.GetShipStatus(nearbyTarget) != null)
                                {
                                    var nearbyShipStatus = _spaceService.GetShipStatus(nearbyTarget);
                                    var nearbyDefenseBonus = nearbyShipStatus.ThermalDefense * 2;
                                    var nearbyDefense = _spaceService.GetShipDefense(nearbyTarget, nearbyDefenseBonus);
                                    var nearbyDefenderStat = GetAbilityScore(nearbyTarget, AbilityType.Vitality);
                                    var damage = _combatService.CalculateDamage(
                                        attack,
                                        moduleDMG,
                                        attackerStat,
                                        nearbyDefense,
                                        nearbyDefenderStat,
                                        0);
                                    var chanceToHit = _spaceService.CalculateChanceToHit(activator, nearbyTarget);
                                    var roll = _random.D100(1);
                                    var isHit = roll <= chanceToHit;
                                    ApplyEffectToObject(DurationType.Instant, missile, nearbyTarget);
                                    if (isHit)
                                    {
                                        _spaceService.ApplyShipDamage(activator, nearbyTarget, damage);
                                    }

                                    var attackId = isHit ? 1 : 4;
                                    var combatLogMessage = _combatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                                    _messagingService.SendMessageNearbyToPlayers(nearbyTarget, combatLogMessage, 60f);

                                    _enmityService.ModifyEnmity(activator, nearbyTarget, damage);
                                    _combatPointService.AddCombatPoint(activator, nearbyTarget, SkillType.Piloting);
                                }
                                nearbyTarget = GetNextObjectInShape(Shape.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature);
                            }
                        }
                    });
                }
            });
        }
    }
}
