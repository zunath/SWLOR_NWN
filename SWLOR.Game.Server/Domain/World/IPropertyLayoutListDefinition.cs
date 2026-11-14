using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.World
{
    internal interface IPropertyLayoutListDefinition
    {
        public Dictionary<PropertyLayoutType, PropertyLayout> Build();
    }
}
