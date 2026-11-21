using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    /// <summary>
    /// A player station (plan section 25): a city whose land is an instance of the station deck instead of a world area.
    /// Its limits and prices are the planet city's.
    /// </summary>
    public class StationLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Station();

            return _builder.Build();
        }

        private void Station()
        {
            _builder.Create(PropertyLayoutType.Station)
                .PropertyType(PropertyType.City)
                .Name("Station")
                .StructureLimit(StationTuning.StructureLimit)
                .ItemStorageLimit(0)
                .BuildingLimit(StationTuning.BuildingLimit)
                .ResearchDeviceLimit(0)
                .InitialPrice(StationTuning.InitialPrice)
                .PricePerDay(StationTuning.PricePerDay)
                .AreaInstance(StationTuning.DeckAreaResref);
        }
    }
}
