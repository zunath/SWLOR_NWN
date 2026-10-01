using System.Globalization;
using System.Text;
using Nwn.Formats.Mtr;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    public readonly record struct MtrAlphaSource(string TextureName, bool UsesRedChannel)
    {
        /// <summary>
        /// The runtime tint shader discards texture1 alpha below 0.2 and texture9 red below 0.3.
        /// Preview renderers must use the same source-specific cutoff or translucent edge pixels
        /// disappear differently in the toolset than they do in game.
        /// </summary>
        public float Cutoff => UsesRedChannel ? 0.3f : 0.2f;

        public byte ByteCutoff => (byte)Math.Ceiling(Cutoff * byte.MaxValue);
    }

    /// <summary>Resolves NWN:EE material descriptors and applies SWLOR tint-alpha policy.</summary>
    public static class MaterialResolver
    {
        /// <summary>Reads a material through the shared, bounded NWN material reader.</summary>
        public static MtrDocument Parse(string text) => MtrReader.Read(Encoding.UTF8.GetBytes(text));

        /// <summary>Reads source bytes directly through the shared, bounded NWN material reader.</summary>
        public static MtrDocument Parse(ReadOnlySpan<byte> bytes) => MtrReader.Read(bytes);

        /// <summary>Returns a material's declared texture binding, or null when absent or explicitly null.</summary>
        public static string? GetTexture(MtrDocument? material, int slot) =>
            material != null && material.Textures.TryGetValue(slot, out var texture) ? texture : null;

        /// <summary>
        /// Returns the SWLOR tint shader's selected alpha texture and channel, if a positive
        /// <c>useTextureNAlpha</c> parameter and that texture slot are both present.
        /// </summary>
        public static MtrAlphaSource? GetAlphaSource(MtrDocument? material)
        {
            if (material == null)
                return null;

            const string prefix = "useTexture";
            const string suffix = "Alpha";
            foreach (var (name, parameter) in material.Parameters)
            {
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var enabledText = parameter.RawValues.FirstOrDefault();
                if (!float.TryParse(
                        enabledText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var enabled) ||
                    enabled <= 0f)
                {
                    continue;
                }

                var slotText = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
                if (!int.TryParse(slotText, NumberStyles.None, CultureInfo.InvariantCulture, out var slot))
                    continue;

                if (material.Textures.TryGetValue(slot, out var texture) &&
                    !string.IsNullOrWhiteSpace(texture))
                {
                    return new MtrAlphaSource(texture, UsesRedChannel: slot == 9);
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves a material's effective diffuse texture. When <paramref name="resolveMaterial"/>
        /// is false, the input is a bitmap name and an unrelated same-name MTR is not consulted.
        /// </summary>
        public static string ResolveDiffuseTextureName(
            ResourceIndex index,
            string textureOrMaterialName,
            bool resolveMaterial = true)
        {
            if (string.IsNullOrWhiteSpace(textureOrMaterialName))
                return textureOrMaterialName;

            var material = resolveMaterial ? TryParseMaterial(index, textureOrMaterialName) : null;
            return material == null
                ? textureOrMaterialName
                : EffectiveSlot(material, 0) ?? textureOrMaterialName;
        }

        /// <summary>
        /// Resolves the material's explicit map slots, or NWN:EE's automatic companion maps when
        /// no material applies. PLT resources are never treated as companion maps.
        /// </summary>
        public static MaterialMaps ResolveMaterialMaps(
            ResourceIndex index,
            string textureOrMaterialName,
            bool resolveMaterial = true)
        {
            if (string.IsNullOrWhiteSpace(textureOrMaterialName))
                return new MaterialMaps { Diffuse = textureOrMaterialName };

            var material = resolveMaterial ? TryParseMaterial(index, textureOrMaterialName) : null;
            if (material != null)
            {
                return new MaterialMaps
                {
                    Diffuse = EffectiveSlot(material, 0) ?? textureOrMaterialName,
                    Normal = EffectiveSlot(material, 1),
                    Specular = EffectiveSlot(material, 2),
                    Roughness = EffectiveSlot(material, 3)
                };
            }

            return new MaterialMaps
            {
                Diffuse = textureOrMaterialName,
                Normal = FindCompanionTexture(index, textureOrMaterialName, "_n"),
                Specular = FindCompanionTexture(index, textureOrMaterialName, "_s"),
                Roughness = FindCompanionTexture(index, textureOrMaterialName, "_r")
            };
        }

        /// <summary>Returns the shared parsed MTR resource, or null when the name has no material.</summary>
        public static MtrDocument? TryParseMaterial(ResourceIndex index, string materialName)
        {
            var identity = new ResourceIdentity(materialName, ResourceIdentity.TypeFromExtension("mtr"));
            if (!index.TryLookup(identity, out var handle))
                return null;

            var bytes = handle.GetBytes();
            return bytes.Length == 0 ? null : Parse(bytes);
        }

        private static string? EffectiveSlot(MtrDocument material, int slot)
        {
            if (!material.Textures.TryGetValue(slot, out var declared))
                return null;

            return string.IsNullOrWhiteSpace(declared) || declared.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? null
                : declared;
        }

        private static string? FindCompanionTexture(ResourceIndex index, string diffuse, string suffix)
        {
            var candidate = diffuse + suffix;
            return TextureResourceExists(index, candidate) ? candidate : null;
        }

        private static bool TextureResourceExists(ResourceIndex index, string resRef) =>
            index.Contains(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("tga"))) ||
            index.Contains(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("dds")));
    }
}
