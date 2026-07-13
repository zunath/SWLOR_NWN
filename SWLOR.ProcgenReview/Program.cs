using System.Diagnostics;
using System.Text.Json.Nodes;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

// Builds a standalone review module ("SWLOR Procgen Review.mod") containing offline-generated
// areas, using the production solver and the real content-theme / tileset-profile / layout-profile
// composition (mirroring DungeonContentPlacer.GetComposition + DungeonComposition.BuildLayoutParameters
// + AreaGeneration.Generate's seed-derived retry). The module opens in the toolset or game without
// the full SWLOR module. All paths derive from the repository root (located by walking up to the
// solution file), so the tool runs on any machine or drive layout.
//
// Usage (from anywhere inside the repo):
//   dotnet run --project SWLOR.ProcgenReview -- [--seeds 4242,777,1337] [--size 16] [--out <path>]
//   dotnet run --project SWLOR.ProcgenReview -- --matrix
//   dotnet run --project SWLOR.ProcgenReview -- --areas minecave:::4242:16,minecave:sewers:organic:777:24
//   dotnet run --project SWLOR.ProcgenReview -- --areas minecave:::4242:16:2:2,scifibase:::777:16:1:3
//
// Default (no --areas): every registered theme x seeds 4242/777/1337, composed with its own default
// tileset/layout profiles (3 areas per theme). Entrance/exit counts are 1/1.
// --matrix: additionally emits one area per (tileset profile x layout profile) pair at seed 4242,
// also with entrance/exit counts 1/1.
// Content is irrelevant offline (no creatures/loot/exit/treasure are ever emitted by this tool), so
// matrix compositions carry no theme.
// --areas: comma-separated batch entries, either the 5-segment "theme:tileset:layout:seed:size"
// form (entrances/exits default to 1/1, doors defaults to "door"), the 7-segment
// "theme:tileset:layout:seed:size:entrances:exits" form, or the 8-segment
// "theme:tileset:layout:seed:size:entrances:exits:doors" form (doors is "door" or "plac"). tileset/
// layout may be left empty to use the theme's own defaults. When given, ONLY these areas are
// generated — no default set, no matrix.
// --areas-file <path>: JSON array of full-fidelity area entries (see AreaBatchFileEntry /
// AreaBatchFile) — { resref?, themeKey, tilesetKey, layoutKey, seed, size, parameters }, where
// parameters is a complete MacroLayoutParameters snapshot (post DungeonComposition.
// BuildLayoutParameters, post any Advanced-settings overrides) consumed VERBATIM instead of being
// recomposed from the theme/tileset/layout keys. This is what SWLOR.ContentBuilder's "Build Review
// Module" writes so the built module reproduces the exact preview, including knobs the 5/7/8-segment
// string spec cannot express (style, room counts/sizes, corridor width, loop factor, organic fill,
// accent, feature density). Like --areas, ONLY these areas are generated when given — no default
// set, no matrix. Combines additively with --areas if both are given.
// --extra-areas: same entry syntax as --areas, but ADDED on top of the default set (and --matrix, if
// also given) instead of replacing it — useful for appending a few showcase areas (e.g. higher
// entrance/exit counts to exercise door-style transitions) to the normal review build.
//
// Each generated area also gets one waypoint per entrance/exit transition point (tags PG_ENT_N /
// PG_EXIT_N, names "PG Entrance N" / "PG Exit N"), so transitions are visible when reviewing the
// area in the toolset.
//
// Output defaults to <repoRoot>/Module/SWLOR Procgen Review.mod — point nwn.ini's MODULES
// directory at <repoRoot>/Module (the SWLOR dev convention) and the toolset sees it directly.

var seeds = new List<int> { 4242, 777, 1337 };
var size = 16;
string outPath = null;
var matrix = false;
string areasArg = null;
string extraAreasArg = null;
string areasFileArg = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seeds":
            seeds = args[++i].Split(',').Select(int.Parse).ToList();
            break;
        case "--size":
            size = int.Parse(args[++i]);
            break;
        case "--out":
            outPath = args[++i];
            break;
        case "--matrix":
            matrix = true;
            break;
        case "--areas":
            areasArg = args[++i];
            break;
        case "--areas-file":
            areasFileArg = args[++i];
            break;
        case "--extra-areas":
            extraAreasArg = args[++i];
            break;
        default:
            Console.Error.WriteLine($"unknown argument '{args[i]}'");
            return 1;
    }
}

var root = FindRepositoryRoot();
outPath ??= Path.Combine(root, "Module", "SWLOR Procgen Review.mod");
var gffTool = Path.Combine(root, "tools", "SWLOR.CLI", "nwn_gff.exe");
var erfTool = Path.Combine(root, "tools", "SWLOR.CLI", "nwn_erf.exe");

if (!File.Exists(gffTool) || !File.Exists(erfTool))
{
    Console.Error.WriteLine($"nwn_gff/nwn_erf not found under {Path.Combine(root, "tools", "SWLOR.CLI")}");
    return 1;
}

