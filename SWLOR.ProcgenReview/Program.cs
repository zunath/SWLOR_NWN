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
// --areas: comma-separated batch entries, either "theme:tileset:layout:seed:size" (entrances/exits
// default to 1/1) or the 7-segment "theme:tileset:layout:seed:size:entrances:exits" form. tileset/
// layout may be left empty to use the theme's own defaults. When given, ONLY these areas are
// generated — no default set, no matrix.
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

    if (areasArg != null)
    {
        var n = 1;
        foreach (var entry in areasArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length != 5 && parts.Length != 7)
            {
                Console.Error.WriteLine($"--areas entry '{entry}': expected theme:tileset:layout:seed:size or theme:tileset:layout:seed:size:entrances:exits — skipped");
                continue;
            }

            var themeKey = parts[0];
            var tilesetOverride = parts[1];
            var layoutOverride = parts[2];
            if (!int.TryParse(parts[3], out var entrySeed) || !int.TryParse(parts[4], out var entrySize))
            {
                Console.Error.WriteLine($"--areas entry '{entry}': seed/size must be integers — skipped");
                continue;
            }

            var entryEntrances = 1;
            var entryExits = 1;
            if (parts.Length == 7 &&
                (!int.TryParse(parts[5], out entryEntrances) || !int.TryParse(parts[6], out entryExits)))
            {
                Console.Error.WriteLine($"--areas entry '{entry}': entrances/exits must be integers — skipped");
                n++;
                continue;
            }

            var composition = ResolveComposition(themes, tilesetProfiles, layoutProfiles, themeKey, tilesetOverride, layoutOverride);
            if (composition == null)
            {
                n++;
                continue;
            }

            var resref = UniqueResref($"pga{n}_{entrySeed}", usedResrefs);
            var display = ComposeDisplayName(composition.Content.DisplayName, composition.Tileset.DisplayName, composition.Layout.DisplayName, entrySeed);
            specs.Add(new AreaSpec(resref, display, composition, entrySeed, entrySize, entryEntrances, entryExits));
            n++;
        }
    }
    else
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
            foreach (var tilesetEntry in tilesetProfiles.OrderBy(t => t.Key))
            foreach (var layoutEntry in layoutProfiles.OrderBy(l => l.Key))
            {
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
            var setPath = Directory
                .EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), tileset.TilesetResref + ".set", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (setPath == null)
            {
                Console.Error.WriteLine($"{spec.Resref}: tileset '{tileset.TilesetResref}' has no .set under SWLOR_Haks — skipped");
                continue;
            }

            model = TilesetSetParser.Parse(tileset.TilesetResref, File.ReadAllText(setPath));
            modelCache[tileset.TilesetResref] = model;
        }

        var placeholderAre = Path.Combine(root, "Module", "are", tileset.PlaceholderResref + ".are.json");
        var placeholderGit = Path.Combine(root, "Module", "git", tileset.PlaceholderResref + ".git.json");
        if (!File.Exists(placeholderAre) || !File.Exists(placeholderGit))
        {
            Console.Error.WriteLine($"{spec.Resref}: placeholder '{tileset.PlaceholderResref}' module JSON missing — skipped");
            continue;
        }

        var baseParameters = spec.Composition.BuildLayoutParameters();
        baseParameters.EntranceCount = spec.Entrances;
        baseParameters.ExitCount = spec.Exits;
        var layout = Generate(model, baseParameters, spec.Seed, spec.Size);
        if (layout == null)
        {
            Console.Error.WriteLine($"{spec.Resref} seed {spec.Seed}: generation failed — skipped");
            continue;
        }

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

static ResolvedLayout Generate(TilesetModel model, MacroLayoutParameters baseParameters, int seed, int size)
{
    // Mirrors AreaGeneration.Generate's seed-derived retry (no path validation offline — that
    // needs the engine; the review module is for visual inspection, not traversal QA).
    for (var attempt = 0; attempt < 6; attempt++)
    {
        var rng = new Random(seed + attempt);
        var parameters = baseParameters.Clone();
        parameters.Width = size;
        parameters.Height = size;
        parameters.SolidTerrain = model.DefaultTerrain;
        parameters.OpenTerrain = model.FloorTerrain;

        MacroLayout macro;
        try
        {
            macro = MacroLayoutGenerator.Generate(parameters, rng);
        }
        catch (InvalidOperationException)
        {
            continue;
        }

        macro.Seed = seed;
        if (TileResolver.TryResolve(model, macro, rng, out var resolved, out _))
            return resolved;
    }

    return null;
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
    var tiles = string.Join(",\n", layout.Tiles.Select(t => TileEntry(t.TileId, t.Orientation,
        lighting.MainLight1, lighting.MainLight2, lighting.SourceLight1, lighting.SourceLight2)));

    var are = File.ReadAllText(placeholderArePath);
    are = are.Replace($"\"{tileset.PlaceholderResref}\"", $"\"{resref}\"");
    are = System.Text.RegularExpressions.Regex.Replace(
        are, "\"0\": \"Generated [^\"]*Placeholder\"", $"\"0\": \"{display}\"");
    are = ReplaceFirstIntField(are, "Height", layout.Height);
    are = ReplaceFirstIntField(are, "Width", layout.Width);

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
    File.WriteAllText(Path.Combine(stage, resref + ".git.json"), git);

    // sanity: must remain valid JSON
    _ = JsonNode.Parse(File.ReadAllText(Path.Combine(stage, resref + ".are.json")));
    _ = JsonNode.Parse(File.ReadAllText(Path.Combine(stage, resref + ".git.json")));
}

/// <summary>
/// Builds one waypoint GFF-JSON struct per entrance/exit transition point, so transitions are
/// visible when reviewing a generated area in the toolset. Struct shape/field set mirrors an
/// existing hand-built waypoint instance (see Module/git/veles_sewers.git.json), __struct_id 5.
/// Positioned at the transition tile's center (tile*10+5), Z 0. Named "PG Entrance N"/"PG Exit N"
/// with tags PG_ENT_N/PG_EXIT_N, numbered separately per kind in transition order (the first
/// Entrance is always the primary arrival anchor).
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
        var x = transition.Tile.X * 10f + 5f;
        var y = transition.Tile.Y * 10f + 5f;

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

static string TileEntry(int tileId, int orientation, int ml1, int ml2, int sl1, int sl2)
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
              "value": 0
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

record AreaSpec(string Resref, string DisplayName, DungeonComposition Composition, int Seed, int Size, int Entrances = 1, int Exits = 1);
