using System;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Entity
{
    public abstract class EntityBase
    {
        [Indexed]
        public string Id { get; set; }
        
        public DateTime DateCreated { get; set; }

        [Indexed]
        public string EntityType { get; set; }

        [JsonConstructor]
        protected EntityBase()
        {
            Id = Guid.NewGuid().ToString();
            DateCreated = DateTime.UtcNow;
            EntityType = GetType().Name;
        }
    }
}