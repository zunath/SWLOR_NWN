using SWLOR.Core.Service.PropertyService;

namespace SWLOR.Core.Feature.PropertyLayoutDefinition
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
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("city_hall");
        }
    }
}
