using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The runtime state of the tint maps (pending equipment-swap color carries and the material tables). The state
    /// lives in <see cref="ITintMapStateService"/>; every member forwards to the container's instance.
    /// </summary>
    public static class TintMapState
    {
        private static ITintMapStateService Instance => FacadeAccessor.Resolve<ITintMapStateService>();

        public static (Guid Lineage, IReadOnlyDictionary<TintMapStateRevisionScope, int> Revisions) RegisterPendingItemColorCarry(
            uint sourceItem,
            uint replacementItem,
            AppearanceArmor armorPart,
            IEnumerable<TintMapLayerType> layers)
        {
            return Instance.RegisterPendingItemColorCarry(sourceItem, replacementItem, armorPart, layers);
        }

        public static void LinkPendingItemColorCarryReplacement(uint sourceItem, uint replacementItem)
        {
            Instance.LinkPendingItemColorCarryReplacement(sourceItem, replacementItem);
        }

        public static bool BelongsToItemColorCarryLineage(uint item, Guid lineage)
        {
            return Instance.BelongsToItemColorCarryLineage(item, lineage);
        }

        public static bool PendingItemColorCarryLayerIsCurrent(
            Guid lineage,
            TintMapLayerType layer,
            AppearanceArmor armorPart,
            IReadOnlyDictionary<TintMapStateRevisionScope, int> capturedRevisions)
        {
            return Instance.PendingItemColorCarryLayerIsCurrent(lineage, layer, armorPart, capturedRevisions);
        }

        public static void MarkPendingItemColorEdit(uint item, TintMapLayerType layer, AppearanceArmor armorPart)
        {
            Instance.MarkPendingItemColorEdit(item, layer, armorPart);
        }

        public static void CompletePendingItemColorCarry(Guid lineage)
        {
            Instance.CompletePendingItemColorCarry(lineage);
        }

        public static void LoadMaterials(IReadOnlyDictionary<string, List<TintMapMaterialDefinition>> materials)
        {
            Instance.LoadMaterials(materials);
        }

        public static IReadOnlyList<TintMapMaterialDefinition> GetMaterials(string modelResref)
        {
            return Instance.GetMaterials(modelResref);
        }

        public static bool AreEquipmentMaterialSlotsEquivalent(
            string sourceMaterialResref,
            string destinationModelResref,
            string destinationMaterialResref,
            TintMapLayerType layer)
        {
            return Instance.AreEquipmentMaterialSlotsEquivalent(
                sourceMaterialResref, destinationModelResref, destinationMaterialResref, layer);
        }

        public static IReadOnlyList<string> GetEquivalentEquipmentMaterialResrefs(
            string sourceModelResref,
            string sourceMaterialResref,
            TintMapLayerType layer)
        {
            return Instance.GetEquivalentEquipmentMaterialResrefs(sourceModelResref, sourceMaterialResref, layer);
        }

        public static bool IsEquipmentMaterialExclusiveToArmorPart(
            string materialResref,
            TintMapLayerType layer,
            AppearanceArmor armorPart)
        {
            return Instance.IsEquipmentMaterialExclusiveToArmorPart(materialResref, layer, armorPart);
        }
    }
}
