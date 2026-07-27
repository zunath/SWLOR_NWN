using System.Text;
using System.Text.RegularExpressions;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Minimal parse of an NWN:EE MTR (material) file: plain-text lines declaring
    /// <c>textureN</c> slots, a <c>renderhint</c>, and <c>customshaderXXX</c> overrides. <c>//</c>
    /// starts a comment (to end of line); everything else is ignored.
    /// </summary>
    public sealed class MtrMaterial
    {
        public string? RenderHint { get; init; }

        /// <summary>Texture slot index (from <c>textureN</c>) to the declared texture resref.</summary>
        public IReadOnlyDictionary<int, string> Textures { get; init; } = new Dictionary<int, string>();

        /// <summary>Raw <c>customshaderXXX</c> key/value pairs, keyed by the full key (e.g. "customshaderVSH").</summary>
        public IReadOnlyDictionary<string, string> CustomShaders { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The declared texture for <paramref name="slot"/> (default: <c>texture0</c>, the diffuse map), or null.</summary>
        public string? GetTexture(int slot = 0) => Textures.TryGetValue(slot, out var texture) ? texture : null;
    }

    /// <summary>
    /// Parses MTR (NWN:EE material) resources and resolves the effective diffuse texture name
    /// for a mesh's bitmap/material name. This deliberately small parser does not attempt to model
    /// render hints, custom shaders, or parameters beyond exposing them as raw data for later
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

                // Unknown keys ignored.
            }

            return new MtrMaterial
            {
                RenderHint = renderHint,
                Textures = textures,
                CustomShaders = customShaders
            };
        }

        /// <summary>
        /// Resolve the effective diffuse texture name for <paramref name="textureOrMaterialName"/>:
        /// if an .mtr resource (restype 2072) with that name exists in <paramref name="index"/>,
        /// parse it and return its <c>texture0</c> slot; otherwise the name passes through
        /// unchanged (the common case - most meshes reference a texture directly, with no
        /// material override).
        /// </summary>
        public static string ResolveDiffuseTextureName(ResourceIndex index, string textureOrMaterialName)
        {
            if (string.IsNullOrWhiteSpace(textureOrMaterialName))
                return textureOrMaterialName;

            var identity = new ResourceIdentity(textureOrMaterialName, ResourceIdentity.TypeFromExtension("mtr"));
            if (!index.TryLookup(identity, out var handle))
                return textureOrMaterialName;

            var bytes = handle.GetBytes();
            if (bytes.Length == 0)
                return textureOrMaterialName;

            var material = Parse(Encoding.ASCII.GetString(bytes));
            var diffuse = material.GetTexture(0);
            return string.IsNullOrWhiteSpace(diffuse) ? textureOrMaterialName : diffuse;
        }

        private static readonly char[] WhitespaceChars = { ' ', '\t' };
    }
}
