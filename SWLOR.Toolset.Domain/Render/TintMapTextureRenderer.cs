using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using Nwn.Preview.Dds;
using Nwn.Formats.Mtr;
using Nwn.Formats.TwoDa;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Applies the tint-map material shader's color step on the CPU for thumbnails. Lighting,
    /// normals, and specularity remain the thumbnail renderer's responsibility.
    /// </summary>
    public static class TintMapTextureRenderer
    {
        private const string TintShader = "fs_plt_tinter";
        private const string NormalMappedTintShader = "fs_plt_tinter_nm";
        private const string HairTintShader = "fs_plt_hair_nm";
        private const string CloakTintShader = "fs_cloaktint";
        private const int MaxCachedCloakTiles = 32;
        private static readonly ConditionalWeakTable<ResourceIndex, CloakTileCache> CloakTiles = new();

        public static bool IsTintMapMaterial(MtrDocument? material)
        {
            return material != null &&
                   material.RawShaderBindings.Values.Any(shader =>
                       shader?.Equals(TintShader, StringComparison.OrdinalIgnoreCase) == true ||
                       shader?.Equals(NormalMappedTintShader, StringComparison.OrdinalIgnoreCase) == true ||
                       shader?.Equals(CloakTintShader, StringComparison.OrdinalIgnoreCase) == true ||
                       shader?.Equals(HairTintShader, StringComparison.OrdinalIgnoreCase) == true);
        }

        public static TextureImage? LoadTintMap(ResourceIndex resourceIndex, MtrDocument material, string? textureName = null)
        {
            if (!material.RawShaderBindings.Values.Any(shader =>
                    shader?.Equals(CloakTintShader, StringComparison.OrdinalIgnoreCase) == true))
            {
                var mapName = MaterialResolver.GetTexture(material, 7);
                return string.IsNullOrWhiteSpace(mapName)
                    ? null
                    : TextureLoader.Load(resourceIndex, mapName, standardDdsRowOrder: DdsStoredRowOrder.BottomUp);
            }

            // Composed cloak bitmaps carry cloakmodel.TEXTURE while geometry/materials are shared.
            if (textureName == null || !int.TryParse(textureName.Split('_').Last(), out var texture) ||
                !resourceIndex.TryLookup(new ResourceIdentity("cloaktint", ResourceIdentity.TypeFromExtension("2da")), out var handle))
                return null;
            var table = TwoDaReader.Read(handle.GetBytes(), TwoDaReadOptions.EngineCompatible);
            if (texture < 0 || texture >= table.Rows.Count || !int.TryParse(table.GetValue(texture, "INDEX"), out var index) || index < 0 || index >= 128)
                return null;
            var atlasName = MaterialResolver.GetTexture(material, 6 + index / 32);
            if (string.IsNullOrWhiteSpace(atlasName))
                return null;
            var tiles = CloakTiles.GetValue(resourceIndex, resources =>
            {
                var cache = new CloakTileCache(MaxCachedCloakTiles);
                resources.ResourcesReloaded += cache.Clear;
                return cache;
            });
            return tiles.GetOrLoad(atlasName + "|" + index, () => LoadCloakTile(resourceIndex, atlasName, index));
        }

        private static TextureImage? LoadCloakTile(ResourceIndex resourceIndex, string atlasName, int index)
        {
            var atlas = TextureLoader.Load(resourceIndex, atlasName, standardDdsRowOrder: DdsStoredRowOrder.BottomUp);
            if (atlas == null || atlas.Width != 2048 || atlas.Height != 4096)
                return null;
            var pixels = new byte[512 * 512 * 4];
            var x = index % 4 * 512;
            // TextureLoader presents DDS rows top-first; shader atlas indices start at the bottom.
            var y = (7 - index % 32 / 4) * 512;
            for (var row = 0; row < 512; row++)
                Array.Copy(atlas.Pixels, ((y + row) * atlas.Width + x) * 4, pixels, row * 512 * 4, 512 * 4);
            return new TextureImage { Width = 512, Height = 512, Pixels = pixels, SourceFormat = atlas.SourceFormat };
        }

        public static TextureImage? Render(
            ResourceIndex resourceIndex,
            string materialName,
            MtrDocument material,
            IReadOnlyDictionary<int, int>? layerColorIndices,
            IReadOnlyDictionary<string, int>? overrides,
            AppearanceArmor armorPart = AppearanceArmor.Invalid,
            string? textureName = null)
        {
            if (!IsTintMapMaterial(material))
                return null;

            var tintMapName = MaterialResolver.GetTexture(material, 7);
            var paletteName = MaterialResolver.GetTexture(material, 10);
            if (string.IsNullOrWhiteSpace(tintMapName) || string.IsNullOrWhiteSpace(paletteName))
                return null;

            var tintMap = LoadTintMap(resourceIndex, material, textureName);
            var palette = TextureLoader.Load(resourceIndex, paletteName);
            if (tintMap == null || palette == null || palette.Width <= 0 || palette.Height <= 0)
                return null;

            var alphaSource = MaterialResolver.GetAlphaSource(material);
            var alphaTexture = alphaSource is { } source
                ? TextureLoader.Load(
                    resourceIndex,
                    source.TextureName,
                    standardDdsRowOrder: DdsStoredRowOrder.BottomUp)
                : null;

            var output = new byte[checked(tintMap.Width * tintMap.Height * 4)];
            for (var pixel = 0; pixel < tintMap.Width * tintMap.Height; pixel++)
            {
                var offset = pixel * 4;
                var shade = tintMap.Pixels[offset];
                var layer = (TintMapLayerType)Math.Clamp(
                    tintMap.Pixels[offset + 1] * 10 / byte.MaxValue,
                    0,
                    9);
                var savedValue = TintMapOverrides.GetMaterialColor(
                    overrides,
                    materialName,
                    layer,
                    armorPart);

                var isCustom = TintMapColor.TryFromStoredValue(savedValue, out var customColor);
                var paletteIndex = isCustom
                    ? 0 // Custom RGB uses the shader's neutral reference row, preserving only shading.
                    : savedValue > 0 &&
                      savedValue <= TintMapMaterialRegistry.PaletteColorCount
                        ? savedValue - 1
                        : layerColorIndices != null &&
                          layerColorIndices.TryGetValue((int)layer, out var standardIndex)
                            ? standardIndex
                            : 0;
                paletteIndex = Math.Clamp(
                    paletteIndex,
                    0,
                    TintMapMaterialRegistry.PaletteColorCount - 1);

                var definition = TintMapMaterialRegistry.GetLayer(layer);
                var paletteX = shade * (palette.Width - 1) / 255;
                // Tint palette coordinates are authored from the bottom of the texture because
                // NWN shaders use OpenGL texture coordinates. TextureLoader exposes decoded
                // images top-first, so convert the shader row before indexing the CPU image.
                var shaderPaletteRow = Math.Clamp(
                    definition.PaletteBaseRow + paletteIndex,
                    0,
                    palette.Height - 1);
                var paletteY = palette.Height - 1 - shaderPaletteRow;
                var paletteOffset = (paletteY * palette.Width + paletteX) * 4;
                output[offset] = palette.Pixels[paletteOffset];
                output[offset + 1] = palette.Pixels[paletteOffset + 1];
                output[offset + 2] = palette.Pixels[paletteOffset + 2];
                if (isCustom)
                {
                    var midpointX = 128 * (palette.Width - 1) / 255;
                    var midpointOffset = (paletteY * palette.Width + midpointX) * 4;
                    var scale = Luminance(palette.Pixels, paletteOffset) /
                                Math.Max(Luminance(palette.Pixels, midpointOffset), 1f);
                    output[offset] = Shade(customColor.Red, scale);
                    output[offset + 1] = Shade(customColor.Green, scale);
                    output[offset + 2] = Shade(customColor.Blue, scale);
                }
                output[offset + 3] = SampleAlpha(
                    alphaTexture,
                    alphaSource,
                    pixel % tintMap.Width,
                    pixel / tintMap.Width,
                    tintMap.Width,
                    tintMap.Height);
            }

            return new TextureImage
            {
                Width = tintMap.Width,
                Height = tintMap.Height,
                Pixels = output,
                SourceFormat = tintMap.SourceFormat,
                AlphaCutoff = alphaSource?.ByteCutoff ?? TextureImage.DefaultAlphaCutoff
            };
        }

        private static float Luminance(byte[] pixels, int offset) =>
            pixels[offset] * 0.2126f + pixels[offset + 1] * 0.7152f + pixels[offset + 2] * 0.0722f;

        private static byte Shade(byte component, float scale) =>
            (byte)Math.Clamp(MathF.Round(component * scale), 0, 255);

        private static byte SampleAlpha(
            TextureImage? texture,
            MtrAlphaSource? source,
            int x,
            int y,
            int targetWidth,
            int targetHeight)
        {
            if (texture == null || source == null || texture.Width <= 0 || texture.Height <= 0)
                return 255;

            var sourceX = Math.Clamp(x * texture.Width / Math.Max(targetWidth, 1), 0, texture.Width - 1);
            var sourceY = Math.Clamp(y * texture.Height / Math.Max(targetHeight, 1), 0, texture.Height - 1);
            var offset = (sourceY * texture.Width + sourceX) * 4;
            return texture.Pixels[offset + (source.Value.UsesRedChannel ? 0 : 3)];
        }
    }
}
