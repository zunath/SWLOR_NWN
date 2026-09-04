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

        /// <summary>
        /// Produces a side-neutral identity for a material used by one member of a mirrored body
        /// part pair. This lets asymmetric material lists match their actual left/right material
        /// instead of relying on unrelated list positions.
        /// </summary>
        public static string GetMirroredPartIdentity(string materialResref, string partName)
        {
            var identity = GetVariantIdentity(materialResref)?.ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(partName))
                return identity;

            identity = identity.Replace(partName.ToLowerInvariant(), "{part}", StringComparison.Ordinal);
            var abbreviatedToken = partName.ToLowerInvariant() switch
            {
                "forel" => "_lf_",
                "forer" => "_rf_",
                "bicepl" => "_lb_",
                "bicepr" => "_rb_",
                "handl" => "_lh_",
                "handr" => "_rh_",
                "shol" => "_ls_",
                "shor" => "_rs_",
                "legl" => "_ll_",
                "legr" => "_rl_",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(abbreviatedToken))
                identity = identity.Replace(abbreviatedToken, "_{part}_", StringComparison.Ordinal);

            return identity;
        }
    }
}
