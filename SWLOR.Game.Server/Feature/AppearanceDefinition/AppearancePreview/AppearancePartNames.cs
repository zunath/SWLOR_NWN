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
