using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HullRepairerModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HullRepairer("hull_rep_b", "Basic Hull Repairer", "H. Rep Basic", 5, 8, 1);
            HullRepairer("hull_rep_1", "Hull Repairer I", "H. Rep I", 8, 12, 1);
            HullRepairer("hull_rep_2", "Hull Repairer II", "H. Rep II", 11, 16, 1);
            HullRepairer("hull_rep_3", "Hull Repairer III", "H. Rep III", 14, 20, 1);
            HullRepairer("hull_rep_4", "Hull Repairer IV", "H. Rep IV", 17, 24, 1);

            return _builder.Build();
        }


        private void HullRepairer(string itemTag, string name, string shortName, int restoreAmount, int capRequired, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .IsActiveModule()
                .Recast(15f)
                .Capacitor(capRequired)
                .Description($"Restores {restoreAmount} points of Hull HP per use.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .ActivatedAction((activator, target) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Knock), target.Creature);
                    Messaging.SendMessageNearbyToPlayers(activator.Creature, $"{GetName(activator.Creature)} restores {restoreAmount} of hull damage on {GetName(target.Creature)}.");
                });
        }

    }
}