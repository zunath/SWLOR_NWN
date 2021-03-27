using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class CombatLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CombatLaser("com_laser_b", "Basic Combat Laser", "B. Cmbt Laser", "Deals light thermal damage to your target.", 1, 3f, 6, 5);
            CombatLaser("com_laser_1", "Combat Laser I", "Cmbt Laser I", "Deals light thermal damage to your target.", 2, 4f, 9, 8);
            CombatLaser("com_laser_2", "Combat Laser II", "Cmbt Laser II", "Deals light thermal damage to your target.", 3, 5f, 12, 11);
            CombatLaser("com_laser_3", "Combat Laser III", "Cmbt Laser III", "Deals light thermal damage to your target.", 4, 6f, 15, 14);
            CombatLaser("com_laser_4", "Combat Laser IV", "Cmbt Laser IV", "Deals light thermal damage to your target.", 5, 7f, 18, 16);

            return _builder.Build();
        }

        private void CombatLaser(string itemTag, string name, string shortName, string description, int requiredLevel, float recast, int capacitor, int baseDamage)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.CombatLaser)
                .Texture("iit_ess_004")
                .Description(description)
                .RequiresTarget()
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus) =>
                {
                    var targetDefense = targetShipStatus.ThermalDefense;
                    var attackerDamage = baseDamage + activatorShipStatus.ThermalDamage;

                    var damage = attackerDamage - targetDefense;
                    if (damage < 0) damage = 0;

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;
                    
                    if (isHit)
                    {
                        AssignCommand(activator, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Fire, activator, BodyNode.Chest);
                            ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                            Space.ApplyShipDamage(activator, target, damage);
                        });
                    }
                    else
                    {
                        AssignCommand(activator, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Fire, activator, BodyNode.Chest, true);
                            ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                        });
                        SendMessageToPC(activator, "You miss your target.");
                    }

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }

    }
}
