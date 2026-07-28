namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// The itemproperty ids a role owns - what <see cref="ItemRoleCatalog.Classify"/> keys off of for
    /// that role, and therefore what switching away from it clears. A role that is not detected from
    /// stored properties (KeyItem, Component, CreatureItem, Custom, DeployedDevice) owns none: nothing
    /// on the item is exclusively that role's to lose.
    /// </summary>
    public static class ItemRoleOwnership
    {
        private static readonly IReadOnlyDictionary<int, string> ExplicitLabels =
            new Dictionary<int, string> { [15] = "Cast Spell" };

        public static IReadOnlyList<int> OwnedProperties(string roleId) => roleId switch
        {
            ItemRoleCatalog.ConsumableId => new[] { 15 },
            ItemRoleCatalog.MealId => new[] { 106, 108 },
            ItemRoleCatalog.DroidPartId => new[] { 121, 122, 123, 124 },
            ItemRoleCatalog.IncubationSampleId => new[] { 127, 128, 129 },
            ItemRoleCatalog.SchematicId => new[] { 130 },
            ItemRoleCatalog.EnhancementId => new[] { 101, 102, 107, 108, 109, 110, 116 },
            _ => Array.Empty<int>()
        };

        /// <summary>
        /// A builder-facing name for an itemproperty id, for the "this will clear X" confirmation.
        /// Resolved from <see cref="ItemStatCatalog"/> where a definition exists; property 15
        /// (CastSpell) has no catalog entry of its own, so it is named explicitly here.
        /// </summary>
        public static string LabelFor(int propertyId)
        {
            var stat = ItemStatCatalog.All.FirstOrDefault(definition => definition.PropertyId == propertyId);
            if (stat != null)
                return stat.Label;

            return ExplicitLabels.TryGetValue(propertyId, out var label)
                ? label
                : $"Property {propertyId}";
        }
    }
}
