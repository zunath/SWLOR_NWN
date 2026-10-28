using System.Collections.Generic;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    /// <summary>
    /// Registers every hull of the space revamp (<see cref="RevampShipCatalog"/>). The Piloting gate is the
    /// RequiresSkill property on each deed, so no ship requires a perk.
    /// </summary>
    public class RevampPlayerShipDefinition : IShipListDefinition
    {
        private readonly ShipBuilder _builder = new();

        public Dictionary<string, ShipDetail> BuildShips()
        {
            foreach (var hull in RevampShipCatalog.Hulls)
            {
                _builder.Create(hull.Tag)
                    .ItemResref(hull.Tag)
                    .Name(hull.Name)
                    .Appearance(hull.Appearance)
                    .MaxShield(hull.Shield)
                    .MaxArmor(hull.Hull)
                    .MaxCapacitor(hull.Capacitor)
                    .HighPowerNodes(hull.High)
                    .MidPowerNodes(hull.Mid)
                    .LowPowerNodes(hull.Low)
                    .ShipConfigurationNodes(0)
                    .Size(hull.Size)
                    .Role(hull.Role)
                    .Manufacturer(hull.Manufacturer)
                    .InteriorLayout(hull.Layout)
                    .Hull(hull);

                if (hull.Size == ShipSize.Capital)
                    _builder.CapitalShip();

                if (DroidIndustryRules.HasDroidBay(hull.Role))
                    _builder.DroidBay();
            }

            return _builder.Build();
        }
    }
}
