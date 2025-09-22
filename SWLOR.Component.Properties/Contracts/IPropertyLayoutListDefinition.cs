using SWLOR.Component.Properties.Enums;
using SWLOR.Component.Properties.Model;

namespace SWLOR.Component.Properties.Contracts
{
    internal interface IPropertyLayoutListDefinition
    {
        public Dictionary<PropertyLayoutType, PropertyLayout> Build();
    }
}
