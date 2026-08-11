namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Matches equipment materials that occupy the same slot on different wearer variants.
    /// Generated materials may use unrelated hashed resrefs between male, female, race, and
    /// phenotype models, so their position in the corresponding model is the stable identity.
    /// </summary>
    public static class TintMapEquipmentMaterialMatcher
    {
        /// <summary>
        /// Removes a parts-model wearer prefix (p + gender + race + phenotype + underscore).
        /// The phenotype portion may contain more than one digit.
        /// </summary>
        public static string GetVariantIdentity(string resref)
        {
            if (string.IsNullOrWhiteSpace(resref) ||
                resref.Length <= 5 ||
                char.ToLowerInvariant(resref[0]) != 'p')
            {
                return resref;
            }

            var separator = resref.IndexOf('_', 3);
            if (separator < 4)
                return resref;

            for (var index = 3; index < separator; index++)
            {
                if (!char.IsDigit(resref[index]))
                    return resref;
            }

            return resref[(separator + 1)..];
        }
    }
}
