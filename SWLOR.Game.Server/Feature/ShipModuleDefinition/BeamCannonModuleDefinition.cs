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
    public class BeamCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            BeamCannon("beamcannon1", "Basic Beam Cannon", "Basic Beam C.", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 3 DMG on a hit.", 3, 6, 1);
            BeamCannon("beamcannon2", "Beam Cannon I", "Beam Cann. 1", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 6 DMG on a hit.", 6, 7, 2);
            BeamCannon("beamcannon3", "Beam Cannon II", "Beam Cann. 2", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 9 DMG on a hit.", 9, 8, 3);
            BeamCannon("beamcannon4", "Beam Cannon III", "Beam Cann. 3", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 12 DMG on a hit.", 12, 9, 4);
            BeamCannon("beamcannon5", "Beam Cannon IV", "Beam Cann. 4", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 15 DMG on a hit.", 15, 10, 5);

            return _builder.Build();
        }

        private void BeamCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg,
            int capacitor,
            int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.BeamLaser)
                .Texture("iit_ess_017")
                .Description(description)
                .MaxDistance(60f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(10f)
                .Capacitor(capacitor)
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
                    var moduleDamage = dmg + moduleBonus / 3;
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.Thermal, AbilityType.Vitality, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

                    var beam = EffectBeam(VisualEffect.Vfx_Beam_Holy, activator, BodyNode.Chest);

                    for (var i = 0; i < 3; i++)
                    {
                        var delay = i * 0.33f;
                        DelayCommand(delay, () =>
                        {
                            var chanceToHit = Space.CalculateChanceToHit(activator, target);
                            var roll = Random.D100(1);
                            var isHit = roll <= chanceToHit;
                            var damage = Combat.CalculateDamage(
                                attack,
                                moduleDamage,
                                attackerStat,
                                defense,
                                defenderStat,
                                0);
                            if (!GetIsDead(activator))
                            {
                                if (isHit)
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
                                        Space.ApplyShipDamage(activator, target, damage);
                                    });
                                }
                                else
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
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
