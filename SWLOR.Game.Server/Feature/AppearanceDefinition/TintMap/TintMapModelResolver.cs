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
                    selections,
                    seenSelections);

                // Non-parts appearances still render simple equipped models. Their base body is a
                // single appearance.RACE model rather than assembled phenotype parts, but a visible
                // helmet or cloak remains item-owned and its custom tints must be edited on that item.
                AddSimpleItemSelections(creature, InventorySlot.Head, "helm", selections, seenSelections);
                AddCloakSelections(creature, string.Empty, selections, seenSelections);
            }

            AddTableModelSelections(
                "wingmodel",
                "MODEL",
                (int)GetCreatureWingType(creature),
                creature,
                selections,
                seenSelections);
            AddTableModelSelections(
                "tailmodel",
                "MODEL",
                (int)GetCreatureTailType(creature),
                creature,
                selections,
                seenSelections);

            return selections;
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
                        true,
                        AppearanceArmor.Robe);
                }
            }

            AddSimpleItemSelections(creature, InventorySlot.Head, "helm", selections, seenSelections);
            AddCloakSelections(creature, prefix, selections, seenSelections);
        }

        private static int ResolvePartId(int creaturePartId, int armorPartId, bool usesItemColors)
        {
            if (!usesItemColors || creaturePartId <= 0)
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
                true,
                AppearanceArmor.Invalid);
            if (!foundRaceSpecific)
            {
                AddModelSelections(
                    selections,
                    seenSelections,
                    $"cloak_{textureId:D3}".ToLowerInvariant(),
                    cloak,
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
            var partId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)sourcePart);
            if (string.IsNullOrWhiteSpace(prefix) || partId <= 0)
                return;

            var sourceMaterials = TintMapMaterialRegistry.GetMaterials(
                $"{prefix}{sourcePartName}{partId:D3}".ToLowerInvariant());
            var destinationMaterials = TintMapMaterialRegistry.GetMaterials(
                $"{prefix}{destinationPartName}{partId:D3}".ToLowerInvariant());
            for (var index = 0; index < destinationMaterials.Count; index++)
            {
                var destination = destinationMaterials[index];
                var source = index < sourceMaterials.Count ? sourceMaterials[index] : null;
                foreach (var layer in destination.Layers)
                {
                    var destinationVariable = TintMapVariable.GetName(destination.Resref, layer);
                    var sourceValue = source != null && source.Layers.Contains(layer)
                        ? GetLocalInt(item, TintMapVariable.GetName(source.Resref, layer))
                        : 0;
                    if (sourceValue > 0)
                        SetLocalInt(item, destinationVariable, sourceValue);
                    else
                        DeleteLocalInt(item, destinationVariable);
                }
            }
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
                true,
                AppearanceArmor.Invalid);
            if (!foundModel && !string.IsNullOrWhiteSpace(fallbackModelPrefix))
            {
                AddModelSelections(
                    selections,
                    seenSelections,
                    $"{fallbackModelPrefix}_{modelId:D3}".ToLowerInvariant(),
                    item,
                    true,
                    AppearanceArmor.Invalid);
            }
        }

        private static void AddTableModelSelections(
            string table,
            string column,
            int row,
            uint creature,
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
                creature,
                false,
                AppearanceArmor.Invalid);
        }

        private static bool AddModelSelections(
            ICollection<TintMapMaterialSelection> selections,
            ISet<string> seenSelections,
            string model,
            uint paletteSource,
            bool usesItemColors,
            AppearanceArmor armorPart)
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
                    usesItemColors,
                    armorPart));
            }

            return foundModel;
        }
    }
}
