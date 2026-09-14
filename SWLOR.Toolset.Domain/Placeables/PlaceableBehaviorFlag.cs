namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// A GFF flag a behavior requires, such as a container needing <c>HasInventory</c>. The editor
    /// ticks these when the behavior is applied and marks them so they cannot be quietly cleared.
    /// </summary>
    /// <param name="FieldName">The byte field at the placeable's root, e.g. <c>Useable</c>.</param>
    /// <param name="Value">What the behavior needs it set to.</param>
    public sealed record PlaceableBehaviorFlag(string FieldName, bool Value);
}
