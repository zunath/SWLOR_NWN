using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Data
{
    /// <summary>
    /// The astromech abilities and the humanoid abilities they copy. An astromech's Utility Systems and Armaments
    /// abilities are generated number for number from First Aid, Staff and Rifle lines
    /// (tools/GenerateAstromechSkillMirrors.py), and apply the same status effects. The table is read from that tool's
    /// configuration, which is embedded in this assembly, so the simulator never repeats a number: a mirrored ability
    /// uses the effect model of the ability it copies.
    /// </summary>
    public static class AstromechMirrors
    {
        private const string ResourceName = "AstromechSkillMirrors.json";

        private static readonly Lazy<Dictionary<FeatType, FeatType>> SourceByFeat = new(Load);

        /// <summary>Every astromech ability feat and the humanoid feat it copies.</summary>
        public static IReadOnlyDictionary<FeatType, FeatType> Feats => SourceByFeat.Value;

        /// <summary>The humanoid ability an astromech ability copies.</summary>
        public static bool TryGetSource(FeatType feat, out FeatType source) => SourceByFeat.Value.TryGetValue(feat, out source);

        /// <summary>The body frame a perk category is open to: any, humanoid only or astromech only.</summary>
        public static BodyFrameRestriction GetRestriction(PerkCategoryType category)
        {
            var member = typeof(PerkCategoryType).GetField(category.ToString());
            var attribute = member?.GetCustomAttributes(typeof(PerkCategoryAttribute), false).OfType<PerkCategoryAttribute>().FirstOrDefault();
            return attribute?.BodyFrameRestriction ?? BodyFrameRestriction.None;
        }

        /// <summary>True if a perk category can be bought by a character of this body frame.</summary>
        public static bool IsOpenTo(PerkCategoryType category, BodyFrame frame) => GetRestriction(category) switch
        {
            BodyFrameRestriction.HumanoidOnly => frame == BodyFrame.Humanoid,
            BodyFrameRestriction.AstromechOnly => frame == BodyFrame.Astromech,
            _ => true
        };

        private static Dictionary<FeatType, FeatType> Load()
        {
            using var stream = typeof(AstromechMirrors).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException($"The embedded resource {ResourceName} is missing.");
            using var reader = new StreamReader(stream);
            var config = JObject.Parse(reader.ReadToEnd());

            var map = new Dictionary<FeatType, FeatType>();
            foreach (var mirror in config["mirrors"]!)
            {
                var ledger = (JObject)config["ledger"]![mirror.Value<string>("skill")!]!;
                foreach (var line in mirror["lines"]!)
                {
                    foreach (var perk in ((JObject)line["perks"]!).Properties())
                    {
                        // The feats of a mirrored perk carry its id and the rank or Trait suffix of the source feat.
                        var id = perk.Value.Value<string>("id")!;
                        foreach (var entry in ledger.Properties().Where(property => property.Value["feat"] != null))
                        {
                            var suffix = entry.Name.StartsWith(id, StringComparison.Ordinal) ? entry.Name[id.Length..] : null;
                            if (suffix == null || suffix != "Trait" && !suffix.All(char.IsDigit) || suffix.Length == 0)
                                continue;
                            if (!Enum.TryParse<FeatType>(entry.Name, out var feat) || !Enum.TryParse<FeatType>(perk.Name + suffix, out var source))
                                throw new InvalidDataException($"Astromech feat {entry.Name} or its source {perk.Name + suffix} is not a FeatType.");
                            map[feat] = source;
                        }
                    }
                }
            }

            return map;
        }
    }
}
