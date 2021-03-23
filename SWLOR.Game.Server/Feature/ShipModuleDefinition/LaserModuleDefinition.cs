using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class LaserModuleDefinition: IShipModuleListDefinition
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
                .Description("deals damage or something, i dunno") // todo
                .IsActiveModule()
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 1)
                .Recast(3.0f)
                .Capacitor(10)
                .ActivatedAction((player, target, ship) =>
                {
                    AssignCommand(player, () =>
                    {
                        var effect = EffectBeam(VisualEffect.Vfx_Beam_Lightning, player, BodyNode.Chest);
                        ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(1), target);
                    });

                });
        }

    }
}
