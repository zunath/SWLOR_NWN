using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class LabLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Lab();

            return _builder.Build();
        }

        private void Lab()
        {
            _builder.Create(PropertyLayoutType.LabStyle1)
                .PropertyType(PropertyType.Lab)
                .Name("Lab")
                .StructureLimit(40)
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .ResearchDeviceLimit(20)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("player_rnd");
        }
    }
}
