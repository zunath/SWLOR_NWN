using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Properties.Contracts
{
    internal interface IPropertyLayoutListDefinition
    {
        public Dictionary<PropertyLayoutType, PropertyLayout> Build();
    }
}
