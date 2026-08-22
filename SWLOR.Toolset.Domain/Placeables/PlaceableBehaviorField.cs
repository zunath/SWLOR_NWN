using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// One local variable a behavior owns, described well enough to render a typed control,
    /// validate the stored value, and know whether the behavior can work without it.
    /// </summary>
    public sealed class PlaceableBehaviorField
    {
        /// <summary>The local variable's name, exactly as the game code reads it.</summary>
        public required string VariableName { get; init; }

        /// <summary>What the field is called on screen.</summary>
        public required string Label { get; init; }

        public required PlaceableFieldKind Kind { get; init; }

        /// <summary>Where legal values come from for a <see cref="PlaceableFieldKind.Choice"/>.</summary>
        public PlaceableValueSource Source { get; init; } = PlaceableValueSource.None;

        /// <summary>
        /// The behavior does not work without this value, so the editor marks it and validation
        /// reports it when empty.
        /// </summary>
        public bool IsRequired { get; init; }

        /// <summary>Optional help text shown under the control.</summary>
        public string? Description { get; init; }

        /// <summary>Text shown when an optional choice has no saved value.</summary>
        public string EmptyChoiceLabel { get; init; } = "Not selected";

        /// <summary>Action text used to remove an optional saved choice.</summary>
        public string ClearChoiceLabel { get; init; } = "Clear";

        /// <summary>Smallest value the numeric editor accepts, when the game has a real lower bound.</summary>
        public int? Minimum { get; init; }

        /// <summary>Largest value the numeric editor accepts, when the game has a real upper bound.</summary>
        public int? Maximum { get; init; }

        /// <summary>
        /// Initial integer value written when this behavior is selected and the variable is absent.
        /// Existing authored values are never replaced.
        /// </summary>
        public int? DefaultIntValue { get; init; }

        /// <summary>
        /// Initial string value written when this behavior is selected and the variable is absent.
        /// Existing authored values are never replaced.
        /// </summary>
        public string? DefaultStringValue { get; init; }

        /// <summary>
        /// False for a fixed implementation detail the behavior owns but the builder should not
        /// have to choose, such as the market terminal's conversation class.
        /// </summary>
        public bool IsVisible { get; init; } = true;

        /// <summary>
        /// The VarTable type this field is stored as: <see cref="VarTable.TypeInt"/> for numbers
        /// and toggles, <see cref="VarTable.TypeString"/> for text. Choice fields follow their
        /// source - key items, skills and visual effects are ids, the rest are names.
        /// </summary>
        public int VarType => Kind switch
        {
            PlaceableFieldKind.Integer => VarTable.TypeInt,
            PlaceableFieldKind.Toggle => VarTable.TypeInt,
            PlaceableFieldKind.Choice => Source is PlaceableValueSource.KeyItems
                or PlaceableValueSource.SkillTypes
                or PlaceableValueSource.MarketRegions
                or PlaceableValueSource.VisualEffects
                ? VarTable.TypeInt
                : VarTable.TypeString,
            _ => VarTable.TypeString
        };
    }
}
