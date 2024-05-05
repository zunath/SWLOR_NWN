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
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            BeamCannon("beamcannon1", "Beam Cannon 1", "A stream of energy deals increasing damage with each successive hit. Starts at 3 DMG per tick and increases by 1 DMG with each consecutive hit. Resets on a miss. Ticks are every 0.2 seconds for 1 second.", 3, 1, 1);
            BeamCannon("beamcannon2", "Beam Cannon 2", "A stream of energy deals increasing damage with each successive hit. Starts at 6 DMG per tick and increases by 1 DMG with each consecutive hit. Resets on a miss. Ticks are every 0.2 seconds for 1 second.", 6, 2, 1);
            BeamCannon("beamcannon3", "Beam Cannon 3", "A stream of energy deals increasing damage with each successive hit. Starts at 9 DMG per tick and increases by 1 DMG with each consecutive hit. Resets on a miss. Ticks are every 0.2 seconds for 1 second.", 9, 3, 1);
            BeamCannon("beamcannon4", "Beam Cannon 4", "A stream of energy deals increasing damage with each successive hit. Starts at 12 DMG per tick and increases by 2 DMG with each consecutive hit. Resets on a miss. Ticks are every 0.2 seconds for 1 second.", 12, 4, 2);
            BeamCannon("beamcannon5", "Beam Cannon 5", "A stream of energy deals increasing damage with each successive hit. Starts at 15 DMG per tick and increases by 2 DMG with each consecutive hit. Resets on a miss. Ticks are every 0.2 seconds for 1 second.", 15, 5, 2);

            return _builder.Build();
        }

        private void BeamCannon(
            string itemTag,
            string name,
            string description,
            int dmg,
            int requiredLevel,
            int tickIncrease)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Type(ShipModuleType.BeamLaser)
                .Texture("iit_ess_017")
                .Description(description)
                .MaxDistance(60f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(12f)
                .Capacitor(8)
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
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.Thermal, AbilityType.Vitality, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    dmg += moduleBonus / 2;
                    var beam = EffectBeam(VisualEffect.Vfx_Beam_Holy, activator, BodyNode.Chest);
                    var startingDMG = dmg;

                    for (int i = 0; i < 4; i++)
                    {
                        float delay = i * 0.25f;
                        DelayCommand(delay, () =>
                        {
                            if (GetIsDead(activator))
                            {
                                var chanceToHit = Space.CalculateChanceToHit(activator, target);
                                var roll = Random.D100(1);
                                var isHit = roll <= chanceToHit;
                                if (isHit)
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
                                        Space.ApplyShipDamage(activator, target, damage);
                                        dmg += tickIncrease;
                                    });
                                }
                                else
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
                                        dmg = startingDMG;
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
