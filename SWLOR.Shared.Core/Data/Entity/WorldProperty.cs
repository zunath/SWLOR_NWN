using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class WorldProperty: EntityBase
    {
        public WorldProperty()
        {
            CustomName = string.Empty;
            CustomDescription = string.Empty;
            ParentPropertyId = string.Empty;
            ChildPropertyIds = new Dictionary<PropertyChildType, List<string>>();
            Dates = new Dictionary<PropertyDateType, DateTime>();
            Positions = new Dictionary<PropertyLocationType, PropertyLocation>();
            Taxes = new Dictionary<PropertyTaxType, int>();
            Upgrades = new Dictionary<PropertyUpgradeType, int>();
        }

        /// <summary>
        /// A custom name set by players.
        /// </summary>
        [Indexed]
        public string CustomName { get; set; }

        /// <summary>
        /// A custom description set by players.
        /// </summary>
        public string CustomDescription { get; set; }

        /// <summary>
        /// The type of property.
        /// </summary>
        [Indexed]
        public PropertyType PropertyType { get; set; }

        /// <summary>
        /// The player Id of the owner.
        /// </summary>
        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>
        /// The parent property Id
        /// </summary>
        [Indexed]
        public string ParentPropertyId { get; set; }

        /// <summary>
        /// Tracks the child property Ids contained inside this property.
        /// </summary>
        public Dictionary<PropertyChildType, List<string>> ChildPropertyIds { get; set; }

        /// <summary>
        /// Tracks dates and timers specific to this property.
        /// </summary>
        public Dictionary<PropertyDateType, DateTime> Dates { get; set; }

        /// <summary>
        /// Tracks positions specific to this property.
        /// Most properties will use a Static position but in the event the property is mobile, such as Starships,
        /// multiple positions may need to be tracked.
        /// </summary>
        public Dictionary<PropertyLocationType, PropertyLocation> Positions { get; set; }

        /// <summary>
        /// Tracks taxes specific to this property.
        /// </summary>
        public Dictionary<PropertyTaxType, int> Taxes { get; set; }

        /// <summary>
        /// Tracks upgrades specific to this property.
        /// </summary>
        public Dictionary<PropertyUpgradeType, int> Upgrades { get; set; }

        /// <summary>
        /// If true, the property will be publicly accessible.
        /// This will only be set if the PropertyType is a Building
        /// </summary>
        [Indexed]
        public bool IsPubliclyAccessible { get; set; }

        /// <summary>
        /// The layout of this property.
        /// For Starships, Apartments, and Buildings this is the interior.
        /// For Cities, this is the detail for the exterior.
        /// Structures do not use this property.
        /// </summary>
        public PropertyLayoutType Layout { get; set; }

        /// <summary>
        /// The structure type of this property.
        /// This will only be set if the PropertyType is a Structure.
        /// </summary>
        [Indexed]
        public StructureType StructureType { get; set; }

        /// <summary>
        /// If this flag is set to true, the property will be permanently removed
        /// from the database the next time the server boots up.
        /// This is delayed to avoid lag and situations where players could log into a deleted property.
        /// </summary>
        [Indexed]
        public bool IsQueuedForDeletion { get; set; }

        /// <summary>
        /// Associated item which has been serialized. Only used if the PropertyType is a Structure.
        /// </summary>
        public string SerializedItem { get; set; }

        /// <summary>
        /// The number of items that can be stored within the property.
        /// For Buildings, Apartments, and Starships, this is the maximum number of items that can be stored.
        /// For Structures, this is how much storage they increase to their parent property.
        /// </summary>
        public int ItemStorageCount { get; set; }

        /// <summary>
        /// Tracks the amount of credits contained within the property's treasury.
        /// Only applicable to City property types.
        /// </summary>
        public long Treasury { get; set; }

        /// <summary>
        /// Tracks the amount of credits required to pay off the upkeep for this property.
        /// Only applicable to City property types.
        /// </summary>
        public int Upkeep { get; set; }
    }
}
