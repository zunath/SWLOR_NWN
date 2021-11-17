using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public abstract class PropertyBase: EntityBase
    {
        protected PropertyBase()
        {
            CustomName = string.Empty;
            CustomDescription = string.Empty;
            ParentPropertyId = string.Empty;
            ChildPropertyIds = new List<string>();
            Timers = new Dictionary<PropertyTimerType, DateTime>();
        }

        [Indexed]
        public string CustomName { get; set; }

        public string CustomDescription { get; set; }

        [Indexed]
        public PropertyType PropertyType { get; set; }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        [Indexed]
        public string ParentPropertyId { get; set; }

        public List<string> ChildPropertyIds { get; set; }

        public Dictionary<PropertyTimerType, DateTime> Timers { get; set; }

        [Indexed]
        public bool IsPubliclyAccessible { get; set; }

        public PropertyLayoutType InteriorLayout { get; set; }

        /// <summary>
        /// If this flag is set to true, the property will be permanently removed
        /// from the database the next time the server boots up.
        /// This is delayed to avoid lag and situations where players could log into a deleted property.
        /// </summary>
        [Indexed]
        public bool IsQueuedForDeletion { get; set; }

        public abstract void SpawnIntoWorld(uint area);
    }
}
