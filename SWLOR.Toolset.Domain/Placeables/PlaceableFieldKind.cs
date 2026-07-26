namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// How one of a behavior's local variables is presented. The Domain layer only carries intent;
    /// the app maps each kind to a control the same way <see cref="Editors.EditorKind"/> is mapped.
    /// </summary>
    public enum PlaceableFieldKind
    {
        /// <summary>Free text stored as a string local.</summary>
        Text,

        /// <summary>Whole number stored as an int local.</summary>
        Integer,

        /// <summary>Yes/no stored as an int local holding 0 or 1.</summary>
        Toggle,

        /// <summary>
        /// One of a known set of values. The set comes from <see cref="PlaceableValueSource"/>;
        /// the stored local is a string or an int depending on the source.
        /// </summary>
        Choice
    }
}
