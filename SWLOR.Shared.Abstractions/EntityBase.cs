using Newtonsoft.Json;

namespace SWLOR.Shared.Abstractions
{
    /// <summary>
    /// Base class for all entities in the system.
    /// Provides common properties and behavior for database entities.
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>
        /// Unique identifier for the entity.
        /// </summary>
        [Indexed]
        public string Id { get; set; }
        
        /// <summary>
        /// Date and time when the entity was created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Type name of the entity for database indexing.
        /// </summary>
        [Indexed]
        public string EntityType { get; set; }

        /// <summary>
        /// Initializes a new instance of the EntityBase class.
        /// </summary>
        [JsonConstructor]
        protected EntityBase()
        {
            Id = Guid.NewGuid().ToString();
            DateCreated = DateTime.UtcNow;
            EntityType = GetType().Name;
        }
    }
}
