using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Precomputed equipment-material slots grouped by wearer-variant identity. Runtime tint
    /// refreshes use this index instead of rescanning the complete tint-map model registry.
    /// </summary>
    public sealed class TintMapEquipmentMaterialIndex
    {
        private readonly HashSet<string> _models = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<MaterialLayerKey, HashSet<MaterialSlot>>>
            _slotsByModel = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<MaterialLayerKey, HashSet<MaterialSlot>>>
            _slotsByVariant = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<LayerSlotKey, HashSet<string>>>
            _materialsByVariantSlot = new(StringComparer.OrdinalIgnoreCase);

        public static TintMapEquipmentMaterialIndex Empty { get; } = new(
            new Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>>());

        public TintMapEquipmentMaterialIndex(
            IReadOnlyDictionary<string, IReadOnlyList<TintMapMaterialDefinition>> materialsByModel)
        {
            ArgumentNullException.ThrowIfNull(materialsByModel);

            foreach (var (modelResref, materials) in materialsByModel)
            {
                _models.Add(modelResref);
                var variantIdentity = TintMapEquipmentMaterialMatcher.GetVariantIdentity(modelResref);
                var modelSlots = GetOrAdd(_slotsByModel, modelResref);
                var variantSlots = GetOrAdd(_slotsByVariant, variantIdentity);
                var variantMaterials = GetOrAdd(_materialsByVariantSlot, variantIdentity);
                var nextSlotBySignature = new Dictionary<string, int>(StringComparer.Ordinal);

                for (var index = 0; index < materials.Count; index++)
                {
                    var material = materials[index];
                    var signature = GetLayerSignature(material);
                    var slot = new MaterialSlot(
                        signature,
                        nextSlotBySignature.GetValueOrDefault(signature));
                    nextSlotBySignature[signature] = slot.Index + 1;
                    foreach (var layer in material.Layers.Distinct())
                    {
                        var materialKey = new MaterialLayerKey(
                            Normalize(material.Resref),
                            layer);
                        GetOrAdd(modelSlots, materialKey).Add(slot);
                        GetOrAdd(variantSlots, materialKey).Add(slot);
                        GetOrAddStringSet(
                                variantMaterials,
                                new LayerSlotKey(layer, slot.Signature, slot.Index))
                            .Add(material.Resref);
                    }
                }
            }
        }

        public bool AreEquivalent(
            string sourceMaterialResref,
            string destinationModelResref,
            string destinationMaterialResref,
            TintMapLayerType layer)
        {
            if (!_slotsByModel.TryGetValue(destinationModelResref, out var destinationMaterials) ||
                !destinationMaterials.TryGetValue(
                    new MaterialLayerKey(Normalize(destinationMaterialResref), layer),
                    out var destinationSlots) ||
                destinationSlots.Count == 0)
            {
                return false;
            }

            var variantIdentity = TintMapEquipmentMaterialMatcher.GetVariantIdentity(
                destinationModelResref);
            if (!_slotsByVariant.TryGetValue(variantIdentity, out var variantMaterials) ||
                !variantMaterials.TryGetValue(
                    new MaterialLayerKey(Normalize(sourceMaterialResref), layer),
                    out var sourceSlots))
            {
                return false;
            }

            // A stored override identifies only its material resref, not the wearer model that
            // produced it. Ambiguous resrefs must not spread a part-specific tint to another slot.
            return sourceSlots.Count == 1 && destinationSlots.Contains(sourceSlots.Single());
        }

        public IReadOnlyList<string> GetEquivalentMaterialResrefs(
            string sourceModelResref,
            string sourceMaterialResref,
            TintMapLayerType layer)
        {
            if (!_models.Contains(sourceModelResref))
                return Array.Empty<string>();

            var variantIdentity = TintMapEquipmentMaterialMatcher.GetVariantIdentity(sourceModelResref);
            if (string.IsNullOrWhiteSpace(variantIdentity) ||
                !_slotsByVariant.TryGetValue(variantIdentity, out var variantMaterials) ||
                !variantMaterials.TryGetValue(
                    new MaterialLayerKey(Normalize(sourceMaterialResref), layer),
                    out var sourceSlots) ||
                sourceSlots.Count != 1 ||
                !_materialsByVariantSlot.TryGetValue(variantIdentity, out var materialsBySlot))
            {
                return Array.Empty<string>();
            }

            var sourceSlot = sourceSlots.Single();
            return materialsBySlot.TryGetValue(
                    new LayerSlotKey(layer, sourceSlot.Signature, sourceSlot.Index),
                    out var materialResrefs)
                ? materialResrefs.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList()
                : Array.Empty<string>();
        }

        private static Dictionary<TKey, HashSet<TValue>> GetOrAdd<TKey, TValue>(
            Dictionary<string, Dictionary<TKey, HashSet<TValue>>> values,
            string key)
            where TKey : notnull
        {
            if (values.TryGetValue(key, out var result))
                return result;

            result = new Dictionary<TKey, HashSet<TValue>>();
            values[key] = result;
            return result;
        }

        private static HashSet<TValue> GetOrAdd<TKey, TValue>(
            Dictionary<TKey, HashSet<TValue>> values,
            TKey key)
            where TKey : notnull
        {
            if (values.TryGetValue(key, out var result))
                return result;

            result = new HashSet<TValue>();
            values[key] = result;
            return result;
        }

        private static HashSet<string> GetOrAddStringSet(
            Dictionary<LayerSlotKey, HashSet<string>> values,
            LayerSlotKey key)
        {
            if (values.TryGetValue(key, out var result))
                return result;

            result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            values[key] = result;
            return result;
        }

        private static string GetLayerSignature(TintMapMaterialDefinition material)
        {
            return string.Join(",", material.Layers
                .Select(value => (int)value)
                .OrderBy(value => value));
        }

        private static string Normalize(string value) => value?.ToLowerInvariant() ?? string.Empty;

        private readonly record struct MaterialLayerKey(string MaterialResref, TintMapLayerType Layer);
        private readonly record struct MaterialSlot(string Signature, int Index);
        private readonly record struct LayerSlotKey(
            TintMapLayerType Layer,
            string Signature,
            int Index);
    }
}
