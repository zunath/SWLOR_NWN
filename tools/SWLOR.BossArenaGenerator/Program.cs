using System.Numerics;
using System.Text.Json;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

if (args.Length != 4 || args.Any(arg => arg.StartsWith("--", StringComparison.Ordinal)))
    throw new ArgumentException("Usage: SWLOR.BossArenaGenerator <repository> <hak-source-directory> <preview-directory> <NWN-install-directory>");
var root = Path.GetFullPath(args[0]);
var hakRoot = Path.GetFullPath(args[1]);
var previewRoot = Path.GetFullPath(args[2]);
var arenas = new[]
{
    new Arena("pw_sc_velescmd", "Viscara - Militia Command Room", "Veles Militia Annex", "tbx78", "modernfacility", 1, 91201, false,
        [new("invinc", "invincible"), new("vitrupt", "vital_rupture"), new("sysshut", "systemic_shutdown")]),
    new Arena("pw_sc_jeditrial", "Dantooine - Saber Trial Chamber", "Dantooine Jedi Enclave Trial Halls", "zin01", "cep_cityinterior", 128, 91202, false,
        [new("sabstorm", "saber_storm"), new("guardmst", "guardian_master"), new("sabcycl", "saber_cyclone")]),
    new Arena("pw_sc_korrforge", "Korriban - Champion Forge", "Korriban Forge Caverns", "ztu01", "underdark", 32, 91203, true,
        [new("absdef", "absolute_defense"), new("soulasc", "soul_ascension"), new("forcebane", "forcebane")]),
    new Arena("pw_sc_canyonpit", "Tatooine - Canyon Dueling Pit", "Anchorhead Canyon Range", "tdm01", "minescaverns_desert", 2, 91204, true,
        [new("unmovctr", "unmoving_center"), new("lastword", "last_word"), new("deadhand", "dead_mans_hand")]),
    new Arena("pw_sc_qioncore", "Hutlar - Overload Chamber", "Hutlar Qion Test Site", "tbx78", "modernfacility", 8, 91205, false,
        [new("perflurry", "perfect_flurry"), new("thermdet", "thermal_detonator"), new("overbarr", "overload_barrage")]),
    new Arena("pw_sc_sithritual", "Korriban - Final Ritual Chamber", "Korriban Sith Crypt Depths", "zid01", "drowinterior", 32, 91206, false,
        [new("lightstand", "last_stand_of_the_light"), new("darkhung", "hunger_of_the_dark"), new("eclipse", "eclipse_of_resolve")]),
    new Arena("pw_sc_repubcmd", "Viscara - Engineering Command Room", "Viscara Republic Engineering Bunker", "tjsb0", "secretbase", 1, 91207, false,
        [new("killbeacon", "killzone_beacon"), new("embunker", "emergency_bunker"), new("deccommand", "decisive_command")]),
    new Arena("pw_sc_tarnalpha", "Dathomir - Alpha Beast Hollow", "Dathomir Tarn Jungle Preserve", "ttu01", "underdark", 64, 91208, true,
        [new("apexbite", "apex_bite"), new("unbrbeast", "unbreakable_beast"), new("alpharhy", "alpha_rhythm")]),
};

using var moduleWriteLock = ModuleWriteLock.Acquire(Path.Combine(root, "Module"));
// Reject collisions across the whole batch before creating any area. Author existing arenas in
// the toolset; regeneration belongs in a separate module copy so it cannot discard builder work.
foreach (var arena in arenas)
foreach (var extension in new[] { "are", "git", "gic" })
    if (File.Exists(Path.Combine(root, "Module", extension, $"{arena.Resref}.{extension}.json")))
        throw new InvalidOperationException($"{arena.Resref} already exists. Generate into a separate module copy.");

// Read the module's ordered hak stack; unpacked sources stay read-only.
var ifo = IfoDocument.Load(Path.Combine(root, "Module", "ifo", "module.ifo.json"));
var layers = ifo.Fields.GetListOrEmpty("Mod_HakList")
    .Select(h => h.GetStringOrNull("Mod_Hak")!)
    .Where(h => !string.IsNullOrWhiteSpace(h) && Directory.Exists(Path.Combine(hakRoot, h)))
    .Select(h => new ResourceIndex.HakLayer(h, Path.Combine(hakRoot, h))).ToArray();
