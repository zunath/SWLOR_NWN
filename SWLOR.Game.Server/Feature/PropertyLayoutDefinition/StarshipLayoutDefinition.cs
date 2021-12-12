using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class StarshipLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();
        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            LightFreighter();
            LightEscort();

            return _builder.Build();
        }

        private void LightFreighter()
        {
            _builder.Create(PropertyLayoutType.LightFreighter1)
                .PropertyType(PropertyType.Starship)
                .Name("Light Freighter 1")
                .StructureLimit(30)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("starship1_int");
        }

        private void LightEscort()
        {
            _builder.Create(PropertyLayoutType.LightFreighter1)
                .PropertyType(PropertyType.Starship)
                .Name("Light Escort 1")
                .StructureLimit(30)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("starship2_int");
        }
    }
}
