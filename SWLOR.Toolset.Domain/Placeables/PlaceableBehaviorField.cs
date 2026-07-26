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
                or PlaceableValueSource.VisualEffects
                ? VarTable.TypeInt
                : VarTable.TypeString,
            _ => VarTable.TypeString
        };
    }
}
