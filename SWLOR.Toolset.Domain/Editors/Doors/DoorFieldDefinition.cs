using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>A shared behavior field with the few conditional/composite rules doors need.</summary>
    public sealed class DoorFieldDefinition : BehaviorFieldDefinition
    {
        public DoorFieldSpecial Special { get; init; }

        /// <summary>The row appears only while this integer field equals <see cref="VisibleWhenValue"/>.</summary>
        public string? VisibleWhenField { get; init; }

        public long VisibleWhenValue { get; init; } = 1;

        /// <summary>A text edit sets this byte to one when non-empty and zero when empty.</summary>
        public string? NonEmptySetsField { get; init; }
    }
}
