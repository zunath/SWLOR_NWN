using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Properties.ValueObjects;

namespace SWLOR.Component.Properties.Contracts
{
    internal interface IPropertyLayoutListDefinition
    {
        public Dictionary<PropertyLayoutType, PropertyLayout> Build();
    }
}
