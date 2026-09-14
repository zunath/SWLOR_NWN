using System.Text;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Renderer hints resolved from a decoded texture and its optional TXI sidecar.
    /// </summary>
    public readonly record struct TextureRenderHints(
        float AlphaCutoff,
        string? EnvironmentMapTexture,
        TxiBlendMode Blending);

    /// <summary>
    /// Resolves Aurora's two meanings for texture alpha: punch-through transparency or the blend
    /// mask between a diffuse texture and an environment map.
    /// </summary>
    /// <remarks>
    /// Part-based creature appearances declare the <c>default</c> environment map. In an area that
    /// means the tileset's map; the standalone Aurora model/toolset preview uses its bundled
    /// <c>chrome1</c> sphere map. The toolset's item preview is likewise standalone, so PLT armor
    /// textures use <c>chrome1</c> unless a TXI names a different map.
    /// </remarks>
    public static class TextureRenderPolicy
    {
        public const string StandaloneEnvironmentMap = "chrome1";

        /// <summary>
        /// Returns the cutoff and environment map for one decoded diffuse texture.
        /// </summary>
        public static TextureRenderHints Resolve(
            ResourceIndex resourceIndex,
            string textureName,
            TextureImage image)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            ArgumentNullException.ThrowIfNull(image);

            var txi = ReadTxi(resourceIndex, textureName);
            var environmentMap = ResolveEnvironmentMap(image.SourceFormat, txi);

            if (environmentMap != null)
                return new TextureRenderHints(0f, environmentMap, txi?.Blending ?? TxiBlendMode.None);

            if (txi?.Blending == TxiBlendMode.PunchThrough)
                return new TextureRenderHints(
                    TextureAlphaPolicy.PunchThroughCutoff,
                    null,
                    TxiBlendMode.PunchThrough);

            if (txi?.Blending == TxiBlendMode.Additive)
                return new TextureRenderHints(0f, null, TxiBlendMode.Additive);

            return new TextureRenderHints(
                TextureAlphaPolicy.RequiresCutoff(image)
                    ? TextureAlphaPolicy.PunchThroughCutoff
                    : 0f,
                null,
                TxiBlendMode.None);
        }

        /// <summary>
        /// Resolves an explicit TXI map first, then the standalone PLT default. An explicit
        /// <c>default</c> TXI value has the same standalone meaning.
        /// </summary>
        public static string? ResolveEnvironmentMap(
            TextureSourceFormat sourceFormat,
            TxiInfo? txi)
        {
            if (!string.IsNullOrWhiteSpace(txi?.EnvMapTexture))
            {
                return txi.EnvMapTexture.Equals("default", StringComparison.OrdinalIgnoreCase)
                    ? StandaloneEnvironmentMap
                    : txi.EnvMapTexture;
            }

            return sourceFormat == TextureSourceFormat.Plt
                ? StandaloneEnvironmentMap
                : null;
        }

        private static TxiInfo? ReadTxi(ResourceIndex resourceIndex, string textureName)
        {
            try
            {
                var identity = new ResourceIdentity(
                    textureName,
                    ResourceIdentity.TypeFromExtension("txi"));
                if (!resourceIndex.TryLookup(identity, out var handle))
                    return null;

                var bytes = handle.GetBytes();
                return bytes.Length == 0
                    ? null
                    : TxiInfo.Parse(Encoding.ASCII.GetString(bytes));
            }
            catch (Exception)
            {
                // A broken optional sidecar must not make the diffuse texture disappear.
                return null;
            }
        }
    }
}
