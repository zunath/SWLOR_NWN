using System.Globalization;
using System.Text;

namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// Parses NWN .set tileset files (INI-style text) into a <see cref="TilesetDefinition"/>.
    ///
    /// Grammar, as derived from the SWLOR_Haks tileset corpus (not from any spec):
    ///   [GENERAL]           - Name, Type, Version, Interior, HasHeightTransition, EnvMap,
    ///                         Transition, UnlocalizedName, Border, Default, Floor,
    ///                         DisplayName (strref, sometimes absent), SelectorHeight (rare).
    ///   [GRASS]             - Grass (0/1), and when Grass=1: Density, Height, GrassTextureName,
    ///                         AmbientRed/Green/Blue, DiffuseRed/Green/Blue.
    ///   [TERRAIN TYPES]     - Count, followed by [TERRAIN0]..[TERRAIN{n-1}]: Name, StrRef
    ///                         (optional), UnlocalizedName (optional - used instead of StrRef by
    ///                         at least one corpus crosser/terrain entry).
    ///   [CROSSER TYPES]     - same shape as [TERRAIN TYPES] but for [CROSSER0]..[CROSSER{n-1}].
    ///   [PRIMARY RULES]     - Count, followed by [PRIMARY RULE0].. (note the space before the
    ///                         index - this is NOT [PRIMARY0]): Placed, PlacedHeight, Adjacent,
    ///                         AdjacentHeight, Changed, ChangedHeight.
    ///   [SECONDARY RULES]   - same shape as [PRIMARY RULES] via [SECONDARY RULE0]... Every file
    ///                         in the corpus has Count=0 here, so the format is inferred from the
    ///                         primary-rule shape rather than observed directly.
    ///   [TILES]             - Count, followed by [TILE0]..[TILE{n-1}]: Model, WalkMesh, four
    ///                         corner terrains (TopLeft/TopRight/BottomLeft/BottomRight) each with
    ///                         a *Height companion key, four edge crossers (Top/Right/Bottom/Left,
    ///                         blank when absent), MainLight1/2, SourceLight1/2, AnimLoop1/2/3,
    ///                         Doors, Sounds, PathNode, Orientation, optional VisibilityNode/
    ///                         VisibilityOrientation and DoorVisibilityNode/DoorVisibilityOrientation,
    ///                         optional ImageMap2D, and a rare per-tile Grass override. A tile's
    ///                         doors live in following [TILEnDOOR0], [TILEnDOOR1], ... blocks:
    ///                         Type, X, Y, Z, Orientation.
    ///                         SURPRISE: the tile's own "Doors=" count cannot be trusted - the
    ///                         corpus contains tiles with garbage Doors values (including negative
    ///                         numbers like -481034240) that have zero actual [TILEnDOORd] blocks.
    ///                         This parser therefore ignores Doors when deciding how many door
    ///                         blocks to read; it scans for however many are actually present.
    ///   [GROUPS]            - Count, followed by [GROUP0].. : Name, Rows, Columns, optional
    ///                         StrRef, and Tile0..Tile{Rows*Columns-1} holding indices into
    ///                         [TILES].
    ///
    /// Tolerance policy: comments (";" to end of line) and blank lines are ignored anywhere.
    /// Section and key lookups are case-insensitive (the corpus itself is inconsistent - e.g. one
    /// file uses a lowercase "floor=" key alongside titlecase keys everywhere else). Duplicate
    /// keys within a section: last-wins (NWN's own toolset behavior here is unknown/unspecified;
    /// last-wins was chosen as the ordinary "last write wins" INI convention and documented here
    /// rather than guessed at silently). Unknown sections and unknown keys are ignored rather than
    /// rejected, since this is a read-only parser. Count keys on [TERRAIN TYPES]/[CROSSER TYPES]/
    /// [PRIMARY RULES]/[SECONDARY RULES]/[TILES]/[GROUPS] are read but not trusted as the sole
    /// source of truth: every repeated block is discovered by scanning sequential indices
    /// (0, 1, 2, ...) until one is missing, so a wrong/corrupt Count never causes data loss or a
    /// crash.
    /// </summary>
    public static class SetFileParser
    {
        public static TilesetDefinition Parse(string content)
        {
            var sections = Tokenize(content);
            return Build(sections);
        }

        public static TilesetDefinition Parse(byte[] content)
        {
            // The corpus is ASCII except for one file with a stray Windows-1252 accented
            // character in a group name; Latin1 maps every byte 1:1 so it never throws.
            return Parse(Encoding.Latin1.GetString(content));
        }

        public static TilesetDefinition ParseFile(string path)
        {
            return Parse(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Reads ONLY the [GENERAL] header (name/display name) without building the tile, terrain,
        /// crosser, or group tables. The corpus is 70 files / ~16 MB, and the largest single tileset
        /// declares over a thousand tiles, so fully parsing every one just to label a picker would
        /// be wasteful - [GENERAL] is the first section, and this stops as soon as the next section
        /// begins. Never throws; a file with no [GENERAL] yields empty strings.
        /// </summary>
        public static TilesetHeader ParseHeader(byte[] content)
        {
            var text = Encoding.Latin1.GetString(content);
            string name = "", unlocalizedName = "";
            var displayName = -1;
            var inGeneral = false;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == ';')
                    continue;

                if (line[0] == '[')
                {
                    if (inGeneral)
                        break; // [GENERAL] is done - everything after it is table data we don't need.

                    inGeneral = line.Equals("[GENERAL]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inGeneral)
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();

                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    name = value;
                else if (key.Equals("UnlocalizedName", StringComparison.OrdinalIgnoreCase))
                    unlocalizedName = value;
                else if (key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(value, out var parsed))
                    displayName = parsed;
            }

            return new TilesetHeader(name, unlocalizedName, displayName);
        }

        private static TilesetDefinition Build(IReadOnlyDictionary<string, IniSection> sections)
        {
            var general = GetSection(sections, "GENERAL");
            var grass = GetSection(sections, "GRASS");

            return new TilesetDefinition
            {
                Name = general.GetString("Name"),
                Type = general.GetString("Type"),
                Version = general.GetString("Version"),
                Interior = general.GetBool("Interior"),
                HasHeightTransition = general.GetBool("HasHeightTransition"),
                EnvMap = general.GetString("EnvMap"),
                Transition = general.GetInt("Transition"),
                UnlocalizedName = general.GetString("UnlocalizedName"),
                Border = general.GetString("Border"),
                Default = general.GetString("Default"),
                Floor = general.GetString("Floor"),
                DisplayName = general.GetInt("DisplayName", -1),
                SelectorHeight = general.GetIntNullable("SelectorHeight"),

                HasGrass = grass.GetBool("Grass"),
                GrassDensity = grass.GetDoubleNullable("Density"),
                GrassHeight = grass.GetDoubleNullable("Height"),
                GrassTextureName = grass.GetStringNullable("GrassTextureName"),
                AmbientRed = grass.GetDoubleNullable("AmbientRed"),
                AmbientGreen = grass.GetDoubleNullable("AmbientGreen"),
                AmbientBlue = grass.GetDoubleNullable("AmbientBlue"),
                DiffuseRed = grass.GetDoubleNullable("DiffuseRed"),
                DiffuseGreen = grass.GetDoubleNullable("DiffuseGreen"),
                DiffuseBlue = grass.GetDoubleNullable("DiffuseBlue"),

                Terrains = BuildNamedList(sections, "TERRAIN", BuildTerrain),
                Crossers = BuildNamedList(sections, "CROSSER", BuildCrosser),
                PrimaryRules = BuildNamedList(sections, "PRIMARY RULE", BuildRule),
                SecondaryRules = BuildNamedList(sections, "SECONDARY RULE", BuildRule),
                Tiles = BuildNamedList(sections, "TILE", (s, i) => BuildTile(sections, s, i)),
                Groups = BuildNamedList(sections, "GROUP", BuildGroup)
            };
        }

        private static TerrainDefinition BuildTerrain(IniSection section, int index)
        {
            return new TerrainDefinition(
                section.GetString("Name"),
                section.GetIntNullable("StrRef"),
                section.GetStringNullable("UnlocalizedName"));
        }

        private static CrosserDefinition BuildCrosser(IniSection section, int index)
        {
            return new CrosserDefinition(
                section.GetString("Name"),
                section.GetIntNullable("StrRef"),
                section.GetStringNullable("UnlocalizedName"));
        }

        private static TileRuleDefinition BuildRule(IniSection section, int index)
        {
            return new TileRuleDefinition(
                section.GetString("Placed"),
                section.GetInt("PlacedHeight"),
                section.GetString("Adjacent"),
                section.GetInt("AdjacentHeight"),
                section.GetString("Changed"),
                section.GetInt("ChangedHeight"));
        }

        private static TileDefinition BuildTile(
            IReadOnlyDictionary<string, IniSection> sections, IniSection section, int tileIndex)
        {
            var doors = new List<TileDoorDefinition>();
            for (var doorIndex = 0; ; doorIndex++)
            {
                if (!sections.TryGetValue($"TILE{tileIndex}DOOR{doorIndex}", out var doorSection))
                    break;

                doors.Add(new TileDoorDefinition(
                    doorSection.GetInt("Type"),
                    doorSection.GetDouble("X"),
                    doorSection.GetDouble("Y"),
                    doorSection.GetDouble("Z"),
                    doorSection.GetDouble("Orientation")));
            }

            return new TileDefinition
            {
                Model = section.GetString("Model"),
                WalkMesh = section.GetString("WalkMesh"),

                TopLeft = section.GetString("TopLeft"),
                TopLeftHeight = section.GetInt("TopLeftHeight"),
                TopRight = section.GetString("TopRight"),
                TopRightHeight = section.GetInt("TopRightHeight"),
                BottomLeft = section.GetString("BottomLeft"),
                BottomLeftHeight = section.GetInt("BottomLeftHeight"),
                BottomRight = section.GetString("BottomRight"),
                BottomRightHeight = section.GetInt("BottomRightHeight"),

                Top = section.GetString("Top"),
                Right = section.GetString("Right"),
                Bottom = section.GetString("Bottom"),
                Left = section.GetString("Left"),

                MainLight1 = section.GetInt("MainLight1"),
                MainLight2 = section.GetInt("MainLight2"),
                SourceLight1 = section.GetInt("SourceLight1"),
                SourceLight2 = section.GetInt("SourceLight2"),

                AnimLoop1 = section.GetInt("AnimLoop1"),
                AnimLoop2 = section.GetInt("AnimLoop2"),
                AnimLoop3 = section.GetInt("AnimLoop3"),

                Sounds = section.GetInt("Sounds"),
                PathNode = section.GetString("PathNode"),
                Orientation = section.GetDouble("Orientation"),

                VisibilityNode = section.GetStringNullable("VisibilityNode"),
                VisibilityOrientation = section.GetDoubleNullable("VisibilityOrientation"),
                DoorVisibilityNode = section.GetStringNullable("DoorVisibilityNode"),
                DoorVisibilityOrientation = section.GetDoubleNullable("DoorVisibilityOrientation"),

                ImageMap2D = section.GetStringNullable("ImageMap2D"),
                Grass = section.GetIntNullable("Grass"),

                DoorsRaw = section.GetInt("Doors"),
                Doors = doors
            };
        }

        private static TileGroupDefinition BuildGroup(IniSection section, int index)
        {
            var rows = section.GetInt("Rows");
            var columns = section.GetInt("Columns");

            var tileIndices = new List<int>();
            for (var tileSlot = 0; ; tileSlot++)
            {
                if (!section.TryGetString($"Tile{tileSlot}", out var raw))
                    break;

                tileIndices.Add(int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : 0);
            }

            return new TileGroupDefinition(
                section.GetString("Name"),
                rows,
                columns,
                section.GetIntNullable("StrRef"),
                tileIndices);
        }

        /// <summary>
        /// Builds a list from sequentially-indexed sections named "{prefix}0", "{prefix}1", ...,
        /// stopping at the first missing index. The declared "Count=" key (on the parent section
        /// sharing the pluralized name) is deliberately not consulted - see the type-level remarks
        /// on why a declared count cannot be trusted as authoritative in this corpus.
        /// </summary>
        private static IReadOnlyList<T> BuildNamedList<T>(
            IReadOnlyDictionary<string, IniSection> sections,
            string prefix,
            Func<IniSection, int, T> build)
        {
            var results = new List<T>();
            for (var index = 0; ; index++)
            {
                if (!sections.TryGetValue($"{prefix}{index}", out var section))
                    break;

                results.Add(build(section, index));
            }

            return results;
        }

        private static IniSection GetSection(IReadOnlyDictionary<string, IniSection> sections, string name)
        {
            return sections.TryGetValue(name, out var section) ? section : IniSection.Empty;
        }

        private static Dictionary<string, IniSection> Tokenize(string content)
        {
            var sections = new Dictionary<string, IniSection>(StringComparer.OrdinalIgnoreCase);
            IniSection? current = null;

            using var reader = new StringReader(content);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == ';')
                    continue;

                if (trimmed[0] == '[' && trimmed[^1] == ']')
                {
                    var name = trimmed[1..^1].Trim();
                    if (!sections.TryGetValue(name, out current))
                    {
                        current = new IniSection();
                        sections[name] = current;
                    }

                    continue;
                }

                // Stray key=value before any section header, or a malformed line with no '=':
                // ignore rather than crash - this is a tolerant, read-only parser.
                if (current == null)
                    continue;

                var separator = trimmed.IndexOf('=');
                if (separator < 0)
                    continue;

                var key = trimmed[..separator].Trim();
                var value = trimmed[(separator + 1)..].Trim();
                current.Set(key, value); // duplicate keys within a section: last-wins
            }

            return sections;
        }

        /// <summary>Case-insensitive key/value bag for a single [Section] block.</summary>
        private sealed class IniSection
        {
            public static readonly IniSection Empty = new();

            private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

            public void Set(string key, string value) => _values[key] = value;

            public bool TryGetString(string key, out string value) => _values.TryGetValue(key, out value!);

            public string GetString(string key, string fallback = "") =>
                _values.TryGetValue(key, out var value) ? value : fallback;

            public string? GetStringNullable(string key) =>
                _values.TryGetValue(key, out var value) && value.Length > 0 ? value : null;

            public int GetInt(string key, int fallback = 0) =>
                _values.TryGetValue(key, out var value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : fallback;

            public int? GetIntNullable(string key) =>
                _values.TryGetValue(key, out var value) && value.Length > 0 &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : null;

            public double GetDouble(string key, double fallback = 0) =>
                _values.TryGetValue(key, out var value) &&
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : fallback;

            public double? GetDoubleNullable(string key) =>
                _values.TryGetValue(key, out var value) && value.Length > 0 &&
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : null;

            public bool GetBool(string key, bool fallback = false) =>
                GetInt(key, fallback ? 1 : 0) != 0;
        }
    }
}
