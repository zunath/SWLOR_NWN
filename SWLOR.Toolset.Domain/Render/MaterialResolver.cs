using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Minimal parse of an NWN:EE MTR (material) file: plain-text lines declaring
    /// <c>textureN</c> slots, a <c>renderhint</c>, <c>customshaderXXX</c> overrides, and named
    /// <c>parameter</c> values. <c>//</c> starts a comment (to end of line); everything else is
    /// ignored.
    /// </summary>
    public sealed class MtrMaterial
    {
        public string? RenderHint { get; init; }

        /// <summary>Texture slot index (from <c>textureN</c>) to the declared texture resref.</summary>
        public IReadOnlyDictionary<int, string> Textures { get; init; } = new Dictionary<int, string>();

        /// <summary>Raw <c>customshaderXXX</c> key/value pairs, keyed by the full key (e.g. "customshaderVSH").</summary>
        public IReadOnlyDictionary<string, string> CustomShaders { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Named values from <c>parameter TYPE NAME VALUE...</c> declarations.</summary>
        public IReadOnlyDictionary<string, string> Parameters { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The declared texture for <paramref name="slot"/> (default: <c>texture0</c>, the diffuse map), or null.</summary>
        public string? GetTexture(int slot = 0) => Textures.TryGetValue(slot, out var texture) ? texture : null;

        /// <summary>
        /// Returns the texture and channel selected by an enabled <c>useTextureNAlpha</c>
        /// parameter, or null. The tint shader treats texture9 as a red-channel cutout mask;
        /// normal-map texture1 stores its cutout in alpha.
        /// </summary>
        public MtrAlphaSource? GetAlphaSource()
        {
            const string prefix = "useTexture";
            const string suffix = "Alpha";
            foreach (var (name, rawValue) in Parameters)
            {
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var enabledText = rawValue.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                if (!float.TryParse(enabledText, NumberStyles.Float, CultureInfo.InvariantCulture, out var enabled) ||
                    enabled <= 0f)
                {
                    continue;
                }

                var slotText = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
                if (!int.TryParse(slotText, NumberStyles.None, CultureInfo.InvariantCulture, out var slot))
                    continue;

                var texture = GetTexture(slot);
                if (!string.IsNullOrWhiteSpace(texture))
                    return new MtrAlphaSource(texture, UsesRedChannel: slot == 9);
            }

            return null;
        }

        public string? GetAlphaTexture() => GetAlphaSource()?.TextureName;
    }

    /// <summary>
    /// Parses MTR (NWN:EE material) resources and resolves the effective diffuse texture name
    /// for a mesh's bitmap/material name. This deliberately small parser does not attempt to model
    /// render hints, custom shaders, or parameters beyond exposing their declarations for later
    /// packages to consume.
    /// </summary>
    public static class MaterialResolver
    {
        private static readonly Regex TextureSlotPattern =
            new(@"^texture(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static MtrMaterial Parse(string text)
        {
            string? renderHint = null;
            var textures = new Dictionary<int, string>();
            var customShaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r').Trim();

                var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                if (commentIndex >= 0)
                    line = line[..commentIndex].TrimEnd();

                if (line.Length == 0)
                    continue;

                var spaceIndex = line.IndexOfAny(WhitespaceChars);
                var key = spaceIndex < 0 ? line : line[..spaceIndex];
                var value = spaceIndex < 0 ? string.Empty : line[(spaceIndex + 1)..].Trim();

                if (value.Length == 0)
                    continue;

                var slotMatch = TextureSlotPattern.Match(key);
                if (slotMatch.Success)
                {
                    textures[int.Parse(slotMatch.Groups[1].Value)] = value;
                    continue;
                }

                if (key.Equals("renderhint", StringComparison.OrdinalIgnoreCase))
                {
                    renderHint = value;
                    continue;
                }

                if (key.StartsWith("customshader", StringComparison.OrdinalIgnoreCase))
                {
                    customShaders[key] = value;
                    continue;
                }

                if (key.Equals("parameter", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = value.Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                        parameters[parts[1]] = string.Join(' ', parts.Skip(2));
                    continue;
                }

                // Unknown keys ignored.
            }

            return new MtrMaterial
            {
                RenderHint = renderHint,
                Textures = textures,
                CustomShaders = customShaders,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Resolve the effective diffuse texture name for <paramref name="textureOrMaterialName"/>:
        /// if an .mtr resource (restype 2072) with that name exists in <paramref name="index"/>,
        /// parse it and return its <c>texture0</c> slot; otherwise the name passes through
        /// unchanged (the common case - most meshes reference a texture directly, with no
        /// material override).
        /// </summary>
        public static string ResolveDiffuseTextureName(
            ResourceIndex index,
            string textureOrMaterialName,
            bool resolveMaterial = true)
        {
            if (string.IsNullOrWhiteSpace(textureOrMaterialName))
                return textureOrMaterialName;

            var material = resolveMaterial ? TryParseMaterial(index, textureOrMaterialName) : null;
            if (material == null)
                return textureOrMaterialName;

            var diffuse = material.GetTexture(0);
            return string.IsNullOrWhiteSpace(diffuse) ? textureOrMaterialName : diffuse;
        }

        /// <summary>
        /// Resolve the full map set for <paramref name="textureOrMaterialName"/>. With an .mtr
        /// present, its slots decide everything: <c>texture0</c> is the diffuse (falling back to
        /// the input name), <c>texture1</c> the normal map, <c>texture2</c> the specular map,
        /// <c>texture3</c> the roughness map - a blank slot or the literal <c>null</c> placeholder
        /// means the material has none, and no further guessing happens. Without an .mtr, NWN:EE's
        /// automatic companion-texture convention applies: a TGA or DDS named
        /// <c>&lt;diffuse&gt;_n</c> is the normal map, <c>&lt;diffuse&gt;_s</c> the specular map
        /// and <c>&lt;diffuse&gt;_r</c> the roughness map, when such a resource exists.
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
                    Diffuse = EffectiveSlot(material.GetTexture(0)) ?? textureOrMaterialName,
                    Normal = EffectiveSlot(material.GetTexture(1)),
                    Specular = EffectiveSlot(material.GetTexture(2)),
                    Roughness = EffectiveSlot(material.GetTexture(3))
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

        /// <summary>Returns the parsed MTR resource, or null when the name has no material.</summary>
        public static MtrMaterial? TryParseMaterial(ResourceIndex index, string materialName)
        {
            var identity = new ResourceIdentity(materialName, ResourceIdentity.TypeFromExtension("mtr"));
            if (!index.TryLookup(identity, out var handle))
                return null;

            var bytes = handle.GetBytes();
            return bytes.Length == 0 ? null : Parse(Encoding.ASCII.GetString(bytes));
        }

        /// <summary>A declared slot value, where blank and the literal <c>null</c> placeholder both mean absent.</summary>
        private static string? EffectiveSlot(string? declared) =>
            string.IsNullOrWhiteSpace(declared) || declared.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? null
                : declared;

        private static string? FindCompanionTexture(ResourceIndex index, string diffuse, string suffix)
        {
            var candidate = diffuse + suffix;
            return TextureResourceExists(index, candidate) ? candidate : null;
        }

        // PLT is deliberately absent: companion maps are plain TGA/DDS artwork, never
        // palette-layered textures.
        private static bool TextureResourceExists(ResourceIndex index, string resRef) =>
            index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("tga")), out _) ||
            index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("dds")), out _);

        private static readonly char[] WhitespaceChars = { ' ', '\t' };
    }
}
