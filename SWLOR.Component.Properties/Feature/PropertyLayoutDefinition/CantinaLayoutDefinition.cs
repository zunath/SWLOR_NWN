using SWLOR.Component.Properties.Contracts;
using SWLOR.Component.Properties.Enums;
using SWLOR.Component.Properties.Model;
using SWLOR.Component.Properties.Service;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Properties.Feature.PropertyLayoutDefinition
{
    public class CantinaLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Cantina();

            return _builder.Build();
        }

        private void Cantina()
        {
            _builder.Create(PropertyLayoutType.CantinaStyle1)
                .PropertyType(PropertyType.Cantina)
                .Name("Cantina")
                .StructureLimit(80)
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("cantina");
        }
    }
}
