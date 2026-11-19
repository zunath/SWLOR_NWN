using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview
{
    /// <summary>
    /// Display names of the Appearance Editor part pictures, read from AppearancePartNames.json.
    /// Heads are named per body model; armor and body parts share one name across bodies and
    /// between the left and right side; helmets and weapon parts are named per picture.
    /// </summary>
    public static class AppearancePartNames
    {
        public const string ResourceName = "SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview.AppearancePartNames.json";

        /// <summary>The longest name that fits under a part picture.</summary>
        public const int MaxLength = 13;
        public const int MaxHeadLength = MaxLength * 2 + 1;

        public static (string First, string Second) CaptionLines(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= MaxLength)
                return (name ?? string.Empty, string.Empty);
            var split = name.IndexOf(" (", StringComparison.Ordinal);
            if (split < 1 || split > MaxLength || name.Length - split - 1 > MaxLength)
                split = name.LastIndexOf(' ', Math.Min(MaxLength, name.Length - 1));
            return split > 0 ? (name.Substring(0, split), name.Substring(split + 1)) : (name, string.Empty);
        }

        /// <summary>
        /// The widest name, in pixels of the game's label font at GUI scale 1, that fits under a
        /// part picture. The label is 106 wide; the margin keeps neighbouring names apart.
        /// </summary>
        public const int MaxWidth = 101;

        // Advance widths of the game's label font (fnt_maintext at 16 px): A-Z, then a-z.
        private static readonly int[] UpperWidths =
            { 11, 8, 11, 11, 8, 8, 12, 10, 4, 4, 9, 8, 14, 11, 12, 8, 12, 8, 8, 10, 11, 10, 14, 9, 8, 9 };
        private static readonly int[] LowerWidths =
            { 7, 8, 7, 9, 8, 6, 7, 8, 3, 3, 7, 3, 12, 8, 8, 8, 9, 5, 5, 7, 8, 7, 11, 7, 7, 7 };

        /// <summary>The drawn width of a name in the game's label font. Other characters count as the widest letter.</summary>
        public static int Width(string name)
        {
            var width = 0;
            foreach (var character in name ?? string.Empty)
            {
                width += character switch
                {
                    >= 'A' and <= 'Z' => UpperWidths[character - 'A'],
                    >= 'a' and <= 'z' => LowerWidths[character - 'a'],
                    ' ' => 7,
                    '\'' => 2,
                    '-' => 5,
                    _ => 14
                };
            }
            return width;
        }

        /// <summary>
        /// A weapon definition's own part name as shown under a picture: a name too wide for the
        /// tile loses its bracketed note ("Bare Fists (Invisible)" shows as "Bare Fists").
        /// </summary>
        public static string TileName(string name)
        {
            if (string.IsNullOrEmpty(name) || Width(name) <= MaxWidth)
                return name;
            var note = name.IndexOf(" (", StringComparison.Ordinal);
            return note > 0 ? name[..note] : name;
        }

        private static readonly Regex BodyPart = new("^p[fm][a-z]0_([a-z]+?)([lr]?)(\\d{3})$", RegexOptions.Compiled);
        private static readonly HashSet<string> SidedStems = new(StringComparer.Ordinal)
        {
            "bicep", "fore", "hand", "leg", "shin", "foot", "sho"
        };

        private static readonly Lazy<IReadOnlyDictionary<string, string>> Names = new(Load);

        /// <summary>Every name key with its name.</summary>
        public static IReadOnlyDictionary<string, string> All => Names.Value;

        /// <summary>
        /// The name of a part picture key (see <see cref="AppearancePreview"/>), or the fallback when
        /// the picture has no name. A head of another body type falls back to the human head's name,
        /// as its picture does.
        /// </summary>
        public static string Get(string pictureKey, string fallback)
        {
            if (string.IsNullOrEmpty(pictureKey))
                return fallback;
            if (Names.Value.TryGetValue(NameKey(pictureKey), out var name))
                return name;
            var twin = AppearancePreview.HumanTwinKey(pictureKey);
            return twin != null && Names.Value.TryGetValue(NameKey(twin), out name) ? name : fallback;
        }

        /// <summary>The name key of a picture key: pmh0_bicepl012 and pfh0_bicepr012 are both bicep012.</summary>
        public static string NameKey(string pictureKey)
        {
            var key = pictureKey.ToLowerInvariant();
            var match = BodyPart.Match(key);
            if (!match.Success)
                return key;
            var stem = match.Groups[1].Value;
            var side = match.Groups[2].Value;
            if (stem == "head")
                return key;
            return side.Length > 0 && SidedStems.Contains(stem)
                ? $"{stem}{match.Groups[3].Value}"
                : $"{stem}{side}{match.Groups[3].Value}";
        }

        private static IReadOnlyDictionary<string, string> Load()
        {
            using var stream = typeof(AppearancePartNames).Assembly.GetManifestResourceStream(ResourceName) ??
                               throw new InvalidOperationException($"Embedded resource '{ResourceName}' could not be opened.");
            using var reader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd()) ??
                   throw new InvalidOperationException($"Embedded resource '{ResourceName}' is empty.");
        }
    }
}
