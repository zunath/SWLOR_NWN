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
                .Capacitor(10)
                .ActivatedAction((player, target, ship) =>
                {
                    var chanceToHit = Space.CalculateChanceToHit(player, target.Creature);
                    var isHit = Random.D100(1) <= chanceToHit;
                    
                    AssignCommand(player, () =>
                    {
                        var effect = EffectBeam(VisualEffect.Vfx_Beam_Lightning, player, BodyNode.Chest, !isHit);
                        ApplyEffectToObject(DurationType.Temporary, effect, target.Creature, 1.0f);

                        if (isHit)
                        {
                            // todo: damage calculations

                            Space.ApplyShipDamage(player, target.Creature, 5);
                        }
                    });
                });
        }

    }
}
