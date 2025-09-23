using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class ProtonBombModuleDefinition : IShipModuleListDefinition
    {
        private readonly IRandomService _random;
        private readonly ICombatService _combatService;
        private readonly ISpaceService _spaceService;
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IMessagingService _messagingService;
        private readonly IShipModuleBuilder _builder;

        public ProtonBombModuleDefinition(IRandomService random, ICombatService combatService, ISpaceService spaceService, IEnmityService enmityService, ICombatPointService combatPointService, IMessagingService messagingService, IShipModuleBuilder builder)
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
            ProtonBomb("protbomblnch", "Proton Bomb Launcher", "Prot B. Launch", "Requires a proton bomb. Can only target capital ships. Deals 120 DMG to target, bypassing shields.", 120);

            return _builder.Build();
        }

        private const string ProtonBombItemResref = "proton_bomb";

        private void PerformAttack(uint activator, uint target, int dmg, int attackBonus, bool? hitOverride)
        {
            var targetShipStatus = _spaceService.GetShipStatus(target);
            if (targetShipStatus == null)
                return;

            var chanceToHit = _spaceService.CalculateChanceToHit(activator, target);
            var roll = _random.D100(1);
            var isHit = hitOverride ?? roll <= chanceToHit;

            var attackerStat = _spaceService.GetAttackStat(activator);
            var attack = _spaceService.GetShipAttack(activator, attackBonus);

            if (isHit)
            {
                var defenseBonus = targetShipStatus.ExplosiveDefense * 2;
                var defense = _spaceService.GetShipDefense(target, defenseBonus);
                var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                var damage = _combatService.CalculateDamage(
                    attack,
                    dmg,
                    attackerStat,
                    defense,
                    defenderStat,
                    0);

                _spaceService.ApplyHullDamage(activator, target, damage);
                _enmityService.ModifyEnmity(activator, target, damage);
            }

            var attackId = isHit ? 1 : 4;
            var combatLogMessage = _combatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
            _messagingService.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
            _combatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
        }

        private void ProtonBomb(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_096")
                .Type(ShipModuleType.ProtonBomb)
                .MaxDistance(10f)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Recast(18f)
                .ValidationAction((activator, _, target, _, _) =>
                {
                    var item = GetItemPossessedBy(activator, ProtonBombItemResref);
                    var stackSize = GetItemStackSize(item);
                    if (stackSize <= 0 && GetIsPC(activator) == true)
                    {
                        return "You need a proton bomb to fire this weapon.";
                    }

                    if (!_spaceService.GetShipStatus(target).CapitalShip)
                    {
                        return "This weapon can only target capital ships.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var moduleDamage = dmg + (moduleBonus * 2);
                    // Bombs do 50% more damage to unshielded targets. Due to shield recharge starting instantly, allow for up to 10 shield points to be considered "unshielded".
                    if (targetShipStatus.Shield <= 10)
                    {
                        moduleDamage += moduleDamage / 2;
                    }
                    var item = GetItemPossessedBy(activator, ProtonBombItemResref);
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
                    var targetLocation = GetLocation(target);
                    var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                    var chanceToHit = _spaceService.CalculateChanceToHit(activator, target);
                    var roll = _random.D100(1);
                    var isHit = roll <= chanceToHit;

                    var attackBonus = moduleBonus * 2 + activatorShipStatus.ExplosiveDamage;

                    // Shoot some missiles out to the target.
                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Trp), activator);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_Torpedo, !isHit), target);
                    });

                    // Display an explosion at the target location in a few seconds (based on travel distance of the initial missile graphic)
                    // Then apply damage on target and those nearby.
                    DelayCommand(delay, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake, !isHit), target);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summondragon, !isHit), target);
                        PerformAttack(activator, target, moduleDamage, attackBonus, isHit);
                    });
                });
        }
    }
}
