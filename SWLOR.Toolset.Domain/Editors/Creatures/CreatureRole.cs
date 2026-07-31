using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>One independent gameplay role exposed on the creature Behavior tab.</summary>
    public sealed class CreatureRole : IBehaviorDescriptor
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public string? Group { get; init; }
        public string? Tagline { get; init; }
        public string? Summary { get; init; }
        public bool AllowsVariables { get; init; }
        public IReadOnlyList<BehaviorFieldDefinition> Fields { get; init; } = Array.Empty<BehaviorFieldDefinition>();
    }
}