var game = KeyBifCatalog.Load(Path.Combine(args[3], "data"));
var resources = new ResourceIndex(game, layers);
resources.EnsureInitialized();
var tilesets = new TilesetCatalog(resources);
var workspace = new ModuleWorkspace(Path.Combine(root, "Module"), resources);
var definitions = new DefinitionCatalog();
var surface = new TwoDaService(Path.Combine(hakRoot, "sw_2da")).GetTable("surfacemat");
var meshes = new TileWalkmeshCache(resources, material => material >= 0 && material < surface.RowCount && surface.GetInt(material, "Walk") == 1);
var models = new TileModelCache(resources);
var summaries = new List<object>();
using var construction = EditScope.EnterConstruction();
Directory.CreateDirectory(previewRoot);

foreach (var arena in arenas)
{
    Console.WriteLine($"Generating {arena.Resref} ({arena.Tileset})...");
    if (!tilesets.TryGetTileset(arena.Tileset, out var definition))
        throw new InvalidOperationException($"Missing tileset {arena.Tileset}");
    var model = TilesetSetParser.FromDefinition(arena.Tileset, definition);
    var profile = definitions.TilesetProfiles[arena.Profile];
    // CEP variants use the same arena footprint, resolved against their actual SET inventory.
    profile.TilesetResref = arena.Tileset;
    var open = string.IsNullOrEmpty(profile.PrimaryOpenTerrain) ? model.FloorTerrain : profile.PrimaryOpenTerrain;
    var solid = string.IsNullOrEmpty(profile.SolidTerrainOverride) ? model.DefaultTerrain : profile.SolidTerrainOverride;
    if (arena.Tileset is "ztu01" or "ttu01") { open = "Floor"; solid = "Rock"; }
    if (arena.Tileset == "zid01") { open = "Floor2"; solid = "Wall"; }

    // The generator's normal profiles create sprawling dungeons. This authoring batch supplies
    // a compact arena macro plan to its shared tile solver, decoration planner, and area writer.
    const int width = 10, height = 10;
    var corners = new CornerTerrainGrid(width, height, solid);
    for (var x = 1; x <= 9; x++)
    for (var y = 3; y <= 9; y++)
    {
        if (arena.Cave && ((x == 1 || x == 9) && (y == 3 || y == 9))) continue;
        corners.Labels[x, y] = open;
    }
    for (var x = 4; x <= 6; x++)
    for (var y = 1; y <= 3; y++) corners.Labels[x, y] = open;
    var macro = new MacroLayout(corners)
    {
        Seed = arena.Seed, OpenTerrain = open, DoorTransitions = false, FeatureDensity = 0,
        DoorSlotCrossers = profile.DoorSlotCrossers.ToList(),
        ExcludedTiles = profile.ExcludedTiles.ToHashSet()
    };
    var entryRoom = new LayoutRoom { Id = 0, Role = RoomRole.Entrance, CenterTile = (4, 1), OpenTerrain = open };
    var bossRoom = new LayoutRoom { Id = 1, Role = RoomRole.Boss, CenterTile = (4, 6), OpenTerrain = open };
    for (var x = 0; x < width; x++)
    for (var y = 0; y < height; y++)
        if (corners.Labels[x,y] == open && corners.Labels[x+1,y] == open && corners.Labels[x,y+1] == open && corners.Labels[x+1,y+1] == open)
            (y < 3 ? entryRoom : bossRoom).Tiles.Add((x,y));
    macro.Rooms.Add(entryRoom);
    macro.Rooms.Add(bossRoom);
    if (!TileResolver.TryResolve(model, macro, new Random(arena.Seed), out var resolved, out var failure))
        throw new InvalidOperationException($"{arena.Resref}: {failure}");
    var theme = new DungeonDetail
    {
        ThemeKey = arena.Resref, DisplayName = arena.Name, DecorationBaseDensity = 0.14,
        Tiers = new() { [1] = new DungeonTierDetail
        {
            BossResref = $"cp_{arena.Lines[0].Code}_ms", MinCreaturesPerRoom = 0, MaxCreaturesPerRoom = 0,
            Creatures = [new DungeonCreatureEntry { Resref = $"cp_{arena.Lines[0].Code}_ms" }],
            TreasureLootTableId = "CAPSTONE_BOSS_LOOT", TreasureItemCount = 1
        }}
    };
    if (arena.Profile is "modernfacility" or "secretbase")
        theme.Decorations = new[] { "_mdrn_pl_crate08", "_mdrn_pl_machin1", "_mdrn_pl_generas" }
            .Select(r => new DungeonDecorationEntry { Resref = r, Context = DecorationContext.WallAdjacent }).ToList();
    if (arena.Profile == "cep_cityinterior")
        theme.Decorations = new[] { "zep_altar002", "zep_arch002" }
            .Select(r => new DungeonDecorationEntry { Resref = r, Context = DecorationContext.WallAdjacent }).ToList();
    var composition = new DungeonComposition { Content = theme, Tileset = profile, Layout = definitions.LayoutProfiles["packed"] };
    var decorations = DungeonDecorationPlanner.Plan(resolved, profile, theme, 100, "")
        .Where(d => d.Position.X < 25f || d.Position.X > 75f || d.Position.Y > 85f).ToList();
    var result = new GenerationResult { Success = true, Layout = macro, Resolved = resolved, Tileset = model,
        AttemptSeed = arena.Seed, Parameters = composition.BuildLayoutParameters(), PlannedDecorations = decorations };
    var settings = new AreaGenerationSettings { ThemeKey = theme.ThemeKey, Width = width, Height = height, Seed = arena.Seed };
    var draft = new AreaGenerationDraft(settings, composition, model, result);
    NewAreaWriter.TilesetResolver resolver = tilesets.TryGetTileset;
    NewAreaWriter.AreaDocumentPopulator populate = (are, git, gic) =>
        {
            GeneratedAreaDocumentPopulator.Populate(draft, workspace, arena.Resref, are, git, gic);
            // Capstone masters are state-gated encounters, never permanent procedural creatures.
            ClearList(git, gic, "Creature List");
            var placed = git.Fields.GetOrAddList("Placeable List");
            var comments = gic.Fields.GetOrAddList("Placeable List");
            for (var i = placed.Count - 1; i >= 0; i--)
                if (placed[i].GetStringOrNull("OnOpen") == "proc_loot_open") { placed.RemoveAt(i); comments.RemoveAt(i); }
            ConfigureArena(arena, are, git, gic);
            ValidateAndGround(arena, draft, are, git, gic);
        };
    if (!NewAreaWriter.TryCreate(workspace, resolver, arena.Resref, arena.Name, arena.Tileset, width, height, populate, out var error))
        throw new InvalidOperationException(error);
    var preview = new AreaGenerationPreviewRenderer(resources).Render(draft, AreaPreviewMode.MapGraphics, false, false, true, 64);
    File.WriteAllBytes(Path.Combine(previewRoot, arena.Resref + ".rgba"), preview.Pixels);
    summaries.Add(new { arena.Resref, arena.Name, arena.Package, arena.Tileset, arena.Seed, Width = width, Height = height,
        EntryTag = arena.Resref + "_entry", Lines = arena.Lines, PreviewWidth = preview.Width, PreviewHeight = preview.Height,
        preview.MissingTileGraphics, PlannedDecorations = decorations.Count });
    Console.WriteLine($"Created {arena.Resref}: {bossRoom.Tiles.Count} arena floor tiles, 3 masters; preview missing {preview.MissingTileGraphics} tiles.");
}
File.WriteAllText(Path.Combine(previewRoot, "manifest.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));

void ConfigureArena(Arena arena, AreDocument are, GitDocument git, GicDocument gic)
{
    // Preserve SWLOR template event scripts; use the established arena's environment defaults.
    var reference = AreDocument.Load(Path.Combine(root, "Module", "are", "pw_sc_dath_sden.are.json"));
    foreach (var field in new[] { "DayNightCycle", "IsNight", "SkyBox", "ChanceRain", "ChanceSnow", "ChanceLightning", "WindPower",
        "SunAmbientColor", "SunDiffuseColor", "MoonAmbientColor", "MoonDiffuseColor", "SunFogAmount", "MoonFogAmount", "FogClipDist" })
        if (reference.Fields.TryGet(field, out var value)) { are.Fields.Remove(field); are.Fields.Add(field, value); }
    are.Tag = arena.Resref;
    SetLocal(git.Fields, "PLANET_TYPE_ID", arena.Planet);
    SetLocal(git.Fields, "IS_DUNGEON", 1);
    SetLocal(git.Fields, "MINI_MAP_DISABLED", 1);
    SetLocal(git.Fields, "BOSS_ARENA_SEED", arena.Seed);
    var referenceGit = GitDocument.Load(Path.Combine(root, "Module", "git", "pw_sc_dantprowar.git.json"));
    var activatorTemplate = referenceGit.Fields.GetListOrEmpty("Placeable List").First(p => p.GetStringOrNull("OnUsed") == "quest_enc");
    var entry = MakeWaypoint("nw_waypoint001", arena.Resref + "_entry", "Arena Entrance", 50f, 20f, 0f);
    Add(git, gic, "WaypointList", entry);
    Add(git, gic, "WaypointList", MakeWaypoint("nw_waypoint001", "STUCK_WAYPOINT", "Recovery", 50f, 25f, 0f));
    var positions = new[] { (30f, 65f), (50f, 75f), (70f, 65f) };
    for (var i = 0; i < arena.Lines.Length; i++)
    {
        var line = arena.Lines[i];
        var activator = Clone(activatorTemplate);
        activator.SetString("Tag", GffFieldType.CExoString, line.Code + "_ms_call");
        activator.SetSingle("X", positions[i].Item1);
        activator.SetSingle("Y", positions[i].Item2 - 5f);
        activator.SetSingle("Z", 0f);
        SetLocal(activator, "QUEST_ID", line.Stem + "_mastery");
        SetLocal(activator, "QUEST_ENCOUNTER_ID", line.Stem + "_mastery_master");
        SetLocal(activator, "QUEST_ENCOUNTER_RESREF", "cp_" + line.Code + "_ms");
        SetLocal(activator, "QUEST_ENCOUNTER_WAYPOINT", "CAPSTONE_" + line.Code.ToUpperInvariant() + "_MS_SPAWN");
        SetLocal(activator, "VISIBILITY_OBJECT_ID", line.Code + "_ms_call");
        Add(git, gic, "Placeable List", activator);
        var wp = JsonGffDocument.Load(Path.Combine(root, "Module", "utw", "wp_" + line.Code + "_ms.utw.json")).Root;
        wp.SetStructId(5);
        wp.SetSingle("XPosition", positions[i].Item1);
        wp.SetSingle("YPosition", positions[i].Item2);
        wp.SetSingle("ZPosition", 0f);
        wp.SetSingle("XOrientation", 0f);
        wp.SetSingle("YOrientation", -1f);
        Add(git, gic, "WaypointList", wp);
    }
}

void ValidateAndGround(Arena arena, AreaGenerationDraft draft, AreDocument are, GitDocument git, GicDocument gic)
{
    var scene = AreaSceneBuilder.Build(are, git, tilesets, models, walkmeshes: meshes);
    var floor = new ArenaFloor(scene.Tiles);
    // Every open tile needs real geometry, and the 2m walkable grid must connect the entrance
    // to every encounter anchor. Nonwalkable surface rows are excluded, never treated as floor.
    foreach (var room in draft.Result.Resolved.Rooms)
    foreach (var (x,y) in room.Tiles)
        if (scene.Tiles[y * 10 + x].IsFallback || scene.Tiles[y * 10 + x].Walkmesh == null)
            throw new InvalidOperationException($"{arena.Resref}: missing floor model/walkmesh at {x},{y}");
    var points = new List<(float X, float Y)>();
    foreach (var waypoint in git.Fields.GetListOrEmpty("WaypointList"))
    {
        var x = waypoint.GetSingleOrNull("XPosition")!.Value;
        var y = waypoint.GetSingleOrNull("YPosition")!.Value;
        waypoint.SetSingle("ZPosition", floor.RequireHeight(x, y));
        points.Add((x,y));
    }
    var props = git.Fields.GetOrAddList("Placeable List");
    for (var propIndex = props.Count - 1; propIndex >= 0; propIndex--)
    {
        var prop = props[propIndex];
        var x = prop.GetSingleOrNull("X")!.Value;
        var y = prop.GetSingleOrNull("Y")!.Value;
        if (prop.GetStringOrNull("OnUsed") != "quest_enc" && floor.Height(x,y) == null)
        {
            props.RemoveAt(propIndex);
            gic.Fields.GetOrAddList("Placeable List").RemoveAt(propIndex);
            continue;
        }
        prop.SetSingle("Z", floor.RequireHeight(x, y));
        if (prop.GetStringOrNull("OnUsed") == "quest_enc") points.Add((x,y));
        else
        {
            // Decorative blueprints must not carry inherited loot, shop, or quest behavior.
            prop.SetInt("Useable", GffFieldType.Byte, 0);
            prop.SetInt("Static", GffFieldType.Byte, 1);
            prop.SetInt("Plot", GffFieldType.Byte, 1);
            foreach (var eventField in prop.Entries.Where(e => e.Key.StartsWith("On", StringComparison.Ordinal)).Select(e => e.Key).ToArray())
                prop.SetString(eventField, GffFieldType.ResRef, "");
            prop.GetOrAddList("VarTable").Clear();
            prop.GetOrAddList("ItemList").Clear();
            prop.SetInt("HasInventory", GffFieldType.Byte, 0);
        }
    }
    var blockers = draft.Result.PlannedDecorations.Where(d => floor.Height(d.Position.X,d.Position.Y) != null).ToArray();
    floor.RequireConnected(points, arena.Resref, blockers);
    Console.WriteLine($"  Walkmesh: all {points.Count} entry/recovery/encounter anchors grounded and connected.");
}

JsonGffStruct MakeWaypoint(string resref, string tag, string name, float x, float y, float z)
{
    var wp = JsonGffDocument.Load(Path.Combine(root, "Module", "utw", "wp_invinc_ms.utw.json")).Root;
    wp.SetStructId(5);
    wp.SetString("TemplateResRef", GffFieldType.ResRef, resref);
    wp.SetString("Tag", GffFieldType.CExoString, tag);
    wp.GetOrAddLocString("LocalizedName").Text = name;
    wp.SetSingle("XPosition", x); wp.SetSingle("YPosition", y); wp.SetSingle("ZPosition", z);
    wp.SetSingle("XOrientation", 0f); wp.SetSingle("YOrientation", 1f);
    return wp;
}

static void Add(GitDocument git, GicDocument gic, string list, JsonGffStruct instance)
{
    var instances = git.Fields.GetOrAddList(list);
    instances.Add(instance);
    gic.InsertBlankComment(list, list == "WaypointList" ? ResourceType.Utw : ResourceType.Utp, instances.Count - 1, instances.Count);
}
static void ClearList(GitDocument git, GicDocument gic, string list)
{
    git.Fields.GetOrAddList(list).Clear(); gic.Fields.GetOrAddList(list).Clear();
}
static JsonGffStruct Clone(JsonGffStruct value) => JsonGffDocument.Parse(new JsonGffDocument("GIT ", value).ToBytes()).Root;
static void SetLocal(JsonGffStruct target, string name, object value)
{
    var list = target.GetOrAddList("VarTable");
    var local = list.FirstOrDefault(v => v.GetStringOrNull("Name") == name);
    if (local == null) { local = JsonGffField.CreateStruct(0).Struct!; list.Add(local); }
    local.SetString("Name", GffFieldType.CExoString, name);
    local.SetInt("Type", GffFieldType.Dword, value is int ? 1 : 3);
    if (value is int number) local.SetInt("Value", GffFieldType.Int, number);
    else local.SetString("Value", GffFieldType.CExoString, (string)value);
}
record Line(string Code, string Stem);
record Arena(string Resref, string Name, string Package, string Tileset, string Profile, int Planet, int Seed, bool Cave, Line[] Lines);

sealed class ArenaFloor
{
    private readonly Dictionary<(int X, int Y), List<(Vector3 A, Vector3 B, Vector3 C)>> triangles = new();
    public ArenaFloor(IReadOnlyList<TilePlacement> tiles)
    {
        foreach (var tile in tiles)
        {
            var faces = new List<(Vector3, Vector3, Vector3)>();
            if (tile.Walkmesh is { } mesh)
                foreach (var face in mesh.Faces.Where(f => f.Walkable))
                    faces.Add((Vector3.Transform(mesh.Vertices[face.A], tile.Transform),
                        Vector3.Transform(mesh.Vertices[face.B], tile.Transform), Vector3.Transform(mesh.Vertices[face.C], tile.Transform)));
            triangles[(tile.Column, tile.Row)] = faces;
        }
    }
    public float? Height(float x, float y)
    {
        float? top = null;
        // Include both sides at tile seams; NWN walkmeshes share their boundary vertices.
        for (var tx = (int)MathF.Floor((x - 0.001f) / 10); tx <= (int)MathF.Floor((x + 0.001f) / 10); tx++)
        for (var ty = (int)MathF.Floor((y - 0.001f) / 10); ty <= (int)MathF.Floor((y + 0.001f) / 10); ty++)
        {
            if (!triangles.TryGetValue((tx,ty), out var faces)) continue;
            foreach (var (a,b,c) in faces)
            {
                var det = (b.Y-c.Y)*(a.X-c.X) + (c.X-b.X)*(a.Y-c.Y);
                if (MathF.Abs(det) < 0.00001f) continue;
                var u = ((b.Y-c.Y)*(x-c.X)+(c.X-b.X)*(y-c.Y))/det;
                var v = ((c.Y-a.Y)*(x-c.X)+(a.X-c.X)*(y-c.Y))/det;
                var w = 1-u-v;
                if (u < -0.0001f || v < -0.0001f || w < -0.0001f) continue;
                var z = u*a.Z+v*b.Z+w*c.Z;
                top = top.HasValue ? MathF.Max(top.Value,z) : z;
            }
        }
        return top;
    }
    public float RequireHeight(float x, float y) => Height(x,y) ?? throw new InvalidOperationException($"No walkable floor at {x},{y}");
    public void RequireConnected(List<(float X,float Y)> points, string area, PlannedDecoration[] blockers)
    {
        bool Segment(float x, float y, float nx, float ny)
        {
            var last = Height(x,y);
            if (last == null) return false;
            for (var i=1;i<=8;i++)
            {
                var sx=x+(nx-x)*i/8; var sy=y+(ny-y)*i/8;
                if(blockers.Any(b => Vector2.Distance(new Vector2(sx,sy), new Vector2(b.Position.X,b.Position.Y)) < b.FootprintRadius*b.VisualScale+0.7f)) return false;
                var z = Height(sx,sy);
                if (z == null || MathF.Abs(z.Value-last.Value) > 0.6f) return false;
                last=z;
            }
            return true;
        }
        var seen = new HashSet<(int X,int Y)> { (25,10) };
        var queue = new Queue<(int X,int Y)>(seen);
        while(queue.TryDequeue(out var p))
            foreach(var (dx,dy) in new[] {(1,0),(-1,0),(0,1),(0,-1)})
            {
                var n=(X:p.X+dx,Y:p.Y+dy);
                if(n.X < 0 || n.Y < 0 || n.X > 50 || n.Y > 50 || seen.Contains(n)) continue;
                if(!Segment(p.X*2,p.Y*2,n.X*2,n.Y*2)) continue;
                seen.Add(n);queue.Enqueue(n);
            }
        foreach(var p in points)
            if(!seen.Any(n => Math.Abs(n.X*2-p.X) <= 2 && Math.Abs(n.Y*2-p.Y) <= 2 && Segment(n.X*2,n.Y*2,p.X,p.Y)))
                throw new InvalidOperationException($"{area}: disconnected anchor at {p}");
    }
}
