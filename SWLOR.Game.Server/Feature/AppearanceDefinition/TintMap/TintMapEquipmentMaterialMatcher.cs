using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Matches equipment materials that occupy the same slot on different wearer variants.
    /// Generated materials may use unrelated hashed resrefs between male, female, race, and
    /// phenotype models, so their position in the corresponding model is the stable identity.
    /// </summary>
    public static class TintMapEquipmentMaterialMatcher
    {
        public static bool AreEquivalent(
            string sourceMaterialResref,
            string destinationModelResref,
            string destinationMaterialResref,
            TintMapLayerType layer,
            IReadOnlyDictionary<string, IReadOnlyList<TintMapMaterialDefinition>> materialsByModel)
        {
            ArgumentNullException.ThrowIfNull(materialsByModel);

            if (!materialsByModel.TryGetValue(destinationModelResref, out var destinationMaterials))
                return false;

            var modelIdentity = GetVariantIdentity(destinationModelResref);
            var destinationSlots = destinationMaterials
                .Select((material, index) => new
                {
                    Material = material,
                    Signature = GetLayerSignature(material),
                    Slot = GetSignatureSlot(destinationMaterials, index)
                })
                .Where(entry =>
                    entry.Material.Layers.Contains(layer) &&
                    string.Equals(
                        entry.Material.Resref,
                        destinationMaterialResref,
                        StringComparison.OrdinalIgnoreCase))
                .Select(entry => (entry.Signature, entry.Slot))
                .ToHashSet();
            if (destinationSlots.Count == 0)
                return false;

            foreach (var (modelResref, materials) in materialsByModel)
            {
                if (!string.Equals(
                        GetVariantIdentity(modelResref),
                        modelIdentity,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                for (var index = 0; index < materials.Count; index++)
                {
                    var material = materials[index];
                    if (!material.Layers.Contains(layer) ||
                        !string.Equals(
                            material.Resref,
                            sourceMaterialResref,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    var sourceSlot = (
                        Signature: GetLayerSignature(material),
                        Slot: GetSignatureSlot(materials, index));
                    if (destinationSlots.Contains(sourceSlot))
                        return true;
                }
            }

            return false;
        }

        private static string GetLayerSignature(TintMapMaterialDefinition material)
        {
            return string.Join(",", material.Layers
                .Select(value => (int)value)
                .OrderBy(value => value));
        }

        private static int GetSignatureSlot(
            IReadOnlyList<TintMapMaterialDefinition> materials,
            int materialIndex)
        {
            var signature = GetLayerSignature(materials[materialIndex]);
            var slot = 0;
            for (var index = 0; index < materialIndex; index++)
            {
                if (string.Equals(
                        GetLayerSignature(materials[index]),
                        signature,
                        StringComparison.Ordinal))
                {
                    slot++;
                }
            }

            return slot;
        }

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
