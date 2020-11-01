using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class EntityList<T> : List<T>
        where T : EntityBase
    {
    }
}