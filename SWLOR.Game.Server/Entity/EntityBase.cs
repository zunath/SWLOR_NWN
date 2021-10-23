using System;

namespace SWLOR.Game.Server.Entity
{
    public abstract class EntityBase
    {
        [Indexed]
        public Guid Id { get; set; }
        
        public DateTime DateCreated { get; set; }

        [Indexed]
        public string EntityType { get; set; }

        protected EntityBase()
        {
            Id = Guid.NewGuid();
            DateCreated = DateTime.UtcNow;
            EntityType = GetType().Name;
        }
    }
}