var stage = Path.Combine(Path.GetTempPath(), "swlor_procgen_review_" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(stage);

try
{
    var themes = Discover<IDungeonListDefinition, DungeonDetail>(d => d.BuildDungeons());
    var tilesetProfiles = Discover<IDungeonTilesetProfileListDefinition, DungeonTilesetProfile>(d => d.BuildTilesetProfiles());
    var layoutProfiles = Discover<IDungeonLayoutProfileListDefinition, DungeonLayoutProfile>(d => d.BuildLayoutProfiles());

    if (themes.Count == 0)
    {
        Console.Error.WriteLine("no dungeon themes discovered");
        return 1;
    }

    var usedResrefs = new HashSet<string>();
    var specs = new List<AreaSpec>();

    // Shared by --areas (exclusive area list) and --extra-areas (appended on top of the default/
    // matrix set). resrefPrefix keeps the two from colliding when both are used together.
    void ParseAreaSpecs(string argValue, string flagName, string resrefPrefix)
    {
        var n = 1;
        foreach (var entry in argValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length != 5 && parts.Length != 7 && parts.Length != 8)
            {
                Console.Error.WriteLine($"{flagName} entry '{entry}': expected theme:tileset:layout:seed:size, theme:tileset:layout:seed:size:entrances:exits, or theme:tileset:layout:seed:size:entrances:exits:doors — skipped");
                continue;
            }

            var themeKey = parts[0];
            var tilesetOverride = parts[1];
            var layoutOverride = parts[2];
            if (!int.TryParse(parts[3], out var entrySeed) || !int.TryParse(parts[4], out var entrySize))
            {
                Console.Error.WriteLine($"{flagName} entry '{entry}': seed/size must be integers — skipped");
                continue;
            }

            var entryEntrances = 1;
            var entryExits = 1;
            var entryDoors = true;
            if (parts.Length >= 7 &&
                (!int.TryParse(parts[5], out entryEntrances) || !int.TryParse(parts[6], out entryExits)))
            {
                Console.Error.WriteLine($"{flagName} entry '{entry}': entrances/exits must be integers — skipped");
                n++;
                continue;
            }

            if (parts.Length == 8)
            {
                if (string.Equals(parts[7], "door", StringComparison.OrdinalIgnoreCase)) entryDoors = true;
                else if (string.Equals(parts[7], "plac", StringComparison.OrdinalIgnoreCase)) entryDoors = false;
                else
                {
                    Console.Error.WriteLine($"{flagName} entry '{entry}': doors segment must be 'door' or 'plac' — skipped");
                    n++;
                    continue;
                }
            }

            var composition = ResolveComposition(themes, tilesetProfiles, layoutProfiles, themeKey, tilesetOverride, layoutOverride);
            if (composition == null)
            {
                n++;
                continue;
            }

            var resref = UniqueResref($"{resrefPrefix}{n}_{entrySeed}", usedResrefs);
            var display = ComposeDisplayName(composition.Content.DisplayName, composition.Tileset.DisplayName, composition.Layout.DisplayName, entrySeed);
            specs.Add(new AreaSpec(resref, display, composition, entrySeed, entrySize, entryEntrances, entryExits, entryDoors));
            n++;
        }
    }

    // --areas-file entries carry the full effective MacroLayoutParameters already (post composition,
    // post any Content Builder Advanced-settings overrides), so unlike ParseAreaSpecs this uses them
    // VERBATIM instead of recomposing from the theme/tileset/layout keys — those keys only resolve
    // which tileset .set/placeholder/lighting and content package to realize the snapshot against.
    void ParseAreaFileEntries(string path, string resrefPrefix)
    {
        List<AreaBatchFileEntry> entries;
        try
        {
            entries = AreaBatchFile.Deserialize(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"--areas-file '{path}': failed to read/parse — {ex.Message}");
            return;
        }

        var n = 1;
        foreach (var entry in entries)
        {
            var composition = ResolveComposition(themes, tilesetProfiles, layoutProfiles, entry.ThemeKey, entry.TilesetKey, entry.LayoutKey);
            if (composition == null)
            {
                n++;
                continue;
            }

            var resref = UniqueResref(
                !string.IsNullOrEmpty(entry.Resref) ? entry.Resref : $"{resrefPrefix}{n}_{entry.Seed}",
                usedResrefs);
            var display = ComposeDisplayName(composition.Content.DisplayName, composition.Tileset.DisplayName, composition.Layout.DisplayName, entry.Seed);
            specs.Add(new AreaSpec(resref, display, composition, entry.Seed, entry.Size,
                entry.Parameters.EntranceCount, entry.Parameters.ExitCount, entry.Parameters.DoorTransitions,
                entry.Parameters));
            n++;
        }
    }

    if (areasArg != null)
    {
        ParseAreaSpecs(areasArg, "--areas", "pga");
    }

    if (areasFileArg != null)
    {
        ParseAreaFileEntries(areasFileArg, "pgb");
    }

    if (areasArg == null && areasFileArg == null)
    {
        foreach (var theme in themes.OrderBy(t => t.Key))
        {
            var composition = ResolveComposition(themes, tilesetProfiles, layoutProfiles, theme.Key, null, null);
            if (composition == null)
                continue;

            foreach (var seed in seeds)
            {
                var resref = UniqueResref($"pg_{TwoLetters(theme.Key)}_{seed}", usedResrefs);
                var display = ComposeDisplayName(composition.Content.DisplayName, composition.Tileset.DisplayName, composition.Layout.DisplayName, seed);
                specs.Add(new AreaSpec(resref, display, composition, seed, size));
            }
        }

        if (matrix)
        {
            const int matrixSeed = 4242;
            // Palette-variant profiles (e.g. "crypt_grey", "minescaverns_desert" -- same TilesetResref
            // as an already-matrixed entry, different terrain composition; see
            // DungeonTilesetProfile.IsPaletteVariant) are excluded from the full tileset x layout
            // cross-product here to keep the review module's area count from growing every time a new
            // palette is onboarded to close a tile-coverage census exemption. Each variant instead gets
            // exactly one showcase area, composed via --extra-areas (e.g.
            // "minecave:crypt_grey:halls:5001:20") -- see the base-game tileset census work.
            foreach (var tilesetEntry in tilesetProfiles.OrderBy(t => t.Key))
            foreach (var layoutEntry in layoutProfiles.OrderBy(l => l.Key))
            {
                if (tilesetEntry.Value.IsPaletteVariant) continue;

                // Content is irrelevant offline — no creatures/loot/exit/treasure are emitted by
                // this tool, so matrix compositions carry no theme.
                var composition = new DungeonComposition
                {
                    Content = null,
                    Tileset = tilesetEntry.Value,
                    Layout = layoutEntry.Value
                };

                var resref = UniqueResref($"pgm_{TwoLetters(tilesetEntry.Key)}{TwoLetters(layoutEntry.Key)}_{matrixSeed}", usedResrefs);
                var display = $"Procgen Matrix {tilesetEntry.Value.DisplayName}/{layoutEntry.Value.DisplayName} ({matrixSeed})";
                specs.Add(new AreaSpec(resref, display, composition, matrixSeed, size));
            }
        }
    }

    if (extraAreasArg != null)
    {
        ParseAreaSpecs(extraAreasArg, "--extra-areas", "pgx");
    }

    if (specs.Count == 0)
    {
        Console.Error.WriteLine("no areas requested");
        return 1;
    }

    var modelCache = new Dictionary<string, TilesetModel>();
    var areas = new List<(string Resref, float EntryX, float EntryY)>();

    foreach (var spec in specs)
    {
        var tileset = spec.Composition.Tileset;

        if (!modelCache.TryGetValue(tileset.TilesetResref, out var model))
        {
            var setPath = TilesetSetSource.FindSetFilePath(root, tileset.TilesetResref);
            if (setPath == null)
            {
                Console.Error.WriteLine($"{spec.Resref}: tileset '{tileset.TilesetResref}' has no .set under SWLOR_Haks or basegame_sets — skipped");
                continue;
            }

            model = TilesetSetParser.Parse(tileset.TilesetResref, File.ReadAllText(setPath));
            modelCache[tileset.TilesetResref] = model;
        }

        // A --areas-file entry's OverrideParameters may have a different Style/CorridorCrosserType
        // than the resolved layout profile's own Template (Content Builder's Style knob is
        // overridable), so the size floor / Alley check must key off the EFFECTIVE style, not
        // assume the profile's own default still applies.
        var effectiveStyle = spec.OverrideParameters?.Style ?? spec.Composition.Layout.Template.Style;
        var effectiveCrosserType = spec.OverrideParameters?.CorridorCrosserType ?? spec.Composition.Layout.Template.CorridorCrosserType;

        // Sizes below the layout style's empirically measured floor fail generation structurally
        // (see LayoutStyleSizeFloor); clamp up with a note instead of burning retries and failing.
        var sizeFloor = LayoutStyleSizeFloor.For(effectiveStyle);
        var effectiveSize = spec.Size;
        if (effectiveSize < sizeFloor)
        {
            Console.WriteLine($"{spec.Resref}: size {spec.Size} is below the {effectiveStyle} floor of {sizeFloor} — clamped to {sizeFloor}");
            effectiveSize = sizeFloor;
        }

        // A layout carving Alley corridors needs the full Alley tile-SHAPE inventory (every shape
        // TunnelVocabularyCheck verifies, not just the crosser name) or MacroLayoutGenerator downgrades
        // CorridorCrosserType from Alley to Corridor before dispatch — at that point the Streets
        // composition's remaining parameters (Tunnel mode, Corridor crosser type) are identical to the
        // equivalent Complex-profile composition for the SAME tileset, so whatever Complex would itself
        // produce (a real Corridor tunnel, or a further OpenLane downgrade if Corridor is ALSO
        // incomplete for this tileset — see the second MacroLayoutGenerator downgrade check) is exactly
        // what Streets produces too. Either way it is a duplicate of an area the matrix already emits
        // under that tileset's Complex entry, so skip instead of emitting redundancy — this must NOT
        // additionally require Corridor to be complete (an earlier version of this check did, which
        // silently regenerated Barrows' Streets/OpenLane result as a duplicate of its own Complex/
        // OpenLane downgrade). Ruins (tdr01) is the motivating case: it declares an "Alley" crosser (so
        // a bare name check passes) but has no side-open boundary tile carrying a lone Alley edge, so
        // every Alley tunnel port fails resolution outright ("No matching tile ... Right=Alley"); the
        // engine's own downgrade lands on Corridor (verified complete for tdr01).
        var effectiveOpenTerrain = string.IsNullOrEmpty(tileset.PrimaryOpenTerrain) ? model.FloorTerrain : tileset.PrimaryOpenTerrain;
        if (effectiveCrosserType == CorridorCrosserType.Alley &&
            !TunnelVocabularyCheck.SupportsTunnels(model, effectiveOpenTerrain, tileset.SecondaryOpenTerrain, model.DefaultTerrain, CorridorCrosserType.Alley))
        {
            Console.WriteLine(
                $"{spec.Resref}: layout '{spec.Composition.Layout.DisplayName}' needs the Alley tile-shape inventory, which '{tileset.TilesetResref}' lacks — skipped (would duplicate the Complex-profile pairing for this tileset)");
            continue;
        }

        var placeholderAre = Path.Combine(root, "Module", "are", tileset.PlaceholderResref + ".are.json");
        var placeholderGit = Path.Combine(root, "Module", "git", tileset.PlaceholderResref + ".git.json");
        if (!File.Exists(placeholderAre) || !File.Exists(placeholderGit))
        {
            Console.Error.WriteLine($"{spec.Resref}: placeholder '{tileset.PlaceholderResref}' module JSON missing — skipped");
            continue;
        }

        // --areas-file entries already carry the full effective parameters (post composition, post
        // any Content Builder override) and are used verbatim; every other spec kind composes fresh
        // via DungeonComposition.BuildLayoutParameters and layers its entrance/exit/door segment on
        // top, exactly as before. Either way, LayoutSolver.Solve is the same shared seed-derived
        // retry loop SWLOR.ContentBuilder's GenerationEngine uses — the two tools can no longer
        // independently drift out of parity with each other.
        var baseParameters = spec.OverrideParameters?.Clone() ?? spec.Composition.BuildLayoutParameters();
        if (spec.OverrideParameters == null)
        {
            baseParameters.EntranceCount = spec.Entrances;
            baseParameters.ExitCount = spec.Exits;
            baseParameters.DoorTransitions = spec.DoorTransitions;
        }

        var solved = LayoutSolver.Solve(baseParameters, model, effectiveSize, effectiveSize, spec.Seed, spec.Composition.Tileset.PrimaryOpenTerrain);
        if (!solved.Success)
        {
            Console.Error.WriteLine($"{spec.Resref} seed {spec.Seed}: generation failed — skipped ({solved.FailureReason})");
            continue;
        }

        var layout = solved.Resolved;
        EmitArea(layout, tileset, spec.Resref, spec.DisplayName, placeholderAre, placeholderGit, stage);

        var entrance = layout.Rooms.First(r => r.Role == RoomRole.Entrance);
        areas.Add((spec.Resref, entrance.CenterTile.X * 10f + 5f, entrance.CenterTile.Y * 10f + 5f));
        Console.WriteLine($"area: {spec.Resref}  \"{spec.DisplayName}\"");
    }

    if (areas.Count == 0)
    {
        Console.Error.WriteLine("no areas generated");
        return 1;
    }

    EmitModuleIfo(root, areas, stage);
    ConvertJsonToGff(stage, gffTool);
    PackModule(stage, erfTool, outPath);

    Console.WriteLine($"packed: {outPath} ({new FileInfo(outPath).Length / 1024} KB, {areas.Count} areas)");
    return 0;
}
finally
{
    Directory.Delete(stage, true);
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            return directory.FullName;
        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the repository root (SWLOR.Game.Server.sln).");
}

