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
    public class QuadLaserCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            QuadLaserCannon("cap_quadlas1", "Quad Laser Cannons", "Quad Lasers", "Fires a series of four shots at a target doing 20 DMG each.", 20, 4);
            QuadLaserCannon("cap_quadlas2", "Quad-Laser Cannon Array", "QLas Array", "Fires a series of five shots at a target doing 20 DMG each.", 20, 5);
            QuadLaserCannon("cap_quadlas3", "Quad-Laser Cannon Battery", "QLas Battery", "Fires a series of six shots at a target doing 20 DMG each.", 20, 6);

            QuadLaserCannon("npc_quadlas1", "Tier 1 Quad Laser", "T1 Quad Lasers", "NPC Quad Lasers.", 5, 4);
            QuadLaserCannon("npc_quadlas2", "Tier 2 Quad Laser", "T2 Quad Lasers", "NPC Quad Lasers.", 7, 4);
            QuadLaserCannon("npc_quadlas3", "Tier 3 Quad Laser", "T3 Quad Lasers", "NPC Quad Lasers.", 9, 4);
            QuadLaserCannon("npc_quadlas4", "Tier 4 Quad Laser", "T4 Quad Lasers", "NPC Quad Lasers.", 12, 4);
            QuadLaserCannon("npc_quadlas5", "Tier 5 Quad Laser", "T5 Quad Lasers", "NPC Quad Lasers.", 15, 5);
            QuadLaserCannon("npc_quadlas6", "Tier 6 Quad Laser", "T6 Quad Lasers", "NPC Quad Lasers.", 20, 6);
            QuadLaserCannon("npc_quadlas7", "Tier 7 Quad Laser", "T7 Quad Lasers", "NPC Quad Lasers.", 20, 6);
            QuadLaserCannon("npc_quadlas8", "Tier 8 Quad Laser", "T8 Quad Lasers", "NPC Quad Lasers.", 25, 6);
            QuadLaserCannon("npc_quadlas9", "Tier 9 Quad Laser", "T9 Quad Lasers", "NPC Quad Lasers.", 25, 6);
            QuadLaserCannon("npc_quadlas10", "Tier 10 Quad Laser", "T10 Quad Lasers", "NPC Quad Lasers.", 30, 6);
            QuadLaserCannon("npc_quadlas11", "Tier 11 Quad Laser", "T11 Quad Lasers", "NPC Quad Lasers.", 30, 7);
            QuadLaserCannon("npc_quadlas12", "Tier 12 Quad Laser", "T12 Quad Lasers", "NPC Quad Lasers.", 30, 8);

            return _builder.Build();
        }

        private void QuadLaserCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg,
            int totalAttacks)
        {
        _builder.Create(itemTag)
            .Name(name)
            .ShortName(shortName)
            .Type(ShipModuleType.QuadLaser)
            .Texture("iit_ess2_035")
            .Description(description)
            .MaxDistance(60f)
            .ValidTargetType(ObjectType.Creature)
            .PowerType(ShipModulePowerType.High)
            .RequirePerk(PerkType.OffensiveModules, 5)
            .Recast(8f)
            .Capacitor(totalAttacks * 2)
            .CapitalClassModule()
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
                var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
                
                for (int i = 0; i < totalAttacks; i++)
                {
                    float delay = i * 0.25f;
                    DelayCommand(delay, () =>
                    {
                        var chanceToHit = Space.CalculateChanceToHit(activator, target);
                        var roll = Random.D100(1);
                        var isHit = roll <= chanceToHit;
                        if (isHit)
                        {
                            AssignCommand(activator, () =>
                        {
                                    ApplyEffectToObject(DurationType.Instant, sound, target);
                                    ApplyEffectToObject(DurationType.Instant, missile, target);

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
                    });
                }
            });
        }
    }
}
