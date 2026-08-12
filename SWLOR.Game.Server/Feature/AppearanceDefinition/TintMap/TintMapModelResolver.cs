using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class TintMapModelResolver
    {
        private static readonly IReadOnlyDictionary<CreaturePart, string> BodyPartNames =
            new Dictionary<CreaturePart, string>
            {
                { CreaturePart.RightFoot, "footr" },
                { CreaturePart.LeftFoot, "footl" },
                { CreaturePart.RightShin, "shinr" },
                { CreaturePart.LeftShin, "shinl" },
                { CreaturePart.LeftThigh, "legl" },
                { CreaturePart.RightThigh, "legr" },
                { CreaturePart.Pelvis, "pelvis" },
                { CreaturePart.Torso, "chest" },
                { CreaturePart.Belt, "belt" },
                { CreaturePart.Neck, "neck" },
                { CreaturePart.RightForearm, "forer" },
                { CreaturePart.LeftForearm, "forel" },
                { CreaturePart.RightBicep, "bicepr" },
                { CreaturePart.LeftBicep, "bicepl" },
                { CreaturePart.RightShoulder, "shor" },
                { CreaturePart.LeftShoulder, "shol" },
                { CreaturePart.RightHand, "handr" },
                { CreaturePart.LeftHand, "handl" },
                { CreaturePart.Head, "head" }
            };

        public static IReadOnlyList<TintMapMaterialSelection> GetCurrentSelections(uint creature)
        {
            var selections = new List<TintMapMaterialSelection>();
            var seenSelections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var appearanceRow = (int)GetAppearanceType(creature);
            var modelType = Get2DAString("appearance", "MODELTYPE", appearanceRow);

            if (modelType.StartsWith("P", StringComparison.OrdinalIgnoreCase))
            {
                AddPartsAppearanceSelections(creature, selections, seenSelections);
            }
            else
            {
                AddTableModelSelections(
                    "appearance",
                    "RACE",
                    appearanceRow,
                    creature,
                    creature,
                    false,
                    selections,
                    seenSelections);

                // Non-parts appearances still render simple equipped models. Their base body is a
                // single appearance.RACE model rather than assembled phenotype parts, but a visible
                // helmet or cloak remains item-owned and its custom tints must be edited on that item.
                AddSimpleItemSelections(creature, InventorySlot.Head, "helm", selections, seenSelections);
                AddCloakSelections(creature, string.Empty, selections, seenSelections);
            }

            var equipmentPaletteSource = GetItemInSlot(InventorySlot.Chest, creature);
            var appendagesUseItemColors = GetIsObjectValid(equipmentPaletteSource);
            var appendagePaletteSource = appendagesUseItemColors
                ? equipmentPaletteSource
                : creature;
            AddTableModelSelections(
                "wingmodel",
                "MODEL",
                (int)GetCreatureWingType(creature),
                appendagePaletteSource,
                creature,
                appendagesUseItemColors,
                selections,
                seenSelections);
            AddTableModelSelections(
                "tailmodel",
                "MODEL",
                (int)GetCreatureTailType(creature),
                appendagePaletteSource,
                creature,
                appendagesUseItemColors,
                selections,
                seenSelections);

            return selections;
        }

        public static IReadOnlyList<TintMapMaterialSelection> GetWorldItemSelections(uint item)
        {
            var selections = new List<TintMapMaterialSelection>();
            if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
                return selections;

            var baseItem = (int)GetBaseItemType(item);
            var itemClass = Get2DAString("baseitems", "ItemClass", baseItem).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(itemClass) || itemClass == "****")
                return selections;

            var seenSelections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!int.TryParse(Get2DAString("baseitems", "ModelType", baseItem), out var modelType))
                return selections;

            if (modelType is 0 or 1)
            {
                var modelId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0);
                if (modelId > 0)
                {
                    var overrideModel = string.Empty;
                    if (itemClass == "cloak" &&
                        int.TryParse(Get2DAString("cloakmodel", "TEXTURE", modelId), out var textureId) &&
                        textureId > 0 && textureId != modelId)
                    {
                        overrideModel = $"cloak_{textureId:D3}";
                    }

                    AddModelSelections(
                        selections,
                        seenSelections,
                        $"{itemClass}_{modelId:D3}",
                        item,
                        item,
                        true,
                        AppearanceArmor.Invalid,
                        overrideModel);
                }
            }
            else if (modelType == 2)
            {
                AddWorldItemPart(
                    item, itemClass, "b", AppearanceWeapon.Bottom, selections, seenSelections);
                AddWorldItemPart(
                    item, itemClass, "m", AppearanceWeapon.Middle, selections, seenSelections);
                AddWorldItemPart(
                    item, itemClass, "t", AppearanceWeapon.Top, selections, seenSelections);
            }

            // ModelType 3 armor uses the engine's generic ground bag. Its tintable body-part
            // models only exist while worn and are already handled by GetCurrentSelections.
            return selections;
        }

        private static void AddWorldItemPart(
            uint item,
            string itemClass,
            string partName,
            AppearanceWeapon part,
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections)
        {
            var modelId = GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)part);
            if (modelId <= 0)
                return;

            AddModelSelections(
                selections,
                seenSelections,
                $"{itemClass}_{partName}_{modelId:D3}",
                item,
                item,
                true,
                AppearanceArmor.Invalid);
        }

        private static void AddPartsAppearanceSelections(
            uint creature,
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections)
        {
            var prefix = GetCreatureModelPrefix(creature);
            if (string.IsNullOrWhiteSpace(prefix))
                return;

            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            var hasArmor = GetIsObjectValid(armor);

            foreach (var (part, partName) in BodyPartNames)
            {
                var usesItemColors = part != CreaturePart.Head && hasArmor;
                var armorPart = usesItemColors
                    ? (AppearanceArmor)(int)part
                    : AppearanceArmor.Invalid;
                var creaturePartId = GetCreatureBodyPart(part, creature);
                var armorPartId = usesItemColors
                    ? GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)armorPart)
                    : 0;
                var partId = ResolvePartId(creaturePartId, armorPartId, usesItemColors);
                if (partId <= 0)
                    continue;

                var model = $"{prefix}{partName}{partId:D3}".ToLowerInvariant();
                AddModelSelections(
                    selections,
                    seenSelections,
                    model,
                    usesItemColors ? armor : creature,
                    creature,
                    usesItemColors,
                    armorPart);
            }

            if (hasArmor)
            {
                var robeId = GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);
                if (robeId > 0)
                {
                    AddModelSelections(
                        selections,
                        seenSelections,
                        $"{prefix}robe{robeId:D3}".ToLowerInvariant(),
                        armor,
                        creature,
                        true,
                        AppearanceArmor.Robe);
                }
            }

            AddSimpleItemSelections(creature, InventorySlot.Head, "helm", selections, seenSelections);
            AddCloakSelections(creature, prefix, selections, seenSelections);
        }

        private static int ResolvePartId(int creaturePartId, int armorPartId, bool usesItemColors)
        {
            if (!usesItemColors)
                return creaturePartId;

            return armorPartId > 0 ? armorPartId : creaturePartId;
        }

        private static string GetCreatureModelPrefix(uint creature)
        {
            var gender = GetGenderModelCode(GetGender(creature));

            var race = Get2DAString("appearance", "RACE", (int)GetAppearanceType(creature));
            if (string.IsNullOrWhiteSpace(race) || race == "****" || race.Length != 1)
                return string.Empty;

            return $"p{gender}{race.ToLowerInvariant()}{(int)GetPhenoType(creature)}_";
        }

        private static string GetGenderModelCode(Gender gender)
        {
            return gender == Gender.Female ? "f" : "m";
        }

        private static void AddCloakSelections(
            uint creature,
            string creaturePrefix,
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections)
        {
            var cloak = GetItemInSlot(InventorySlot.Cloak, creature);
            if (!GetIsObjectValid(cloak))
                return;

            var appearanceId = GetItemAppearance(cloak, ItemAppearanceType.SimpleModel, 0);
            if (appearanceId <= 0)
                return;

            var textureId = appearanceId;
            if (int.TryParse(Get2DAString("cloakmodel", "TEXTURE", appearanceId), out var mappedTexture) &&
                mappedTexture > 0)
            {
                textureId = mappedTexture;
            }

            var foundRaceSpecific = AddModelSelections(
                selections,
                seenSelections,
                $"{creaturePrefix}cloak_{textureId:D3}".ToLowerInvariant(),
                cloak,
                creature,
                true,
                AppearanceArmor.Invalid);
            if (!foundRaceSpecific)
            {
                AddModelSelections(
                    selections,
                    seenSelections,
                    $"cloak_{textureId:D3}".ToLowerInvariant(),
                    cloak,
                    creature,
                    true,
                    AppearanceArmor.Invalid);
            }
        }

        public static void CopyArmorPartTintOverrides(
            uint creature,
            uint item,
            AppearanceArmor sourcePart,
            AppearanceArmor destinationPart)
        {
            if (!GetIsObjectValid(item) ||
                !BodyPartNames.TryGetValue((CreaturePart)(int)sourcePart, out var sourcePartName) ||
                !BodyPartNames.TryGetValue((CreaturePart)(int)destinationPart, out var destinationPartName))
            {
                return;
            }

            var prefix = GetCreatureModelPrefix(creature);
            var sourceCreaturePart = (CreaturePart)(int)sourcePart;
            var destinationCreaturePart = (CreaturePart)(int)destinationPart;
            var sourcePartId = ResolvePartId(
                GetCreatureBodyPart(sourceCreaturePart, creature),
                GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)sourcePart),
                true);
            var destinationPartId = ResolvePartId(
                GetCreatureBodyPart(destinationCreaturePart, creature),
                GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)destinationPart),
                true);
            if (string.IsNullOrWhiteSpace(prefix) || sourcePartId <= 0)
                return;

            var sourceModelResref =
                $"{prefix}{sourcePartName}{sourcePartId:D3}".ToLowerInvariant();
            var destinationModelResref =
                $"{prefix}{destinationPartName}{sourcePartId:D3}".ToLowerInvariant();
            var previousDestinationModelResref =
                $"{prefix}{destinationPartName}{destinationPartId:D3}".ToLowerInvariant();
            var sourceMaterials = TintMapMaterialRegistry.GetMaterials(sourceModelResref);
            var destinationMaterials = TintMapMaterialRegistry.GetMaterials(destinationModelResref);
            var previousDestinationMaterials =
                TintMapMaterialRegistry.GetMaterials(previousDestinationModelResref);
            var activeVariables = BodyPartNames
                .SelectMany(pair =>
                {
                    var part = (AppearanceArmor)(int)pair.Key;
                    var activePartId = part == destinationPart
                        ? sourcePartId
                        : ResolvePartId(
                            GetCreatureBodyPart(pair.Key, creature),
                            GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)part),
                            true);
                    if (activePartId <= 0)
                        return Array.Empty<string>();

                    var activeModelResref =
                        $"{prefix}{pair.Value}{activePartId:D3}".ToLowerInvariant();
                    return TintMapMaterialRegistry.GetMaterials(activeModelResref)
                        .SelectMany(material => material.Layers.SelectMany(layer =>
                            GetEquivalentMaterialVariables(activeModelResref, material, layer)));
                })
                .ToHashSet(StringComparer.Ordinal);
            foreach (var layer in destinationMaterials
                         .SelectMany(destination => destination.Layers)
                         .Distinct())
            {
                var sourceLayerMaterials = sourceMaterials
                    .Where(source => source.Layers.Contains(layer))
                    .ToList();
                var destinationLayerMaterials = destinationMaterials
                    .Where(destination => destination.Layers.Contains(layer))
                    .ToList();
                for (var index = 0; index < destinationLayerMaterials.Count; index++)
                {
                    var destination = destinationLayerMaterials[index];
                    var destinationVariables = GetEquivalentMaterialVariables(
                        destinationModelResref,
                        destination,
                        layer);
                    var source = FindMirroredSourceMaterial(
                        sourceLayerMaterials,
                        destinationLayerMaterials,
                        destination,
                        index,
                        sourcePartName,
                        destinationPartName);
                    var sourceValue = source == null
                        ? 0
                        : GetLocalInt(
                            item,
                            TintMapVariable.GetName(source.Resref, layer));
                    foreach (var destinationVariable in destinationVariables)
                    {
                        if (sourceValue > 0)
                            SetLocalInt(item, destinationVariable, sourceValue);
                        else
                            DeleteLocalInt(item, destinationVariable);
                    }
                }
            }

            foreach (var previousDestination in previousDestinationMaterials)
            {
                foreach (var layer in previousDestination.Layers)
                {
                    foreach (var previousVariable in GetEquivalentMaterialVariables(
                                 previousDestinationModelResref,
                                 previousDestination,
                                 layer))
                    {
                        if (!activeVariables.Contains(previousVariable))
                            DeleteLocalInt(item, previousVariable);
                    }
                }
            }
        }

        private static TintMapMaterialDefinition FindMirroredSourceMaterial(
            IReadOnlyList<TintMapMaterialDefinition> sourceMaterials,
            IReadOnlyList<TintMapMaterialDefinition> destinationMaterials,
            TintMapMaterialDefinition destination,
            int destinationIndex,
            string sourcePartName,
            string destinationPartName)
        {
            var destinationIdentity = TintMapEquipmentMaterialMatcher.GetMirroredPartIdentity(
                destination.Resref,
                destinationPartName);
            var identityMatches = sourceMaterials
                .Where(source => string.Equals(
                    TintMapEquipmentMaterialMatcher.GetMirroredPartIdentity(source.Resref, sourcePartName),
                    destinationIdentity,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (identityMatches.Count == 1)
                return identityMatches[0];

            // One source material intentionally broadcasts to each destination material. For
            // genuinely parallel lists, position remains the only stable identity for generated
            // hashed materials. Never use position for asymmetric lists: an extra material on one
            // side would copy an unrelated tint, which is worse than clearing an unmatched slot.
            if (sourceMaterials.Count == 1)
                return sourceMaterials[0];
            return sourceMaterials.Count == destinationMaterials.Count &&
                   destinationIndex < sourceMaterials.Count
                ? sourceMaterials[destinationIndex]
                : null;
        }

        private static IReadOnlyList<string> GetEquivalentMaterialVariables(
            string modelResref,
            TintMapMaterialDefinition material,
            TintMapLayerType layer)
        {
            var equivalentResrefs = TintMapMaterialRegistry.GetEquivalentEquipmentMaterialResrefs(
                modelResref,
                material.Resref,
                layer);
            if (equivalentResrefs.Count == 0)
                equivalentResrefs = new[] { material.Resref };

            return equivalentResrefs
                .Select(resref => TintMapVariable.GetName(resref, layer))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static void AddSimpleItemSelections(
            uint creature,
            InventorySlot slot,
            string modelPrefix,
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections,
            string fallbackModelPrefix = null)
        {
            var item = GetItemInSlot(slot, creature);
            if (!GetIsObjectValid(item))
                return;

            var modelId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0);
            if (modelId <= 0)
                return;

            var foundModel = AddModelSelections(
                selections,
                seenSelections,
                $"{modelPrefix}_{modelId:D3}".ToLowerInvariant(),
                item,
                creature,
                true,
                AppearanceArmor.Invalid);
            if (!foundModel && !string.IsNullOrWhiteSpace(fallbackModelPrefix))
            {
                AddModelSelections(
                    selections,
                    seenSelections,
                    $"{fallbackModelPrefix}_{modelId:D3}".ToLowerInvariant(),
                    item,
                    creature,
                    true,
                    AppearanceArmor.Invalid);
            }
        }

        private static void AddTableModelSelections(
            string table,
            string column,
            int row,
            uint paletteSource,
            uint creaturePaletteSource,
            bool usesItemColors,
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections)
        {
            if (row < 0)
                return;

            var model = Get2DAString(table, column, row).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(model) || model == "****")
                return;

            AddModelSelections(
                selections,
                seenSelections,
                model,
                paletteSource,
                creaturePaletteSource,
                usesItemColors,
                AppearanceArmor.Invalid);
        }

        private static bool AddModelSelections(
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections,
            string model,
            uint paletteSource,
            uint creaturePaletteSource,
            bool usesItemColors,
            AppearanceArmor armorPart,
            string overrideModelResref = null)
        {
            var foundModel = false;
            foreach (var material in TintMapMaterialRegistry.GetMaterials(model))
            {
                foundModel = true;
                var identity = $"{material.Resref}|{paletteSource}|{(int)armorPart}";
                if (!seenSelections.Add(identity))
                    continue;

                selections.Add(new TintMapMaterialSelection(
                    model,
                    material,
                    paletteSource,
                    creaturePaletteSource,
                    usesItemColors,
                    armorPart,
                    overrideModelResref));
            }

            return foundModel;
        }
    }
}
