using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Social.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class CombatLaserModuleDefinition : IShipModuleListDefinition
    {
        private readonly IRandomService _random;
        private readonly ICombatService _combatService;
        private readonly ISpaceService _spaceService;
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IMessagingService _messagingService;
        private readonly IShipModuleBuilder _builder;

        public CombatLaserModuleDefinition(IRandomService random, ICombatService combatService, ISpaceService spaceService, IEnmityService enmityService, ICombatPointService combatPointService, IMessagingService messagingService, IShipModuleBuilder builder)
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
            CombatLaser("com_laser_b", "Basic Combat Laser", "B. Cmbt Laser", "Deals 12 thermal DMG to your target.", 1, 12, 2);
            CombatLaser("com_laser_1", "Combat Laser I", "Cmbt Laser I", "Deals 18 thermal DMG to your target.", 2, 18, 3);
            CombatLaser("com_laser_2", "Combat Laser II", "Cmbt Laser II", "Deals 24 thermal DMG to your target.", 3, 24, 4);
            CombatLaser("com_laser_3", "Combat Laser III", "Cmbt Laser III", "Deals 30 thermal DMG to your target.", 4, 30, 5);
            CombatLaser("com_laser_4", "Combat Laser IV", "Cmbt Laser IV", "Deals 36 thermal DMG to your target.", 5, 36, 6);

            return _builder.Build();
        }

        private void CombatLaser(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int dmg,
            int capacitor)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.CombatLaser)
                .Texture("iit_ess_004")
                .Description(description)
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(6f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = _spaceService.GetAttackStat(activator);
                    var attack = _spaceService.GetShipAttack(activator, attackBonus);
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = _spaceService.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var moduleDamage = dmg + moduleBonus / 3;
                    var damage = _combatService.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var chanceToHit = _spaceService.CalculateChanceToHit(activator, target);
                    var roll = _random.D100(1);
                    var isHit = roll <= chanceToHit;
                    var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                    var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);

                    if (isHit)
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, sound, target);
                            ApplyEffectToObject(DurationType.Instant, missile, target);

                            DelayCommand(0.3f, () =>
                            {
                                _spaceService.ApplyShipDamage(activator, target, damage);
                            });
                        });
                    }
                    else
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, sound, target);
                            ApplyEffectToObject(DurationType.Instant, missile, target);
                        });
                    }

                    var attackId = isHit ? 1 : 4;
                    var combatLogMessage = _combatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                    _messagingService.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    _enmityService.ModifyEnmity(activator, target, damage);
                    _combatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}
