using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data
{
    public abstract class EntityBase: IEntity
    {
        protected EntityBase()
        {
            ID = Guid.NewGuid();
        }

        public virtual Guid ID { get; protected set; }
        public abstract IEntity Clone();
    }
}
