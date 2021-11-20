using System;
using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Entity
{
    public class WorldProperty: EntityBase
    {
        public WorldProperty()
        {
            CustomName = string.Empty;
            CustomDescription = string.Empty;
            ParentPropertyId = string.Empty;
            ChildPropertyIds = new List<string>();
            Timers = new Dictionary<PropertyTimerType, DateTime>();
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
        public List<string> ChildPropertyIds { get; set; }

        /// <summary>
        /// Tracks timers specific to this property.
        /// </summary>
        public Dictionary<PropertyTimerType, DateTime> Timers { get; set; }

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
        /// Position of the property. Only used if the PropertyType is a Structure.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Orientation/facing of the property. Only used if the PropertyType is a Structure.
        /// </summary>
        public float Orientation { get; set; }

        /// <summary>
        /// Associated item which has been serialized. Only used if the PropertyType is a Structure.
        /// </summary>
        public string SerializedItem { get; set; }

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        public void SpawnIntoWorld(uint area)
        {
            // Structures represent placeables within the game world such as furniture and buildings
            if (PropertyType == PropertyType.Structure)
            {
                var furniture = Property.GetStructureByType(StructureType);

                var position = Vector3(Position.X, Position.Y, Position.Z);
                var location = Location(area, position, Orientation);

                var placeable = CreateObject(ObjectType.Placeable, furniture.Resref, location);
                Property.AssignPropertyId(placeable, Id);
            }
            // All other property types are area instances or regular areas (in the case of cities)
            else
            {
                uint targetArea;

                // If no interior layout is defined, the provided area will be used.
                if (Layout == PropertyLayoutType.Invalid)
                {
                    targetArea = area;
                }
                // If there is an interior, create an instance and use that as our target.
                else
                {
                    var layout = Property.GetLayoutByType(Layout);
                    targetArea = CreateArea(layout.AreaInstanceResref);
                    Property.RegisterInstance(Id, targetArea);

                    SetName(targetArea, CustomName);
                }

                if (ChildPropertyIds.Count > 0)
                {
                    var query = new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(Id), ChildPropertyIds);
                    var children = DB.Search(query);

                    foreach (var child in children)
                    {
                        child.SpawnIntoWorld(targetArea);
                    }
                }
            }
        }
    }
}
