using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PropertyService
{
    internal interface IPropertyLayoutListDefinition
    {
        public Dictionary<PropertyLayoutType, PropertyLayout> Build();
    }
}
