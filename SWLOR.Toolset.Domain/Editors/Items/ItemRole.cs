using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// One role a Miscellaneous/Essence/CreatureItem/Tool item can play, and the values that role
    /// owns - the item-editor equivalent of <c>DoorBehavior</c>. Unlike a door, a role is detected
    /// from itemproperty entries rather than a VarTable, so it carries no owned-local-prefix list.
    /// </summary>
    public sealed class ItemRole : IBehaviorDescriptor
    {
        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        public string? Group { get; init; }

        public string? Tagline { get; init; }

        public string? Summary { get; init; }

        public IReadOnlyList<BehaviorFieldDefinition> Fields { get; init; } = Array.Empty<BehaviorFieldDefinition>();

        public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } = Array.Empty<BehaviorManagedValue>();

        /// <summary>True only for Custom: every other role is fully described by its stat groups.</summary>
        public bool AllowsVariables { get; init; }
    }
}
