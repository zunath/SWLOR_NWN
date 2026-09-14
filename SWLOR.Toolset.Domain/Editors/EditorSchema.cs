using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors
{
    /// <summary>
    /// The declarative description of a blueprint editor: which resource type it edits and
    /// the grouped fields it presents. One schema per blueprint type; the generic editor view
    /// model stamps its UI from this data.
    /// </summary>
    public sealed class EditorSchema
    {
        public required ResourceType ResourceType { get; init; }

        public required IReadOnlyList<FieldGroup> Groups { get; init; }

        /// <summary>True when the schema includes the VarTable grid section.</summary>
        public bool HasVarTable { get; init; } = true;

        public IEnumerable<FieldDescriptor> AllFields => Groups.SelectMany(group => group.Fields);
    }

    /// <summary>Well-known lookup keys FieldDescriptor.LookupKey may reference. The app layer
    /// maps these to the corresponding lookup services.</summary>
    public static class LookupKeys
    {
        public const string Appearance = "appearance";
        public const string Portraits = "portraits";
        public const string Placeables = "placeables";
        public const string DoorTypes = "doortypes";
        public const string GenericDoors = "genericdoors";
        public const string AmbientSounds = "ambientsounds";
        public const string Factions = "factions";
        public const string Gender = "gender";
        public const string Phenotype = "phenotype";
        public const string SoundSets = "soundsets";
        public const string BaseItems = "baseitems";
        public const string LoadScreens = "loadscreens";
        public const string Races = "races";
        public const string CreatureMovementRates = "creaturemovementrates";

        /// <summary>Trigger "Type" - a small fixed engine enum, not a 2DA table.</summary>
        public const string TriggerTypes = "triggertypes";

        /// <summary>waypoint.2da - the marker a waypoint draws in the area view.</summary>
        public const string WaypointAppearances = "waypointappearances";
    }
}
