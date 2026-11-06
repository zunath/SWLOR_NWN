using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The runtime state of the tint maps: the equipment-swap color carries that are waiting to be applied, and the
    /// tint-map material tables read from the 2DAs when the module loads. The tint logic stays in the
    /// <c>TintMapService</c> and <c>TintMapMaterialRegistry</c> Feature classes.
    /// </summary>
    public interface ITintMapStateService
    {
        /// <summary>
        /// Starts (or joins) the pending carry lineage of <paramref name="sourceItem"/> for a replacement item, and
        /// returns the lineage with the edit revisions captured for the layers of <paramref name="armorPart"/>.
        /// </summary>
        (Guid Lineage, IReadOnlyDictionary<TintMapStateRevisionScope, int> Revisions) RegisterPendingItemColorCarry(
            uint sourceItem,
            uint replacementItem,
            AppearanceArmor armorPart,
            IEnumerable<TintMapLayerType> layers);

        /// <summary>Adds a replacement item to the pending carry lineage of its source item, when it has one.</summary>
        void LinkPendingItemColorCarryReplacement(uint sourceItem, uint replacementItem);

        /// <summary>Whether the item belongs to the pending carry lineage.</summary>
        bool BelongsToItemColorCarryLineage(uint item, Guid lineage);

        /// <summary>Whether no newer edit has touched the layer since the carry captured its revisions.</summary>
        bool PendingItemColorCarryLayerIsCurrent(
            Guid lineage,
            TintMapLayerType layer,
            AppearanceArmor armorPart,
            IReadOnlyDictionary<TintMapStateRevisionScope, int> capturedRevisions);

        /// <summary>Counts an edit of the layer on the item's pending carry lineage, which cancels older carries.</summary>
        void MarkPendingItemColorEdit(uint item, TintMapLayerType layer, AppearanceArmor armorPart);

        /// <summary>Ends one pending carry of the lineage and forgets the lineage when none are left.</summary>
        void CompletePendingItemColorCarry(Guid lineage);

        /// <summary>
        /// Replaces the model-to-material table with <paramref name="materials"/> and indexes the equipment material
        /// slots for the runtime tint refreshes.
        /// </summary>
        void LoadMaterials(IReadOnlyDictionary<string, List<TintMapMaterialDefinition>> materials);

        /// <summary>The materials of a model, or none.</summary>
        IReadOnlyList<TintMapMaterialDefinition> GetMaterials(string modelResref);

        bool AreEquipmentMaterialSlotsEquivalent(
            string sourceMaterialResref,
            string destinationModelResref,
            string destinationMaterialResref,
            TintMapLayerType layer);

        IReadOnlyList<string> GetEquivalentEquipmentMaterialResrefs(
            string sourceModelResref,
            string sourceMaterialResref,
            TintMapLayerType layer);

        bool IsEquipmentMaterialExclusiveToArmorPart(
            string materialResref,
            TintMapLayerType layer,
            AppearanceArmor armorPart);
    }
}
