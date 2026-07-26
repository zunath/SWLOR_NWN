using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>One role a door can play and the values that role owns.</summary>
    public sealed class DoorBehavior : IBehaviorDescriptor
    {
        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        public string? Group { get; init; }

        public string? Tagline { get; init; }

        public string? Summary { get; init; }

        public IReadOnlyList<DoorFieldDefinition> Fields { get; init; } = Array.Empty<DoorFieldDefinition>();

        public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } = Array.Empty<BehaviorManagedValue>();

        public IReadOnlyList<string> OwnedLocalPrefixes { get; init; } = Array.Empty<string>();

        public bool AllowsVariables { get; init; }
    }
}
