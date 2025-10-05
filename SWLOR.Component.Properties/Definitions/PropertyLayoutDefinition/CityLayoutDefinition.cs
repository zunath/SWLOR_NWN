using SWLOR.Component.Properties.Contracts;
using SWLOR.Component.Properties.Service;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Properties.ValueObjects;

namespace SWLOR.Component.Properties.Definitions.PropertyLayoutDefinition
{
    public class CityLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            City();

            return _builder.Build();
        }

        private void City()
        {
            _builder.Create(PropertyLayoutType.City)
                .PropertyType(PropertyType.City)
                .Name("City")
                .StructureLimit(75) // For Cities, determines how many regular structures (non-buildings) can be placed in the area
                .ItemStorageLimit(0)
                .BuildingLimit(21)
                .ResearchDeviceLimit(0)
                .InitialPrice(100000)
                .PricePerDay(5000)
                .AreaInstance("");
        }
    }
}
