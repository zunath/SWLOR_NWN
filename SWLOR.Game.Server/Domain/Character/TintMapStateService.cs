using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The state that used to be static in <c>TintMapService</c> (the pending equipment-swap color carries) and in
    /// <c>TintMapMaterialRegistry</c> (the tint-map material tables), moved here unchanged. Every carry operation
    /// takes the same lock as before; nothing here reaches the engine, the database or another service.
    /// </summary>
    public class TintMapStateService : ITintMapStateService
    {
        private sealed class PendingItemColorCarryLineage
        {
            public HashSet<uint> Items { get; } = new();
            public Dictionary<TintMapStateRevisionScope, int> Revisions { get; } = new();
            public int PendingCount { get; set; }
        }

        private readonly object _pendingItemColorCarryLock = new();
        private readonly Dictionary<uint, Guid> _itemColorCarryLineages = new();
        private readonly Dictionary<Guid, PendingItemColorCarryLineage> _pendingItemColorCarries = new();

        private readonly Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>> _materialsByModel =
            new(StringComparer.OrdinalIgnoreCase);
        private TintMapEquipmentMaterialIndex _equipmentMaterialIndex = TintMapEquipmentMaterialIndex.Empty;

        public (Guid Lineage, IReadOnlyDictionary<TintMapStateRevisionScope, int> Revisions) RegisterPendingItemColorCarry(
            uint sourceItem,
            uint replacementItem,
            AppearanceArmor armorPart,
            IEnumerable<TintMapLayerType> layers)
        {
            lock (_pendingItemColorCarryLock)
            {
                if (!_itemColorCarryLineages.TryGetValue(sourceItem, out var lineage) ||
                    !_pendingItemColorCarries.TryGetValue(lineage, out var state))
                {
                    lineage = Guid.NewGuid();
                    state = new PendingItemColorCarryLineage();
                    _pendingItemColorCarries[lineage] = state;
                }

                _itemColorCarryLineages[replacementItem] = lineage;
                state.Items.Add(replacementItem);
                state.PendingCount++;
                var revisions = layers
                    .Distinct()
                    .SelectMany(layer => new[]
                    {
                        new TintMapStateRevisionScope(layer, armorPart),
                        new TintMapStateRevisionScope(layer, AppearanceArmor.Invalid)
                    })
                    .Distinct()
                    .ToDictionary(
                        scope => scope,
                        scope => state.Revisions.GetValueOrDefault(scope));
                return (lineage, revisions);
            }
        }

        public void LinkPendingItemColorCarryReplacement(uint sourceItem, uint replacementItem)
        {
            lock (_pendingItemColorCarryLock)
            {
                if (!_itemColorCarryLineages.TryGetValue(sourceItem, out var lineage) ||
                    !_pendingItemColorCarries.TryGetValue(lineage, out var state))
                {
                    return;
                }

                _itemColorCarryLineages[replacementItem] = lineage;
                state.Items.Add(replacementItem);
            }
        }

        public bool BelongsToItemColorCarryLineage(uint item, Guid lineage)
        {
            lock (_pendingItemColorCarryLock)
            {
                return _itemColorCarryLineages.TryGetValue(item, out var itemLineage) &&
                       itemLineage == lineage;
            }
        }

        public bool PendingItemColorCarryLayerIsCurrent(
            Guid lineage,
            TintMapLayerType layer,
            AppearanceArmor armorPart,
            IReadOnlyDictionary<TintMapStateRevisionScope, int> capturedRevisions)
        {
            var scope = new TintMapStateRevisionScope(layer, armorPart);
            var globalScope = new TintMapStateRevisionScope(
                layer,
                AppearanceArmor.Invalid);
            lock (_pendingItemColorCarryLock)
            {
                return _pendingItemColorCarries.TryGetValue(lineage, out var state) &&
                       state.Revisions.GetValueOrDefault(scope) ==
                       capturedRevisions.GetValueOrDefault(scope) &&
                       state.Revisions.GetValueOrDefault(globalScope) ==
                       capturedRevisions.GetValueOrDefault(globalScope);
            }
        }

        public void MarkPendingItemColorEdit(
            uint item,
            TintMapLayerType layer,
            AppearanceArmor armorPart)
        {
            lock (_pendingItemColorCarryLock)
            {
                if (!_itemColorCarryLineages.TryGetValue(item, out var lineage) ||
                    !_pendingItemColorCarries.TryGetValue(lineage, out var state))
                {
                    return;
                }

                var scope = new TintMapStateRevisionScope(layer, armorPart);
                state.Revisions[scope] = state.Revisions.GetValueOrDefault(scope) + 1;
            }
        }

        public void CompletePendingItemColorCarry(Guid lineage)
        {
            lock (_pendingItemColorCarryLock)
            {
                if (!_pendingItemColorCarries.TryGetValue(lineage, out var state))
                    return;

                state.PendingCount--;
                if (state.PendingCount > 0)
                    return;

                foreach (var item in state.Items)
                {
                    if (_itemColorCarryLineages.TryGetValue(item, out var itemLineage) &&
                        itemLineage == lineage)
                    {
                        _itemColorCarryLineages.Remove(item);
                    }
                }

                _pendingItemColorCarries.Remove(lineage);
            }
        }

        public void LoadMaterials(IReadOnlyDictionary<string, List<TintMapMaterialDefinition>> materials)
        {
            _materialsByModel.Clear();
            foreach (var (model, modelMaterials) in materials)
            {
                _materialsByModel[model] = modelMaterials;
            }
            _equipmentMaterialIndex = new TintMapEquipmentMaterialIndex(_materialsByModel);
        }

        public IReadOnlyList<TintMapMaterialDefinition> GetMaterials(string modelResref)
        {
            if (string.IsNullOrWhiteSpace(modelResref))
                return Array.Empty<TintMapMaterialDefinition>();

            return _materialsByModel.TryGetValue(modelResref, out var materials)
                ? materials
                : Array.Empty<TintMapMaterialDefinition>();
        }

        public bool AreEquipmentMaterialSlotsEquivalent(
            string sourceMaterialResref,
            string destinationModelResref,
            string destinationMaterialResref,
            TintMapLayerType layer)
        {
            return _equipmentMaterialIndex.AreEquivalent(
                sourceMaterialResref,
                destinationModelResref,
                destinationMaterialResref,
                layer);
        }

        public IReadOnlyList<string> GetEquivalentEquipmentMaterialResrefs(
            string sourceModelResref,
            string sourceMaterialResref,
            TintMapLayerType layer)
        {
            return _equipmentMaterialIndex.GetEquivalentMaterialResrefs(
                sourceModelResref,
                sourceMaterialResref,
                layer);
        }

        public bool IsEquipmentMaterialExclusiveToArmorPart(
            string materialResref,
            TintMapLayerType layer,
            AppearanceArmor armorPart)
        {
            return _equipmentMaterialIndex.IsExclusiveToArmorPart(
                materialResref,
                layer,
                armorPart);
        }
    }
}
