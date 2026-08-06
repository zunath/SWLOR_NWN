using System.Globalization;
using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// Converts the stable identifiers used by the server into labels suitable for builders.
    /// The identifier is still kept as the option value and is therefore what gets saved.
    /// </summary>
    internal static class TableChoiceDisplayName
    {
        private static readonly Regex WordBoundary = new(
            @"(?<=[a-z0-9])(?=[A-Z])",
            RegexOptions.CultureInvariant);

        private static readonly IReadOnlyDictionary<string, string> KnownTokens =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ABSDEF"] = "Absolute Defense",
                ["ADAMGUARD"] = "Adamantine Guard",
                ["ALPHARHY"] = "Alpha Rhythm",
                ["APEXBITE"] = "Apex Bite",
                ["BYYSKGUARDIAN"] = "Byysk Guardian",
                ["CORALISLEINNER"] = "Coral Isle Inner",
                ["CORALISLEOUTER"] = "Coral Isle Outer",
                ["CZ220"] = "CZ-220",
                ["CRIPDEF"] = "Crippling Defense",
                ["DAN"] = "Dantooine",
                ["DANENCLAVE"] = "Dantooine Jedi Enclave",
                ["DANMED"] = "Dantooine Medical Sublevel",
                ["DATH"] = "Dathomir",
                ["DATHGROTTO"] = "Dathomir Grotto",
                ["DARKHUNG"] = "Hunger of the Dark",
                ["DEADEYE"] = "Deadeye",
                ["DEADHAND"] = "Dead Man's Hand",
                ["DECCOMMAND"] = "Decisive Command",
                ["DEEPMOUNTAIN"] = "Deep Mountain",
                ["DEEPMOUNTAINS"] = "Deep Mountains",
                ["ECLIPSE"] = "Eclipse of Resolve",
                ["ECOTERRORIST"] = "Eco-Terrorist",
                ["ECOTERRORISTS"] = "Eco-Terrorists",
                ["EMBUNKER"] = "Emergency Bunker",
                ["EMCOCKTAIL"] = "Emergency Cocktail",
                ["FORCEBANE"] = "Forcebane",
                ["FORCEBEAST"] = "Force-Bonded Beast",
                ["FIGHTCLUB"] = "Fight Club",
                ["FLESHEATER"] = "Flesh Eater",
                ["FLESHEATERS"] = "Flesh Eaters",
                ["FLESHLEADER"] = "Flesh Leader",
                ["GUARDMST"] = "Guardian Master",
                ["HOLDLINE"] = "Hold the Line",
                ["INFCONDUIT"] = "Infinite Conduit",
                ["INVINC"] = "Invincible",
                ["KILLBEACON"] = "Killzone Beacon",
                ["KILLBOX"] = "Kill Box",
                ["KORCRYPT"] = "Korriban Crypt",
                ["KORFORGE"] = "Korriban Forge",
                ["LASTWORD"] = "Last Word",
                ["LIGHTSTAND"] = "Last Stand of the Light",
                ["MANDALORIANFACILITY"] = "Mandalorian Facility",
                ["MONC"] = "Mon Cala",
                ["MOUNTAINVALLEY"] = "Mountain Valley",
                ["NAR"] = "Nar Shaddaa",
                ["NARSHADDAA"] = "Nar Shaddaa",
                ["NORTHERNDUNES"] = "Northern Dunes",
                ["ONESHOT"] = "One Shot",
                ["OVERBARR"] = "Overload Barrage",
                ["PERFLURRY"] = "Perfect Flurry",
                ["PRIMOVER"] = "Primal Overrun",
                ["QIONHIVE"] = "Qion Hive",
                ["QIONTUNDRA"] = "Qion Tundra",
                ["RAINSTEEL"] = "Rain of Steel",
                ["REDBLOOM"] = "Red Bloom",
                ["SABCYCL"] = "Saber Cyclone",
                ["SABSTORM"] = "Saber Storm",
                ["SANDDEMON"] = "Sand Demon",
                ["SANDSWIMMER"] = "Sand Swimmer",
                ["SANDSWIMMERS"] = "Sand Swimmers",
                ["SCRAPLOCK"] = "Scrapheap Lockdown",
                ["SOULASC"] = "Soul Ascension",
                ["SYSSHUT"] = "Systemic Shutdown",
                ["TAT"] = "Tatooine",
                ["TATTOOINE"] = "Tatooine",
                ["TEMPBLOOM"] = "Tempest Bloom",
                ["THERMDET"] = "Thermal Detonator",
                ["UNBRBEAST"] = "Unbreakable Beast",
                ["UNMOVCTR"] = "Unmoving Center",
                ["UNTINST"] = "Untouchable Instinct",
                ["VELESSEWERS"] = "Veles Sewers",
                ["VISC"] = "Viscara",
                ["VISBUNKER"] = "Viscara Bunker",
                ["VITRUPT"] = "Vital Rupture",
                ["WORLDBRK"] = "Worldbreaker"
            };

        public static string FromIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return string.Empty;

            var tokens = WordBoundary
                .Replace(identifier.Trim(), "_")
                .Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var sections = new List<string>();
            AddPrefix(tokens, sections);
            var suffix = TakeSuffix(tokens);

            if (tokens.Count > 0)
                sections.Add(string.Join(" ", tokens.Select(HumanizeToken)));

            if (suffix != null)
                sections.Add(suffix);

            return sections.Count > 0
                ? string.Join(" - ", sections)
                : HumanizeToken(identifier);
        }

        private static void AddPrefix(List<string> tokens, ICollection<string> sections)
        {
            if (StartsWith(tokens, "ANCHRANGE", "CANYON"))
            {
                sections.Add("Anchorhead Canyon Range");
                tokens.RemoveRange(0, 2);
            }
            else if (StartsWith(tokens, "ANCHRANGE"))
            {
                sections.Add("Anchorhead Range");
                tokens.RemoveAt(0);
            }
            else if (StartsWith(tokens, "SPACE", "RESOURCES"))
            {
                sections.Add("Space Resources");
                tokens.RemoveRange(0, 2);
            }
            else if (StartsWith(tokens, "FP"))
            {
                sections.Add("Fishing");
                tokens.RemoveAt(0);
            }
            else if (StartsWith(tokens, "ASTEROID"))
            {
                sections.Add("Asteroid");
                tokens.RemoveAt(0);
            }
            else if (StartsWith(tokens, "HARVESTING"))
            {
                sections.Add("Harvesting");
                tokens.RemoveAt(0);
            }
            else if (StartsWith(tokens, "RESOURCES"))
            {
                sections.Add("Resources");
                tokens.RemoveAt(0);
            }
            else if (StartsWith(tokens, "CAPSTONE"))
            {
                sections.Add("Capstone");
                tokens.RemoveAt(0);
            }
        }

        private static string? TakeSuffix(List<string> tokens)
        {
            if (EndsWith(tokens, "WD", "RARES"))
            {
                tokens.RemoveRange(tokens.Count - 2, 2);
                return "Warden Rare Drops";
            }

            if (EndsWith(tokens, "LESSON", "LOOT"))
            {
                tokens.RemoveRange(tokens.Count - 2, 2);
                return "Lesson Loot";
            }

            if (EndsWith(tokens, "BOSS", "LOOT"))
            {
                tokens.RemoveRange(tokens.Count - 2, 2);
                return "Boss Loot";
            }

            var suffixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["COMP"] = "Components",
                ["COMPONENT"] = "Components",
                ["COMPONENTS"] = "Components",
                ["LOOT"] = "Loot",
                ["RARES"] = "Rare Drops",
                ["RESOURCES"] = "Resources",
                ["STRIP"] = "Strip Mining"
            };

            if (tokens.Count == 0 ||
                !suffixes.TryGetValue(tokens[^1], out var suffix))
            {
                return null;
            }

            tokens.RemoveAt(tokens.Count - 1);
            return suffix;
        }

        private static string HumanizeToken(string token)
        {
            if (KnownTokens.TryGetValue(token, out var known))
                return known;

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token.ToLowerInvariant());
        }

        private static bool StartsWith(IReadOnlyList<string> tokens, params string[] expected)
        {
            if (tokens.Count < expected.Length)
                return false;

            for (var index = 0; index < expected.Length; index++)
            {
                if (!string.Equals(tokens[index], expected[index], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool EndsWith(IReadOnlyList<string> tokens, params string[] expected)
        {
            if (tokens.Count < expected.Length)
                return false;

            var offset = tokens.Count - expected.Length;
            for (var index = 0; index < expected.Length; index++)
            {
                if (!string.Equals(
                        tokens[offset + index],
                        expected[index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
