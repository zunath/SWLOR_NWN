using System.Diagnostics;
using System.Text.Json.Nodes;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

// Builds a standalone review module ("SWLOR Procgen Review.mod") containing offline-generated
// areas for every registered dungeon theme, using the production solver and each theme's real
// tileset/lighting/placeholder settings. The module opens in the toolset or game without the
// full SWLOR module. All paths derive from the repository root (located by walking up to the
// solution file), so the tool runs on any machine or drive layout.
//
// Usage (from anywhere inside the repo):
//   dotnet run --project tools/ProcgenReview -- [--seeds 4242,777] [--size 16] [--out <path>]
//
// Output defaults to <repoRoot>/Module/SWLOR Procgen Review.mod — point nwn.ini's MODULES
// directory at <repoRoot>/Module (the SWLOR dev convention) and the toolset sees it directly.

var seeds = new List<int> { 4242, 777 };
var size = 16;
string outPath = null;

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
    var themes = DiscoverThemes();
    if (themes.Count == 0)
    {
        Console.Error.WriteLine("no dungeon themes discovered");
        return 1;
    }

    var areas = new List<(string Resref, float EntryX, float EntryY)>();
    var usedResrefs = new HashSet<string>();

    foreach (var detail in themes)
    {
        var setPath = Directory
            .EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), detail.TilesetResref + ".set", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (setPath == null)
        {
            Console.Error.WriteLine($"{detail.ThemeKey}: tileset '{detail.TilesetResref}' has no .set under SWLOR_Haks — skipped");
            continue;
        }

        var placeholderAre = Path.Combine(root, "Module", "are", detail.PlaceholderResref + ".are.json");
        var placeholderGit = Path.Combine(root, "Module", "git", detail.PlaceholderResref + ".git.json");
        if (!File.Exists(placeholderAre) || !File.Exists(placeholderGit))
        {
            Console.Error.WriteLine($"{detail.ThemeKey}: placeholder '{detail.PlaceholderResref}' module JSON missing — skipped");
            continue;
        }

        var model = TilesetSetParser.Parse(detail.TilesetResref, File.ReadAllText(setPath));

        foreach (var seed in seeds)
        {
            var layout = Generate(model, seed, size);
            if (layout == null)
            {
                Console.Error.WriteLine($"{detail.ThemeKey} seed {seed}: generation failed — skipped");
                continue;
            }

            var resref = MakeResref(detail.ThemeKey, seed, usedResrefs);
            var display = $"Procgen {detail.DisplayName} (seed {seed})";

            EmitArea(layout, detail, resref, display, placeholderAre, placeholderGit, stage);

            var entrance = layout.Rooms.First(r => r.Role == RoomRole.Entrance);
            areas.Add((resref, entrance.CenterTile.X * 10f + 5f, entrance.CenterTile.Y * 10f + 5f));
            Console.WriteLine($"area: {resref}  \"{display}\"");
        }
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

static List<DungeonDetail> DiscoverThemes()
{
    var themes = new List<DungeonDetail>();
    var types = typeof(IDungeonListDefinition).Assembly.GetTypes()
        .Where(t => typeof(IDungeonListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    foreach (var type in types)
    {
        var definition = (IDungeonListDefinition)Activator.CreateInstance(type);
        foreach (var (_, detail) in definition.BuildDungeons().OrderBy(d => d.Key))
            themes.Add(detail);
    }

    return themes.OrderBy(t => t.ThemeKey).ToList();
}

static ResolvedLayout Generate(TilesetModel model, int seed, int size)
{
    // Mirrors the runtime facade's seed-derived retry (no path validation offline — that
    // needs the engine; the review module is for visual inspection, not traversal QA).
    for (var attempt = 0; attempt < 6; attempt++)
    {
        var rng = new Random(seed + attempt);
        MacroLayout macro;
        try
        {
            macro = MacroLayoutGenerator.Generate(new MacroLayoutParameters
            {
                Width = size,
                Height = size,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = model.FloorTerrain,
                MinRooms = 4,
                MaxRooms = 8
            }, rng);
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

static string MakeResref(string themeKey, int seed, HashSet<string> used)
{
    var prefix = new string(themeKey.Where(char.IsLetterOrDigit).Take(2).ToArray());
    var resref = $"pg_{prefix}_{seed}";
    var suffix = 2;
    while (!used.Add(resref))
        resref = $"pg_{prefix}{suffix++}_{seed}";
    return resref;
}

static void EmitArea(ResolvedLayout layout, DungeonDetail detail, string resref, string display,
    string placeholderArePath, string placeholderGitPath, string stage)
{
    var lighting = detail.Lighting;
    var tiles = string.Join(",\n", layout.Tiles.Select(t => TileEntry(t.TileId, t.Orientation,
        lighting.MainLight1, lighting.MainLight2, lighting.SourceLight1, lighting.SourceLight2)));

    var are = File.ReadAllText(placeholderArePath);
    are = are.Replace($"\"{detail.PlaceholderResref}\"", $"\"{resref}\"");
    are = System.Text.RegularExpressions.Regex.Replace(
        are, "\"0\": \"Generated [^\"]*Placeholder\"", $"\"0\": \"{display}\"");
    are = ReplaceFirstIntField(are, "Height", layout.Height);
    are = ReplaceFirstIntField(are, "Width", layout.Width);

    var start = are.IndexOf("\"Tile_List\"", StringComparison.Ordinal);
    var open = are.IndexOf('[', start);
    var close = are.IndexOf(']', open);
    are = are[..(open + 1)] + "\n" + tiles + "\n    " + are[close..];

    File.WriteAllText(Path.Combine(stage, resref + ".are.json"), are);
    File.Copy(placeholderGitPath, Path.Combine(stage, resref + ".git.json"));

    // sanity: must remain valid JSON
    _ = JsonNode.Parse(File.ReadAllText(Path.Combine(stage, resref + ".are.json")));
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
