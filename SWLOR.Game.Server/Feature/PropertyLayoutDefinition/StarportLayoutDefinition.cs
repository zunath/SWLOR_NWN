using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class StarportLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Starport();

            return _builder.Build();
        }

        private void Starport()
        {
            _builder.Create(PropertyLayoutType.StarportStyle1)
                .PropertyType(PropertyType.Starport)
                .Name("Starport")
                .StructureLimit(30)
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("starport")
                .OnSpawn(instance =>
                {
                    // todo: spawn dockhands
                });
        }
    }
}
