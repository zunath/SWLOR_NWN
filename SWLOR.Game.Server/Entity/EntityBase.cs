using System;

namespace SWLOR.Game.Server.Entity
{
    public abstract class EntityBase
    {
        [Indexed]
        public Guid ID { get; set; }
        
        public DateTime DateCreated { get; set; }

        [Indexed]
        public abstract string KeyPrefix { get; }

        protected EntityBase()
        {
            ID = Guid.NewGuid();
            DateCreated = DateTime.UtcNow;
        }
    }
}