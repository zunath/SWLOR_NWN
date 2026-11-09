using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class TintMapMaterialRegistry
    {
        public const int PaletteColorCount = 176;
        public const int PaletteTextureHeight = 2048;
        public const int CustomColorReferenceIntensity = 128;
        private const string TintMap2DA = "tintmap";

        // Built once and never written again, so it needs no service and the toolset reads it without a container.
        // The material tables read from the 2DAs are runtime state and live in TintMapState.
        private static readonly FrozenDictionary<TintMapLayerType, TintMapLayerDefinition> LayerDefinitions =
            new Dictionary<TintMapLayerType, TintMapLayerDefinition>
        {
            { TintMapLayerType.Skin, new TintMapLayerDefinition("Skin", "rowSkin", "tintSkin", "useCustomSkin", "gui_pal_skin", 0) },
            { TintMapLayerType.Hair, new TintMapLayerDefinition("Hair", "rowHair", "tintHair", "useCustomHair", "gui_pal_hair01", 176) },
            { TintMapLayerType.Metal1, new TintMapLayerDefinition("Metal 1", "rowMetal1", "tintMetal1", "useCustomMetal1", "gui_pal_armor01", 352) },
            { TintMapLayerType.Metal2, new TintMapLayerDefinition("Metal 2", "rowMetal2", "tintMetal2", "useCustomMetal2", "gui_pal_armor01", 528) },
            { TintMapLayerType.Cloth1, new TintMapLayerDefinition("Cloth 1", "rowCloth1", "tintCloth1", "useCustomCloth1", "gui_pal_tattoo", 704) },
            { TintMapLayerType.Cloth2, new TintMapLayerDefinition("Cloth 2", "rowCloth2", "tintCloth2", "useCustomCloth2", "gui_pal_tattoo", 704) },
            { TintMapLayerType.Leather1, new TintMapLayerDefinition("Leather 1", "rowLeath1", "tintLeath1", "useCustomLeath1", "gui_pal_tattoo", 880) },
            { TintMapLayerType.Leather2, new TintMapLayerDefinition("Leather 2", "rowLeath2", "tintLeath2", "useCustomLeath2", "gui_pal_tattoo", 880) },
            { TintMapLayerType.Tattoo1, new TintMapLayerDefinition("Tattoo 1", "rowTat1", "tintTat1", "useCustomTat1", "gui_pal_tattoo", 1056) },
            { TintMapLayerType.Tattoo2, new TintMapLayerDefinition("Tattoo 2", "rowTat2", "tintTat2", "useCustomTat2", "gui_pal_tattoo", 1056) }
        }.ToFrozenDictionary();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var materials = new Dictionary<string, List<TintMapMaterialDefinition>>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in new[] { TintMap2DA, "cloaktint" })
            {
                for (var row = 0; row < Get2DARowCount(table); row++)
                {
                    var model = Get2DAString(table, "MODEL", row);
                    var material = Get2DAString(table, "MATERIAL", row);
                    var layerValues = Get2DAString(table, "LAYERS", row).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(material) || layerValues.Length == 0)
                        continue;

                    var layers = new List<TintMapLayerType>();
                    foreach (var layerValue in layerValues)
                    {
                        if (int.TryParse(layerValue, out var value) && Enum.IsDefined(typeof(TintMapLayerType), value))
                            layers.Add((TintMapLayerType)value);
                    }

                    if (layers.Count == 0)
                        continue;

                    if (!materials.TryGetValue(model, out var modelMaterials))
                    {
                        modelMaterials = new List<TintMapMaterialDefinition>();
                        materials[model] = modelMaterials;
                    }

                    modelMaterials.Add(new TintMapMaterialDefinition(material, material, layers.ToArray()));
                }
            }

            TintMapState.LoadMaterials(materials);

            Log.WriteStructured(
                LogGroup.Server,
                "Loaded {TintMapModelCount} tint-map models.",
                materials.Count);
        }

        public static IReadOnlyList<TintMapMaterialDefinition> GetMaterials(string modelResref) =>
            TintMapState.GetMaterials(modelResref);

        public static bool AreEquipmentMaterialSlotsEquivalent(
            string sourceMaterialResref,
            string destinationModelResref,
            string destinationMaterialResref,
            TintMapLayerType layer)
        {
            return TintMapState.AreEquipmentMaterialSlotsEquivalent(
                sourceMaterialResref,
                destinationModelResref,
                destinationMaterialResref,
                layer);
        }

        public static IReadOnlyList<string> GetEquivalentEquipmentMaterialResrefs(
            string sourceModelResref,
            string sourceMaterialResref,
            TintMapLayerType layer)
        {
            return TintMapState.GetEquivalentEquipmentMaterialResrefs(
                sourceModelResref,
                sourceMaterialResref,
                layer);
        }

        public static bool IsEquipmentMaterialExclusiveToArmorPart(
            string materialResref,
            TintMapLayerType layer,
            SWLOR.NWN.API.NWScript.Enum.Item.AppearanceArmor armorPart)
        {
            return TintMapState.IsEquipmentMaterialExclusiveToArmorPart(
                materialResref,
                layer,
                armorPart);
        }

        public static TintMapLayerDefinition GetLayer(TintMapLayerType layer)
        {
            if (!LayerDefinitions.TryGetValue(layer, out var definition))
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown tint map layer.");

            return definition;
        }

        public static float GetPaletteCoordinate(TintMapLayerType layer, int colorId)
        {
            if (colorId < 0 || colorId >= PaletteColorCount)
                throw new ArgumentOutOfRangeException(nameof(colorId));

            var definition = GetLayer(layer);
            return (definition.PaletteBaseRow + colorId + 0.5f) / PaletteTextureHeight;
        }

    }
}
