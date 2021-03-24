using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class CombatLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            Laser();
            return _builder.Build();
        }

        private void Laser()
        {
            _builder.Create("com_laser_b")
                .Name("Basic Combat Laser")
                .ShortName("B. Cmbt Laser")
                .Description("Deals light thermal damage to your target.")
                .IsActiveModule()
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 1)
                .Recast(3.0f)
                .Capacitor(6)
                .ActivatedAction((activator, target) =>
                {
                    var chanceToHit = Space.CalculateChanceToHit(activator.Creature, target.Creature);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;
                    
                    Console.WriteLine($"roll = {roll} vs {chanceToHit}"); // todo debug
                    
                    if (isHit)
                    {
                        AssignCommand(activator.Creature, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Lightning, activator.Creature, BodyNode.Chest);
                            ApplyEffectToObject(DurationType.Temporary, effect, target.Creature, 1.0f);
                            Space.ApplyShipDamage(activator.Creature, target.Creature, 5); // todo: damage calculation
                        });
                    }
                    else
                    {
                        AssignCommand(activator.Creature, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Lightning, activator.Creature, BodyNode.Chest, true);
                            ApplyEffectToObject(DurationType.Temporary, effect, target.Creature, 1.0f);
                        });
                        SendMessageToPC(activator.Creature, "You miss your target.");
                    }
                });
        }

    }
}
