using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class BankLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Bank();

            return _builder.Build();
        }

        private void Bank()
        {
            _builder.Create(PropertyLayoutType.Bank)
                .PropertyType(PropertyType.Bank)
                .Name("Bank")
                .StructureLimit(30)
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("bank");
        }
    }
}
