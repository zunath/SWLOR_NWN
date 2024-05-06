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
    public class TurboLaserModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            Turbolaser("turbolas1", "Heavy Turbolaser Cannon", "Turbolaser1", "Deals 80 thermal DMG to your target. Designed to target capital ships, it struggles against subcapital ships and has a 50% miss chance against them.", 1, 80, 1, 10);
            Turbolaser("turbolas2", "Dual Turbolaser Cannon", "Turbolaser1", "Deals 60 thermal DMG to your target, firing twice over two seconds. Designed to target capital ships, it struggles against subcapital ships and has a 50% miss chance against them.", 1, 60, 2, 15);
            Turbolaser("turbolas3", "Quad Turbolaser Cannon", "Turbolaser1", "Deals 40 thermal DMG to your target, firing four times over four seconds. Designed to target capital ships, it struggles against subcapital ships and has a 50% miss chance against them.", 1, 40, 4, 30);

            return _builder.Build();
        }

        private void Turbolaser(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int dmg,
            int attacks,
            int capacitor)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.TurboLaser)
                .Texture("iit_ess_079")
                .Description(description)
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(18f)
                .Capacitor(capacitor)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Piloting, attackBonus);
                    if (GetHasFeat(FeatType.IntuitivePiloting, activator) && GetAbilityScore(activator, AbilityType.Willpower) > GetAbilityScore(activator, AbilityType.Perception))
                    {
                        attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                        attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);
                    }
                    var moduleDamage = dmg + moduleBonus * 2;
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.Thermal, AbilityType.Vitality, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;
                    var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                    var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
                    for (int i = 0; i < attacks; i++)
                    {
                        float delay = i * 3f;
                        DelayCommand(delay, () =>
                        {
                            if (!GetIsDead(activator))
                            {
                                var roll = Random.D100(1);
                                var isHit = roll <= chanceToHit;
                                if (isHit && (!Space.GetShipStatus(target).CapitalShip && Random.D2(1) != 2))
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Instant, sound, target);
                                        ApplyEffectToObject(DurationType.Instant, missile, target);
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Fire), target);

                                        DelayCommand(0.3f, () =>
                                        {
                                            Space.ApplyShipDamage(activator, target, damage);
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
                                var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                                Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                                Enmity.ModifyEnmity(activator, target, damage);
                                CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                            }
                        });
                    }
                });
        }
    }
}
