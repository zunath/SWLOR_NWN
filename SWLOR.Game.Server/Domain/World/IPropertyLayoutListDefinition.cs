using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.World
{
    internal interface IPropertyLayoutListDefinition
    {
        public Dictionary<PropertyLayoutType, PropertyLayout> Build();
    }
}
