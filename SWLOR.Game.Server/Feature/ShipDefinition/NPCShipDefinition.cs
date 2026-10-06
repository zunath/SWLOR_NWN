using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public sealed class NPCShipDefinition : IShipListDefinition
    {
        public Dictionary<string, ShipDetail> BuildShips()
        {
            var builder = new ShipBuilder();
            foreach (var binding in SpaceEncounterCatalog.Default.Bindings.Values)
            {
                var profile = SpaceEncounterCatalog.Default.Profiles[binding.Profile];
                builder.Create(binding.Ship).ItemResref(binding.Tag).Name(binding.Name)
                    .MaxArmor(profile.Hull).MaxShield(profile.Shield).MaxCapacitor(profile.CapacitorPool)
                    .HighPowerNodes(profile.Weapons).LowPowerNodes(0).ShipConfigurationNodes(0);
                if (profile.Id == "fleet_objective") builder.CapitalShip();
            }
            return builder.Build();
        }
    }
}