/// <summary>
/// Discovers every non-abstract implementation of TInterface in this assembly and merges their
/// build-dictionaries, mirroring the DungeonContentPlacer/DungeonDefinitionBuilder discovery
/// convention (IDungeonListDefinition, IDungeonTilesetProfileListDefinition, IDungeonLayoutProfileListDefinition).
/// </summary>
static Dictionary<string, TValue> Discover<TInterface, TValue>(Func<TInterface, Dictionary<string, TValue>> build)
{
    var result = new Dictionary<string, TValue>();
    var types = typeof(TInterface).Assembly.GetTypes()
        .Where(t => typeof(TInterface).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    foreach (var type in types)
    {
        var instance = (TInterface)Activator.CreateInstance(type);
        foreach (var (key, value) in build(instance))
            result[key] = value;
    }

    return result;
}

/// <summary>
/// Resolves a (content, tileset, layout) composition exactly like DungeonContentPlacer.GetComposition:
/// the theme supplies content and its default profiles; either profile can be overridden per request.
/// </summary>
static DungeonComposition ResolveComposition(
    Dictionary<string, DungeonDetail> themes,
    Dictionary<string, DungeonTilesetProfile> tilesetProfiles,
    Dictionary<string, DungeonLayoutProfile> layoutProfiles,
    string themeKey, string tilesetProfileKeyOverride, string layoutProfileKeyOverride)
{
    if (!themes.TryGetValue(themeKey, out var content))
    {
        Console.Error.WriteLine($"unknown theme '{themeKey}' — skipped");
        return null;
    }

    var tilesetKey = string.IsNullOrEmpty(tilesetProfileKeyOverride) ? content.TilesetProfileKey : tilesetProfileKeyOverride;
    var layoutKey = string.IsNullOrEmpty(layoutProfileKeyOverride) ? content.LayoutProfileKey : layoutProfileKeyOverride;

    if (!tilesetProfiles.TryGetValue(tilesetKey, out var tileset))
    {
        Console.Error.WriteLine($"theme '{themeKey}' references unknown tileset profile '{tilesetKey}' — skipped");
        return null;
    }

    if (!layoutProfiles.TryGetValue(layoutKey, out var layout))
    {
        Console.Error.WriteLine($"theme '{themeKey}' references unknown layout profile '{layoutKey}' — skipped");
        return null;
    }

    return new DungeonComposition
    {
        Content = content,
        Tileset = tileset,
        Layout = layout
    };
}

static string ComposeDisplayName(string themeDisplayName, string tilesetDisplayName, string layoutDisplayName, int seed)
{
    return $"Procgen {themeDisplayName} [{tilesetDisplayName}/{layoutDisplayName}] (seed {seed})";
}

static string TwoLetters(string key)
{
    return new string(key.Where(char.IsLetterOrDigit).Take(2).ToArray()).ToLowerInvariant();
}

/// <summary>Ensures resrefs stay unique and within NWN's 16-character resource limit.</summary>
static string UniqueResref(string baseResref, HashSet<string> used)
{
    var candidate = baseResref.Length > 16 ? baseResref[..16] : baseResref;
    var suffix = 2;
    while (!used.Add(candidate))
    {
        var suffixText = suffix.ToString();
        var maxBaseLength = 16 - suffixText.Length;
        candidate = (baseResref.Length > maxBaseLength ? baseResref[..maxBaseLength] : baseResref) + suffixText;
        suffix++;
    }

    return candidate;
}

static void EmitArea(ResolvedLayout layout, DungeonTilesetProfile tileset, string resref, string display,
    string placeholderArePath, string placeholderGitPath, string stage)
{
    var lighting = tileset.Lighting;
    var tiles = string.Join(",\n", layout.Tiles.Select(t => TileEntry(t.TileId, t.Orientation, t.Height,
        lighting.MainLight1, lighting.MainLight2, lighting.SourceLight1, lighting.SourceLight2)));

    var are = File.ReadAllText(placeholderArePath);
    are = are.Replace($"\"{tileset.PlaceholderResref}\"", $"\"{resref}\"");
    are = System.Text.RegularExpressions.Regex.Replace(
        are, "\"0\": \"Generated [^\"]*Placeholder\"", $"\"0\": \"{display}\"");
    are = ReplaceFirstIntField(are, "Height", layout.Height);
    are = ReplaceFirstIntField(are, "Width", layout.Width);

    // The placeholder area carries its own Tileset resref (gen_placeholder1 is a tdt01 area). At
    // runtime the NWNX override sets the tileset explicitly, but an emitted .are must carry the
    // REAL tileset or the toolset indexes the tile list against the wrong .set — e.g. Steamworks
    // tile IDs (up to 178) read against tdt01's 159 tiles crash with "List index out of bounds".
    are = System.Text.RegularExpressions.Regex.Replace(
        are,
        "(\"Tileset\"\\s*:\\s*\\{[^}]*\"value\"\\s*:\\s*\")[^\"]*(\")",
        $"${{1}}{tileset.TilesetResref}$2");

    var start = are.IndexOf("\"Tile_List\"", StringComparison.Ordinal);
    var open = are.IndexOf('[', start);
    var close = are.IndexOf(']', open);
    are = are[..(open + 1)] + "\n" + tiles + "\n    " + are[close..];

    File.WriteAllText(Path.Combine(stage, resref + ".are.json"), are);

    var git = File.ReadAllText(placeholderGitPath);
    var waypoints = BuildWaypointEntries(layout);
    if (!string.IsNullOrEmpty(waypoints))
    {
        var wpStart = git.IndexOf("\"WaypointList\"", StringComparison.Ordinal);
        var wpOpen = git.IndexOf('[', wpStart);
        var wpClose = git.IndexOf(']', wpOpen);
        git = git[..(wpOpen + 1)] + "\n" + waypoints + "\n    " + git[wpClose..];
    }

    var doors = BuildDoorEntries(layout);
    if (!string.IsNullOrEmpty(doors))
    {
        var doorStart = git.IndexOf("\"Door List\"", StringComparison.Ordinal);
        var doorOpen = git.IndexOf('[', doorStart);
        var doorClose = git.IndexOf(']', doorOpen);
        git = git[..(doorOpen + 1)] + "\n" + doors + "\n  " + git[doorClose..];
    }

    File.WriteAllText(Path.Combine(stage, resref + ".git.json"), git);

    // sanity: must remain valid JSON
    _ = JsonNode.Parse(File.ReadAllText(Path.Combine(stage, resref + ".are.json")));
    _ = JsonNode.Parse(File.ReadAllText(Path.Combine(stage, resref + ".git.json")));
}

/// <summary>
/// Builds one waypoint GFF-JSON struct per entrance/exit transition point, so transitions are
/// visible when reviewing a generated area in the toolset. Struct shape/field set mirrors an
/// existing hand-built waypoint instance (see Module/git/veles_sewers.git.json), __struct_id 5.
/// Door-bearing transitions (Door/GroupExit) position the waypoint 2m in front of the door — the
/// anchor tile's exact center may sit inside the decorative geometry many open-floor tile variants
/// carry (mounds, pipes), which buried waypoints under set dressing in toolset review. Placeable
/// transitions keep the tile center. Named "PG Entrance N"/"PG Exit N" with tags PG_ENT_N/PG_EXIT_N,
/// numbered separately per kind in transition order (the first Entrance is always the primary
/// arrival anchor).
/// </summary>
static string BuildWaypointEntries(ResolvedLayout layout)
{
    var entranceCount = 0;
    var exitCount = 0;
    var entries = new List<string>();

    foreach (var transition in layout.Transitions)
    {
        var isEntrance = transition.Kind == TransitionKind.Entrance;
        var index = isEntrance ? ++entranceCount : ++exitCount;
        var label = isEntrance ? "Entrance" : "Exit";
        var tag = (isEntrance ? "PG_ENT_" : "PG_EXIT_") + index;
        var name = $"PG {label} {index}";

        var anchorX = transition.Tile.X * 10f + 5f;
        var anchorY = transition.Tile.Y * 10f + 5f;
        var x = anchorX;
        var y = anchorY;

        if (transition.Style != TransitionStyle.Placeable)
        {
            // Step from the door position toward the open anchor cell so the waypoint sits just
            // inside walkable floor, directly in front of the door.
            var dx = anchorX - transition.DoorX;
            var dy = anchorY - transition.DoorY;
            var length = MathF.Sqrt(dx * dx + dy * dy);
            if (length > 0.01f)
            {
                x = transition.DoorX + dx / length * 2f;
                y = transition.DoorY + dy / length * 2f;
            }
        }

        entries.Add(WaypointEntry(name, tag, x, y));
    }

    return string.Join(",\n", entries);
}

static string WaypointEntry(string name, string tag, float x, float y)
{
    return $$"""
          {
            "__struct_id": 5,
            "Appearance": {
              "type": "byte",
              "value": 1
            },
            "Description": {
              "type": "cexolocstring",
              "value": {}
            },
            "HasMapNote": {
              "type": "byte",
              "value": 0
            },
            "LinkedTo": {
              "type": "cexostring",
              "value": ""
            },
            "LocalizedName": {
              "type": "cexolocstring",
              "value": {
                "0": "{{name}}"
              }
            },
            "MapNote": {
              "type": "cexolocstring",
              "value": {}
            },
            "MapNoteEnabled": {
              "type": "byte",
              "value": 0
            },
            "Tag": {
              "type": "cexostring",
              "value": "{{tag}}"
            },
            "TemplateResRef": {
              "type": "resref",
              "value": "nw_waypoint001"
            },
            "XOrientation": {
              "type": "float",
              "value": 0.0
            },
            "XPosition": {
              "type": "float",
              "value": {{FormatFloat(x)}}
            },
            "YOrientation": {
              "type": "float",
              "value": 1.0
            },
            "YPosition": {
              "type": "float",
              "value": {{FormatFloat(y)}}
            },
            "ZPosition": {
              "type": "float",
              "value": 0.0
            }
          }
    """;
}

/// <summary>
/// Builds one Door GFF-JSON struct per Door-style or GroupExit-style transition point (see
/// TileDoorPlanner / GroupExitPlanner / TransitionPoint.Style), in addition to that transition's
/// waypoint. Struct shape/field set mirrors
/// a hand-built "nw_door_fancy" generic-door instance (Module/git/dan_battlemon.git.json,
/// __struct_id 8) — the most common plain generic door already used across the SWLOR module, fitting
/// any Type=0 generic door slot. Position/bearing come straight from the planner's world-transform
/// (TransitionPoint.DoorX/Y/Z/DoorOrientation, degrees); Bearing is stored in radians, matching NWN's
/// .git convention (confirmed against hand-built door Bearing fields, e.g. veles_sewers).
/// </summary>
static string BuildDoorEntries(ResolvedLayout layout)
{
    var entranceCount = 0;
    var exitCount = 0;
    var entries = new List<string>();

    foreach (var transition in layout.Transitions)
    {
        var isEntrance = transition.Kind == TransitionKind.Entrance;
        var index = isEntrance ? ++entranceCount : ++exitCount;
        if (transition.Style is not (TransitionStyle.Door or TransitionStyle.GroupExit))
            continue;

        var label = isEntrance ? "Entrance" : "Exit";
        var tag = (isEntrance ? "PG_DOOR_ENT_" : "PG_DOOR_EXIT_") + index;
        var bearingRadians = transition.DoorOrientation * Math.PI / 180.0;

        entries.Add(DoorEntry(tag, label, transition.DoorX, transition.DoorY, transition.DoorZ, bearingRadians));
    }

    return string.Join(",\n", entries);
}

static string DoorEntry(string tag, string locName, float x, float y, float z, double bearingRadians)
{
    return $$"""
          {
            "__struct_id": 8,
            "AnimationState": {
              "type": "byte",
              "value": 0
            },
            "Appearance": {
              "type": "dword",
              "value": 0
            },
            "AutoRemoveKey": {
              "type": "byte",
              "value": 0
            },
            "Bearing": {
              "type": "float",
              "value": {{FormatFloat(bearingRadians)}}
            },
            "CloseLockDC": {
              "type": "byte",
              "value": 0
            },
            "Conversation": {
              "type": "resref",
              "value": ""
            },
            "CurrentHP": {
              "type": "short",
              "value": 15
            },
            "Description": {
              "type": "cexolocstring",
              "value": {}
            },
            "DisarmDC": {
              "type": "byte",
              "value": 0
            },
            "Faction": {
              "type": "dword",
              "value": 1
            },
            "Fort": {
              "type": "byte",
              "value": 0
            },
            "GenericType_New": {
              "type": "dword",
              "value": 51
            },
            "Hardness": {
              "type": "byte",
              "value": 5
            },
            "HP": {
              "type": "short",
              "value": 15
            },
            "Interruptable": {
              "type": "byte",
              "value": 1
            },
            "KeyName": {
              "type": "cexostring",
              "value": ""
            },
            "KeyRequired": {
              "type": "byte",
              "value": 0
            },
            "LinkedTo": {
              "type": "cexostring",
              "value": ""
            },
            "LinkedToFlags": {
              "type": "byte",
              "value": 0
            },
            "LoadScreenID": {
              "type": "word",
              "value": 0
            },
            "Lockable": {
              "type": "byte",
              "value": 0
            },
            "Locked": {
              "type": "byte",
              "value": 0
            },
            "LocName": {
              "type": "cexolocstring",
              "value": {
                "0": "{{locName}}"
              }
            },
            "OnClick": {
              "type": "resref",
              "value": ""
            },
            "OnClosed": {
              "type": "resref",
              "value": ""
            },
            "OnDamaged": {
              "type": "resref",
              "value": ""
            },
            "OnDeath": {
              "type": "resref",
              "value": "x2_door_death"
            },
            "OnDisarm": {
              "type": "resref",
              "value": ""
            },
            "OnFailToOpen": {
              "type": "resref",
              "value": ""
            },
            "OnHeartbeat": {
              "type": "resref",
              "value": ""
            },
            "OnLock": {
              "type": "resref",
              "value": ""
            },
            "OnMeleeAttacked": {
              "type": "resref",
              "value": ""
            },
            "OnOpen": {
              "type": "resref",
              "value": ""
            },
            "OnSpellCastAt": {
              "type": "resref",
              "value": ""
            },
            "OnTrapTriggered": {
              "type": "resref",
              "value": ""
            },
            "OnUnlock": {
              "type": "resref",
              "value": ""
            },
            "OnUserDefined": {
              "type": "resref",
              "value": ""
            },
            "OpenLockDC": {
              "type": "byte",
              "value": 0
            },
            "Plot": {
              "type": "byte",
              "value": 0
            },
            "PortraitId": {
              "type": "word",
              "value": 0
            },
            "Ref": {
              "type": "byte",
              "value": 0
            },
            "Tag": {
              "type": "cexostring",
              "value": "{{tag}}"
            },
            "TemplateResRef": {
              "type": "resref",
              "value": "nw_door_fancy"
            },
            "TrapDetectable": {
              "type": "byte",
              "value": 0
            },
            "TrapDetectDC": {
              "type": "byte",
              "value": 0
            },
            "TrapDisarmable": {
              "type": "byte",
              "value": 0
            },
            "TrapFlag": {
              "type": "byte",
              "value": 0
            },
            "TrapOneShot": {
              "type": "byte",
              "value": 0
            },
            "TrapType": {
              "type": "byte",
              "value": 0
            },
            "Will": {
              "type": "byte",
              "value": 0
            },
            "X": {
              "type": "float",
              "value": {{FormatFloat(x)}}
            },
            "Y": {
              "type": "float",
              "value": {{FormatFloat(y)}}
            },
            "Z": {
              "type": "float",
              "value": {{FormatFloat(z)}}
            }
          }
    """;
}

/// <summary>nwn_gff requires float lexemes ("5.0"); a bare integer-valued double round-trips as "5".</summary>
static string FormatFloat(double value)
{
    var text = value.ToString("0.0###############", System.Globalization.CultureInfo.InvariantCulture);
    return text;
}

static string ReplaceFirstIntField(string json, string field, int value)
{
    return new System.Text.RegularExpressions.Regex(
            $"(\"{field}\": \\{{\\s*\"type\": \"int\",\\s*\"value\": )\\d+")
        .Replace(json, "${1}" + value, 1);
}

static string TileEntry(int tileId, int orientation, int tileHeight, int ml1, int ml2, int sl1, int sl2)
{
    return $$"""
          {
            "__struct_id": 1,
            "Tile_AnimLoop1": {
              "type": "byte",
              "value": 1
            },
            "Tile_AnimLoop2": {
              "type": "byte",
              "value": 1
            },
            "Tile_AnimLoop3": {
              "type": "byte",
              "value": 1
            },
            "Tile_Height": {
              "type": "int",
              "value": {{tileHeight}}
            },
            "Tile_ID": {
              "type": "int",
              "value": {{tileId}}
            },
            "Tile_MainLight1": {
              "type": "byte",
              "value": {{ml1}}
            },
            "Tile_MainLight2": {
              "type": "byte",
              "value": {{ml2}}
            },
            "Tile_Orientation": {
              "type": "int",
              "value": {{orientation}}
            },
            "Tile_SrcLight1": {
              "type": "byte",
              "value": {{sl1}}
            },
            "Tile_SrcLight2": {
              "type": "byte",
              "value": {{sl2}}
            }
          }
    """;
}

static void EmitModuleIfo(string root, List<(string Resref, float EntryX, float EntryY)> areas, string stage)
{
    var ifo = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Module", "ifo", "module.ifo.json")));

    var list = new JsonArray();
    foreach (var (resref, _, _) in areas)
    {
        list.Add(new JsonObject
        {
            ["__struct_id"] = 6,
            ["Area_Name"] = new JsonObject { ["type"] = "resref", ["value"] = resref }
        });
    }

    ifo["Mod_Area_list"]["value"] = list;
    ifo["Mod_Entry_Area"] = new JsonObject { ["type"] = "resref", ["value"] = areas[0].Resref };
    ifo["Mod_Entry_X"] = new JsonObject { ["type"] = "float", ["value"] = areas[0].EntryX };
    ifo["Mod_Entry_Y"] = new JsonObject { ["type"] = "float", ["value"] = areas[0].EntryY };
    ifo["Mod_Entry_Z"] = new JsonObject { ["type"] = "float", ["value"] = 1.0 };
    ifo["Mod_Entry_Dir_X"] = new JsonObject { ["type"] = "float", ["value"] = 0.0 };
    ifo["Mod_Entry_Dir_Y"] = new JsonObject { ["type"] = "float", ["value"] = 1.0 };
    if (ifo["Mod_Name"] is JsonObject name)
        name["value"] = new JsonObject { ["0"] = "SWLOR Procgen Review" };

    var json = ifo.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    // nwn_gff requires float lexemes ("0.0"); System.Text.Json serializes whole doubles as "0".
    // Restore the decimal point on every float-typed field that lost it in the round-trip.
    json = System.Text.RegularExpressions.Regex.Replace(
        json,
        "(\"type\": \"float\",\\s*\"value\": -?\\d+)(?=\\s*[,\\}])",
        "$1.0");

    File.WriteAllText(Path.Combine(stage, "module.ifo.json"), json);
}

static void ConvertJsonToGff(string stage, string gffTool)
{
    foreach (var jsonFile in Directory.GetFiles(stage, "*.json"))
    {
        var gffFile = jsonFile[..^5]; // strip .json -> pg_x.are / module.ifo
        Run(gffTool, $"-i \"{jsonFile}\" -o \"{gffFile}\"");
        File.Delete(jsonFile);
    }
}

static void PackModule(string stage, string erfTool, string outPath)
{
    if (File.Exists(outPath))
        File.Delete(outPath);

    var entries = string.Join(" ", Directory.GetFiles(stage).Select(f => $"\"{f}\""));
    Run(erfTool, $"-e MOD -c -f \"{outPath}\" {entries}");
}

static void Run(string exe, string arguments)
{
    var psi = new ProcessStartInfo(exe, arguments)
    {
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    using var proc = Process.Start(psi);
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    if (proc.ExitCode != 0)
        throw new InvalidOperationException($"{Path.GetFileName(exe)} failed: {stderr}");
}

/// <summary>
/// OverrideParameters is non-null only for --areas-file entries: the full effective
/// MacroLayoutParameters snapshot, used verbatim instead of Composition.BuildLayoutParameters() +
/// Entrances/Exits/DoorTransitions (see the main generation loop). Entrances/Exits/DoorTransitions
/// stay meaningful even for those entries (mirrored from the snapshot) for logging/display symmetry
/// with the string-spec kinds.
/// </summary>
record AreaSpec(string Resref, string DisplayName, DungeonComposition Composition, int Seed, int Size, int Entrances = 1, int Exits = 1, bool DoorTransitions = true, MacroLayoutParameters OverrideParameters = null);
