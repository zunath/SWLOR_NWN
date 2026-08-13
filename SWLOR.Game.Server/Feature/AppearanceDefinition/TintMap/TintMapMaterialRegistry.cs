using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class TintMapMaterialRegistry
    {
        public const int PaletteColorCount = 176;
        public const int PaletteTextureHeight = 2048;
        public const int CustomColorReferenceIntensity = 128;
        private const int CustomColorRedMultiplier = 65536;
        private const int CustomColorGreenMultiplier = 256;
        private const string TintMap2DA = "tintmap";

        private static readonly Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>> MaterialsByModel =
            new(StringComparer.OrdinalIgnoreCase);
        private static TintMapEquipmentMaterialIndex _equipmentMaterialIndex =
            TintMapEquipmentMaterialIndex.Empty;

        private static readonly Dictionary<TintMapLayerType, TintMapLayerDefinition> LayerDefinitions = new()
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
        };

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var materials = new Dictionary<string, List<TintMapMaterialDefinition>>(StringComparer.OrdinalIgnoreCase);

            for (var row = 0; row < Get2DARowCount(TintMap2DA); row++)
            {
                var model = Get2DAString(TintMap2DA, "MODEL", row);
                var material = Get2DAString(TintMap2DA, "MATERIAL", row);
                var layerValues = Get2DAString(TintMap2DA, "LAYERS", row).Split(',', StringSplitOptions.RemoveEmptyEntries);
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

            MaterialsByModel.Clear();
            foreach (var (model, modelMaterials) in materials)
            {
                MaterialsByModel[model] = modelMaterials;
            }
            _equipmentMaterialIndex = new TintMapEquipmentMaterialIndex(MaterialsByModel);

            Log.WriteStructured(
                LogGroup.Server,
                "Loaded {TintMapModelCount} tint-map models.",
                MaterialsByModel.Count);
        }

        public static IReadOnlyList<TintMapMaterialDefinition> GetMaterials(string modelResref)
        {
            if (string.IsNullOrWhiteSpace(modelResref))
                return Array.Empty<TintMapMaterialDefinition>();

            return MaterialsByModel.TryGetValue(modelResref, out var materials)
                ? materials
                : Array.Empty<TintMapMaterialDefinition>();
        }

        /// <summary>
        /// Packs an RGB color into the scalar row parameter that NWN replicates for live
        /// material overrides. Every 24-bit RGB value is exactly representable by a 32-bit
        /// float; the negative sign distinguishes custom colors from positive palette rows.
        /// </summary>
        public static float GetCustomColorUniformValue(TintMapColor color)
        {
            var packedColor = color.Red * CustomColorRedMultiplier +
                              color.Green * CustomColorGreenMultiplier +
                              color.Blue;
            return -(packedColor + 1f);
        }

        public static bool AreEquipmentMaterialSlotsEquivalent(
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

        public static IReadOnlyList<string> GetEquivalentEquipmentMaterialResrefs(
            string sourceModelResref,
            string sourceMaterialResref,
            TintMapLayerType layer)
        {
            return _equipmentMaterialIndex.GetEquivalentMaterialResrefs(
                sourceModelResref,
                sourceMaterialResref,
                layer);
        }

        public static bool IsEquipmentMaterialExclusiveToArmorPart(
            string materialResref,
            TintMapLayerType layer,
            SWLOR.NWN.API.NWScript.Enum.Item.AppearanceArmor armorPart)
        {
            return _equipmentMaterialIndex.IsExclusiveToArmorPart(
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
