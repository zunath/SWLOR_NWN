namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>Which Appearance-tab surface an item's base item (by baseitems.2da ModelType) gets.</summary>
    public enum ItemAppearanceKind
    {
        /// <summary>ModelType 0 (simple) or 1 (layered): one gallery of picture tiles over ModelPart1.</summary>
        Gallery,

        /// <summary>ModelType 2 (composite): three model-color galleries, one per composite layer.</summary>
        Composite,

        /// <summary>ModelType 3 (armor): body-part number fields plus the six dye channels.</summary>
        ArmorParts,

        /// <summary>The base item is unknown, unresolved, or an unrecognized ModelType; nothing is offered.</summary>
        None
    }
}
