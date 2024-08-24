using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class AssaultConcussionMissileModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            AssaultConcussionMissile("acm_launch_1", "Assault Concussion Missile Launcher", "ACM Launcher", "Requires an assault concussion missile. Its massive warhead deals 200 DMG to enemy capital ships, but cannot target subcapital ships.", 200);

            return _builder.Build();
        }

        private const string ACMItemResref = "acm_ammo";

        private void PerformAttack(uint activator, uint target, int dmg, int attackBonus, bool? hitOverride)
        {
            var targetShipStatus = Space.GetShipStatus(target);
            if (targetShipStatus == null)
                return;

            var chanceToHit = Space.CalculateChanceToHit(activator, target);
            var roll = Random.D100(1);
            var isHit = hitOverride ?? roll <= chanceToHit;

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);

            if (isHit)
            {
                var defenseBonus = targetShipStatus.ExplosiveDefense * 2;
                var defense = Stat.GetDefense(target, CombatDamageType.Explosive, AbilityType.Vitality, defenseBonus);
                var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                var damage = Combat.CalculateDamage(
                    attack,
                    dmg,
                    attackerStat,
                    defense,
                    defenderStat,
                    0);

                Space.ApplyHullDamage(activator, target, damage);
                Enmity.ModifyEnmity(activator, target, damage);
            }

            var attackId = isHit ? 1 : 4;
            var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
            Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
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

                    if (!Space.GetShipStatus(target).CapitalShip)
                    {
                        return "This weapon can only target capital ships.";
                    }

                    return string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    // Missiles do 25% more damage to unshielded targets. Due to shield recharge starting instantly, allow for up to 4 shield points to be considered "unshielded".
                    if (targetShipStatus.Shield <= 4)
                    {
                        dmg += dmg / 4;
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

                    var moduleDamage = dmg + (moduleBonus * 3);

                    var targetDistance = GetDistanceBetween(activator, target);
                    var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
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
