using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// One entry of the trigger editor's behavior list: what the trigger is for, the fields that
    /// configure it, and the raw values it writes on the builder's behalf.
    /// </summary>
    /// <remarks>
    /// Local variables are reachable only under <see cref="AllowsVariables"/>, which is true for
    /// Custom alone. Every other behavior owns whichever locals it needs and exposes them as named
    /// fields, so there is never a second place to set the same thing.
    /// </remarks>
    public sealed class TriggerBehavior : IBehaviorDescriptor
    {
        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        /// <summary>Heading this behavior sits under in the list; null sits it above the groups.</summary>
        public string? Group { get; init; }

        /// <summary>Trailing clause on the list row.</summary>
        public string? Tagline { get; init; }

        /// <summary>
        /// One line under the panel's title saying what this behavior does. A sub-header, not a
        /// field: it belongs where the reader starts, not at the bottom of the form.
        /// </summary>
        public string? Summary { get; init; }

        public IReadOnlyList<BehaviorFieldDefinition> Fields { get; init; } = Array.Empty<BehaviorFieldDefinition>();

        public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } = Array.Empty<BehaviorManagedValue>();

        /// <summary>True only for Custom: the raw VarTable is the builder's to edit.</summary>
        public bool AllowsVariables { get; init; }

        /// <summary>Every local this behavior owns, whether as a row or as a managed value.</summary>
        public IEnumerable<string> OwnedLocals =>
            Fields.Where(row => row.Storage == BehaviorFieldStorage.Local).Select(row => row.Name)
                .Concat(Manages.Where(value => value.Storage == BehaviorFieldStorage.Local).Select(value => value.Name))
                .Distinct(StringComparer.Ordinal);
    }
}
