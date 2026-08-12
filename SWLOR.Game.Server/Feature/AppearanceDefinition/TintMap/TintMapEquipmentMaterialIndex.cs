using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Precomputed equipment-material slots grouped by wearer-variant identity. Runtime tint
    /// refreshes use this index instead of rescanning the complete tint-map model registry.
    /// </summary>
    public sealed class TintMapEquipmentMaterialIndex
    {
        private readonly HashSet<string> _models = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<MaterialLayerKey, HashSet<int>>>
            _slotsByModel = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<MaterialLayerKey, HashSet<int>>>
            _slotsByVariant = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<LayerSlotKey, HashSet<string>>>
            _materialsByVariantSlot = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<MaterialLayerKey, HashSet<AppearanceArmor>>
            _armorPartsByMaterial = new();

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
                var hasArmorPart = TryGetArmorPart(variantIdentity, out var armorPart);
                var modelSlots = GetOrAdd(_slotsByModel, modelResref);
                var variantSlots = GetOrAdd(_slotsByVariant, variantIdentity);
                var variantMaterials = GetOrAdd(_materialsByVariantSlot, variantIdentity);
                var nextSlotByLayer = new Dictionary<TintMapLayerType, int>();

                for (var index = 0; index < materials.Count; index++)
                {
                    var material = materials[index];
                    foreach (var layer in material.Layers.Distinct())
                    {
                        var slot = nextSlotByLayer.GetValueOrDefault(layer);
                        nextSlotByLayer[layer] = slot + 1;
                        var materialKey = new MaterialLayerKey(
                            Normalize(material.Resref),
                            layer);
                        GetOrAdd(modelSlots, materialKey).Add(slot);
                        GetOrAdd(variantSlots, materialKey).Add(slot);
                        GetOrAddStringSet(
                                variantMaterials,
                                new LayerSlotKey(layer, slot))
                            .Add(material.Resref);
                        if (hasArmorPart)
                            GetOrAdd(_armorPartsByMaterial, materialKey).Add(armorPart);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true only when the registry proves that a material belongs exclusively to the
        /// requested modular armor part. Shared or unknown materials fail closed so clearing an
        /// inactive part cannot erase a sibling part's persisted color.
        /// </summary>
        public bool IsExclusiveToArmorPart(
            string materialResref,
            TintMapLayerType layer,
            AppearanceArmor armorPart)
        {
            return _armorPartsByMaterial.TryGetValue(
                       new MaterialLayerKey(Normalize(materialResref), layer),
                       out var armorParts) &&
                   armorParts.Count == 1 &&
                   armorParts.Contains(armorPart);
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

            var normalizedSource = Normalize(sourceMaterialResref);
            var normalizedDestination = Normalize(destinationMaterialResref);
            var sourceIdentity = TintMapEquipmentMaterialMatcher.GetVariantIdentity(normalizedSource);
            var destinationIdentity = TintMapEquipmentMaterialMatcher.GetVariantIdentity(
                normalizedDestination);
            if (!normalizedSource.Equals(normalizedDestination, StringComparison.Ordinal) &&
                !sourceIdentity.Equals(normalizedSource, StringComparison.Ordinal) &&
                !destinationIdentity.Equals(normalizedDestination, StringComparison.Ordinal) &&
                sourceIdentity.Equals(destinationIdentity, StringComparison.Ordinal) &&
                destinationSlots.Count == 1 &&
                !destinationMaterials.ContainsKey(new MaterialLayerKey(normalizedSource, layer)))
            {
                // Some converted models add a semantic material on one wearer variant, shifting
                // every later per-layer slot. The natural material identity is stronger evidence
                // than that shifted position, unless the destination also contains the source
                // resref (which would make the identity ambiguous between two distinct meshes).
                return true;
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

            var hasCorrespondingNaturalSource =
                !destinationIdentity.Equals(normalizedDestination, StringComparison.Ordinal) &&
                variantMaterials.Keys.Any(key =>
                    key.Layer == layer &&
                    !key.MaterialResref.Equals(normalizedDestination, StringComparison.Ordinal) &&
                    TintMapEquipmentMaterialMatcher.GetVariantIdentity(key.MaterialResref)
                        .Equals(destinationIdentity, StringComparison.Ordinal));
            if (hasCorrespondingNaturalSource &&
                !sourceIdentity.Equals(destinationIdentity, StringComparison.Ordinal))
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
                    new LayerSlotKey(layer, sourceSlot),
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

        private static string Normalize(string value) => value?.ToLowerInvariant() ?? string.Empty;

        private static bool TryGetArmorPart(
            string variantIdentity,
            out AppearanceArmor armorPart)
        {
            var identity = variantIdentity?.ToLowerInvariant() ?? string.Empty;
            var partName = identity.TakeWhile(character => !char.IsDigit(character)).ToArray();
            if (partName.Length == identity.Length ||
                !int.TryParse(identity[partName.Length..], out _))
            {
                armorPart = AppearanceArmor.Invalid;
                return false;
            }

            armorPart = new string(partName) switch
            {
                "footr" => AppearanceArmor.RightFoot,
                "footl" => AppearanceArmor.LeftFoot,
                "shinr" => AppearanceArmor.RightShin,
                "shinl" => AppearanceArmor.LeftShin,
                "legl" => AppearanceArmor.LeftThigh,
                "legr" => AppearanceArmor.RightThigh,
                "pelvis" => AppearanceArmor.Pelvis,
                "chest" => AppearanceArmor.Torso,
                "belt" => AppearanceArmor.Belt,
                "neck" => AppearanceArmor.Neck,
                "forer" => AppearanceArmor.RightForearm,
                "forel" => AppearanceArmor.LeftForearm,
                "bicepr" => AppearanceArmor.RightBicep,
                "bicepl" => AppearanceArmor.LeftBicep,
                "shor" => AppearanceArmor.RightShoulder,
                "shol" => AppearanceArmor.LeftShoulder,
                "handr" => AppearanceArmor.RightHand,
                "handl" => AppearanceArmor.LeftHand,
                "robe" => AppearanceArmor.Robe,
                _ => AppearanceArmor.Invalid
            };
            return armorPart != AppearanceArmor.Invalid;
        }

        private readonly record struct MaterialLayerKey(string MaterialResref, TintMapLayerType Layer);
        private readonly record struct LayerSlotKey(TintMapLayerType Layer, int Index);
    }
}
