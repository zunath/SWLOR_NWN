using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>One behavior offered by the ambient-sound editor.</summary>
    public sealed class SoundBehavior : IBehaviorDescriptor
    {
        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        public string? Group { get; init; }

        public string? Tagline { get; init; }

        public string? Summary { get; init; }

        public IReadOnlyList<BehaviorFieldDefinition> Fields { get; init; } =
            Array.Empty<BehaviorFieldDefinition>();

        public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } =
            Array.Empty<BehaviorManagedValue>();

        public bool AllowsVariables { get; init; }

        public bool IsLoop => Id is SoundBehaviorCatalog.PointLoopId or SoundBehaviorCatalog.AreaLoopId;
    }
}
