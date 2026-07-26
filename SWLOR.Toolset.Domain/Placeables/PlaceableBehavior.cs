namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// What a placeable does in SWLOR, declared once: the script slots the server expects, the
    /// flags it needs set, and the local variables that configure it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A behavior is a <b>view over stored GFF</b>, never a stored value of its own. Nothing in the
    /// .utp or .git records "this is a scavenge point" - the behavior is recognised from the script
    /// slots and variable names already there (see <see cref="PlaceableBehaviorDetector"/>), and
    /// applying one only writes fields the game already reads. That keeps the round trip
    /// byte-identical for a placeable nobody edits.
    /// </para>
    /// <para>
    /// One declaration drives four things: the behavior list, the field set shown when it is
    /// selected, the script slots and flags written on apply, and validation.
    /// </para>
    /// </remarks>
    public sealed class PlaceableBehavior
    {
        /// <summary>Stable identifier used by tests and settings; never shown to a builder.</summary>
        public required string Id { get; init; }

        /// <summary>What the behavior is called in the list.</summary>
        public required string Name { get; init; }

        /// <summary>Heading this behavior sits under in the list.</summary>
        public required string Group { get; init; }

        /// <summary>
        /// Script slots this behavior owns, as GFF field name to script resref (e.g.
        /// <c>OnUsed</c> to <c>res_used</c>). Applying the behavior writes these; switching away
        /// clears them.
        /// </summary>
        public IReadOnlyDictionary<string, string> Scripts { get; init; } =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Script resrefs that also identify this behavior but are never written by it - the
        /// base-game and CEP equivalents the module already uses. A chair is a chair whether it
        /// runs SWLOR's <c>sit</c> or the 2,181 instances still on <c>zep_use_chair</c>; applying
        /// the behavior writes SWLOR's, recognising it accepts either.
        /// </summary>
        public IReadOnlyList<string> AlternateScripts { get; init; } = Array.Empty<string>();

        /// <summary>Flags the behavior requires, ticked on apply and marked in the editor.</summary>
        public IReadOnlyList<PlaceableBehaviorFlag> Flags { get; init; } = Array.Empty<PlaceableBehaviorFlag>();

        /// <summary>
        /// Root flags this behavior lets the builder choose. These are controls, not requirements:
        /// applying or saving the behavior preserves the selected value.
        /// </summary>
        public IReadOnlyList<PlaceableBehaviorEditableFlag> EditableFlags { get; init; } =
            Array.Empty<PlaceableBehaviorEditableFlag>();

        /// <summary>The local variables that configure this behavior, in the order shown.</summary>
        public IReadOnlyList<PlaceableBehaviorField> Fields { get; init; } = Array.Empty<PlaceableBehaviorField>();

        /// <summary>
        /// True for the two behaviors that describe an absence rather than a system: Decor (no
        /// behavior wiring) and Custom (wiring no declaration covers).
        /// </summary>
        public bool IsSentinel { get; init; }

        /// <summary>
        /// True when the builder edits variables directly rather than through typed fields, which
        /// is what makes the Variables tab appear.
        /// </summary>
        public bool AllowsRawEditing { get; init; }

        /// <summary>Variable names this behavior owns; everything else on the placeable is unmanaged.</summary>
        public IEnumerable<string> VariableNames => Fields.Select(declared => declared.VariableName);
    }
}
