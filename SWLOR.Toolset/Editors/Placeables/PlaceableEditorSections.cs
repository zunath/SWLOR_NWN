namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// The two tabs only a placeable has. Built by the editor service and handed to the generic
    /// blueprint editor, which keeps the placeable's extra machinery out of every other type.
    /// </summary>
    public sealed record PlaceableEditorSections(
        AppearanceSectionViewModel Appearance,
        PlaceableBehaviorSectionViewModel Behavior);
}
