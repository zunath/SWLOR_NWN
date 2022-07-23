using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class CityHallLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            CityHall();
            
            return _builder.Build();
        }

        private void CityHall()
        {
            _builder.Create(PropertyLayoutType.CityHallStyle1)
                .PropertyType(PropertyType.CityHall)
                .Name("City Hall")
                .StructureLimit(80) 
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("city_hall");
        }
    }
}
