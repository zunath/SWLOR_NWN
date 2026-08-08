namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// A root-level placeable flag the builder may choose for one behavior. Unlike
    /// <see cref="PlaceableBehaviorFlag"/>, this is not a required implementation value and must
    /// never be overwritten when the behavior is applied or finalized for saving.
    /// </summary>
    /// <param name="FieldName">The byte field at the placeable root.</param>
    /// <param name="Label">The builder-facing checkbox label.</param>
    /// <param name="Description">Optional help shown with the checkbox.</param>
    public sealed record PlaceableBehaviorEditableFlag(
        string FieldName,
        string Label,
        string? Description = null);
}
