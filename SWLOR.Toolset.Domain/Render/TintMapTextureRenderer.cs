using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
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

        public static bool IsTintMapMaterial(MtrMaterial? material)
        {
            return material != null &&
                   material.CustomShaders.Values.Any(shader =>
                       shader.Equals(TintShader, StringComparison.OrdinalIgnoreCase) ||
                       shader.Equals(NormalMappedTintShader, StringComparison.OrdinalIgnoreCase));
        }

        public static TextureImage? Render(
            ResourceIndex resourceIndex,
            string materialName,
            MtrMaterial material,
            IReadOnlyDictionary<int, int>? layerColorIndices,
            IReadOnlyDictionary<string, int>? overrides,
            AppearanceArmor armorPart = AppearanceArmor.Invalid)
        {
            if (!IsTintMapMaterial(material))
                return null;

            var tintMapName = material.GetTexture(7);
            var paletteName = material.GetTexture(10);
            if (string.IsNullOrWhiteSpace(tintMapName) || string.IsNullOrWhiteSpace(paletteName))
                return null;

            var tintMap = TextureLoader.Load(resourceIndex, tintMapName);
            var palette = TextureLoader.Load(resourceIndex, paletteName);
            if (tintMap == null || palette == null || palette.Width <= 0 || palette.Height <= 0)
                return null;

            var alphaSource = material.GetAlphaSource();
            var alphaTexture = alphaSource is { } source
                ? TextureLoader.Load(resourceIndex, source.TextureName)
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

                var paletteIndex = TintMapColor.TryFromStoredValue(savedValue, out var customColor)
                    ? TintMapPaletteColors.GetClosestColorId(layer, customColor)
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
