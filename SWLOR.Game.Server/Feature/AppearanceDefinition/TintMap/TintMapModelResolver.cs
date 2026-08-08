using System;
using System.Collections.Generic;
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
            AddSimpleItemSelections(
                creature,
                InventorySlot.Cloak,
                $"{prefix}cloak",
                selections,
                seenSelections,
                "cloak");
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
