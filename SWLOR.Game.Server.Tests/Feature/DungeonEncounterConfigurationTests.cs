using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class DungeonEncounterConfigurationTests
{
    public record Dungeon(string Area, string Arena, int Key, string RareTable);

    private static readonly Dungeon[] Dungeons =
    {
        new("pw_sc_velesannx", "pw_sc_velescmd", 107, "VELES_MILITIA_ANNEX_RARES"),
        new("pw_sc_jedihalls", "pw_sc_jeditrial", 108, "DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS_RARES"),
        new("pw_sc_forgecav", "pw_sc_korrforge", 109, "KORRIBAN_FORGE_CAVERNS_RARES"),
        new("pw_sc_emfbackr", "pw_sc_smarena", 110, "FIGHTCLUB_BACKROOMS_RARES"),
        new("cz220shipbreakin", "cz220shipbreaker", 111, "CZ220_BREAKER_YARD_RARES"),
        new("pw_sc_canyonrng", "pw_sc_canyonpit", 112, "ANCHORHEAD_CANYON_RANGE_RARES"),
        new("pw_ar_czarmrange", "ka_ar_czweaparen", 113, "CZERKA_ARMS_TEST_RANGE_RARES"),
        new("pw_sc_qiontest", "pw_sc_qioncore", 114, "HUTLAR_QION_TEST_SITE_RARES"),
        new("pw_sc_cryptdeep", "pw_sc_sithritual", 115, "KORRIBAN_SITH_CRYPT_DEPTHS_RARES"),
        new("pw_sc_repubeng", "pw_sc_repubcmd", 116, "VISCARA_REPUBLIC_ENGINEERING_BUNKER_RARES"),
        new("pw_sc_dantmedsub", "pw_sc_dantprowar", 117, "DANTOOINE_MEDICAL_SUBLEVEL_RARES"),
        new("pw_sc_tarnpres", "pw_sc_tarnalpha", 118, "DATHOMIR_TARN_JUNGLE_PRESERVE_RARES"),
        new("pw_sc_dath_apexd", "pw_sc_dath_sden", 119, "DATHOMIR_GROTTO_APEX_DEN_RARES"),
    };

    private string _root = null!;
    private Dictionary<string, JsonElement> _areas = null!;
    private Dictionary<string, SpawnTable> _spawns = null!;
    private Dictionary<string, LootTable> _loot = null!;
    private record PlacedObject(string Area, JsonElement Object);
    private record Route(PlacedObject Source, string Destination);
    private Dictionary<string, PlacedObject[]> _tags = null!;
    private Route[] _routes = null!;

    [OneTimeSetUp]
    public void LoadConfiguration()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        _root = directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        _areas = Directory.EnumerateFiles(Path.Combine(_root, "Module", "git"), "*.git.json")
            .ToDictionary(p => Path.GetFileName(p).Split('.')[0], Read);
        var objects = _areas.SelectMany(a => Objects(a.Value).Select(o => new PlacedObject(a.Key, o))).ToArray();
        _tags = objects.Where(o => Text(o.Object, "Tag") != string.Empty)
            .GroupBy(o => Text(o.Object, "Tag"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        _routes = objects.SelectMany(o => new[] { Text(o.Object, "LinkedTo"), Local(o.Object, "DESTINATION") }
                .Where(d => !string.IsNullOrEmpty(d)).Select(d => new Route(o, d)))
            .ToArray();
        _spawns = Definitions<ISpawnListDefinition>().SelectMany(d => d.BuildSpawnTables())
            .ToDictionary(t => t.Key, t => t.Value);
        _loot = Definitions<ILootTableDefinition>().SelectMany(d => d.BuildLootTables())
            .ToDictionary(t => t.Key, t => t.Value);
    }

    [TestCaseSource(nameof(Dungeons))]
    public void DungeonAndArena_HaveRecoveryWaypoints(Dungeon dungeon)
    {
        foreach (var area in new[] { dungeon.Area, dungeon.Arena })
            List(_areas[area], "WaypointList").Should().ContainSingle(w => Text(w, "Tag") == "STUCK_WAYPOINT",
                $"{area} needs a recovery point for stuck players");
    }

    [TestCaseSource(nameof(Dungeons))]
    public void RareEnemyPool_HasAPlacedSpawnAndReachableLoot(Dungeon dungeon)
    {
        var waypoints = List(_areas[dungeon.Area], "WaypointList");
        waypoints.Count(w => Text(w, "Tag") == dungeon.RareTable).Should().Be(1,
            "registering a rare table alone does not make its creatures or loot obtainable");
        _spawns.Should().ContainKey(dungeon.RareTable);
        var table = _spawns[dungeon.RareTable];
        table.RespawnDelayMinutes.Should().BePositive();
        table.Spawns.Should().HaveCount(3).And.OnlyContain(s => s.IsRare && s.Weight > 0 && s.Type == ObjectType.Creature);
        foreach (var spawn in table.Spawns)
        {
            AssertCreatureLoot(spawn.Resref);
            var creature = Load("utc", spawn.Resref);
            var tables = List(creature, "VarTable").Where(v => Text(v, "Name").StartsWith("LOOT_TABLE_"))
                .Select(v => Text(v, "Value").Split(',')[0]);
            tables.Should().Contain(id => _loot[id].IsRare, "named rare enemies must expose their rare rewards");
        }
    }

    [TestCaseSource(nameof(Dungeons))]
    public void LessonWardensAndMasters_ResolveFromAreaToCreatureAndLoot(Dungeon dungeon)
    {
        var group = CapstoneQuestDefinitionTestData.AreaGroups.Single(g => (int)g.AccessKeyItem == dungeon.Key);
        var area = _areas[dungeon.Area];
        var arena = _areas[dungeon.Arena];
        var fixedSpawns = List(area, "WaypointList").Count(w => Text(w, "Tag") == group.SpawnTableId);
        (fixedSpawns > 0 || Local(area, "CREATURE_SPAWN_TABLE_ID") == group.SpawnTableId).Should().BeTrue();
        _spawns[group.SpawnTableId].Spawns.Should().OnlyContain(s => s.Weight > 0 && !s.IsRare);
        foreach (var spawn in _spawns[group.SpawnTableId].Spawns)
            AssertCreatureLoot(spawn.Resref);

        foreach (var (room, step) in new[] { (dungeon.Area, 2), (dungeon.Arena, 4) })
        {
            var calls = List(_areas[room], "Placeable List").Where(p => Text(p, "OnUsed") == "quest_enc").ToArray();
            calls.Should().HaveCount(3);
            foreach (var line in CapstoneQuestDefinitionTestData.Lines.Where(l => l.AreaGroup == group))
            {
                var call = calls.Single(p => Local(p, "QUEST_ENCOUNTER_RESREF") == line.EnemyResrefs[step]);
                Local(call, "QUEST_ID").Should().Be(line.QuestIds[step]);
                Local(call, "QUEST_STATE").Should().Be("1");
                Local(call, "QUEST_ENCOUNTER_WAYPOINT").Should().Be(line.EncounterSpawnWaypointTags[step]);
                Local(call, "VISIBILITY_HIDDEN_DEFAULT").Should().Be("1");
                Local(call, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").Should().Be("60");
                Value(call, "Appearance").GetInt32().Should().Be(479);
                Value(call, "Useable").GetInt32().Should().Be(1);
                Value(call, "Static").GetInt32().Should().Be(0);
                Value(call, "Plot").GetInt32().Should().Be(1);
                Value(call, "LocName").GetProperty("0").GetString().Should().Be("???");
                var waypoint = _tags[line.EncounterSpawnWaypointTags[step]];
                waypoint.Should().ContainSingle().Which.Area.Should().Be(room);
                AssertCreatureLoot(line.EnemyResrefs[step], directSpawn: true);
            }
        }
        List(arena, "WaypointList").Should().NotContain(w => Text(w, "Tag") == group.SpawnTableId || Text(w, "Tag") == dungeon.RareTable);
    }

    [TestCaseSource(nameof(Dungeons))]
    public void WorldAccess_IsKeyGatedWithPartyEntryAndReturnRoutes(Dungeon dungeon)
    {
        var rooms = new[] { dungeon.Area, dungeon.Arena };
        bool Targets(Route r, string area) => _tags.TryGetValue(r.Destination, out var targets) && targets.Any(t => t.Area == area);
        var incoming = _routes.Where(r => !rooms.Contains(r.Source.Area) && rooms.Any(a => Targets(r, a))).ToArray();
        incoming.Should().ContainSingle("each package should have one world entrance and no ungated shortcut");
        var gate = incoming.Single();
        Targets(gate, dungeon.Area).Should().BeTrue();
        _tags[gate.Destination].Should().ContainSingle();
        Text(gate.Source.Object, "OnUsed").Should().Be("teleport");
        Local(gate.Source.Object, "KEY_ITEM_ID").Should().Be(dungeon.Key.ToString());
        Local(gate.Source.Object, "TELEPORT_PARTY_MEMBERS").Should().Be("1");
        Value(gate.Source.Object, "Useable").GetInt32().Should().Be(1);
        Value(gate.Source.Object, "Static").GetInt32().Should().Be(0);

        foreach (var route in _routes.Where(r => rooms.Contains(r.Source.Area)))
            _tags.Should().ContainKey(route.Destination, "every dungeon and arena exit must resolve");
        _routes.Where(r => r.Source.Area == dungeon.Area && Targets(r, gate.Source.Area)).Should().NotBeEmpty("the dungeon needs a return to its world entrance");
        _routes.Where(r => r.Source.Area == dungeon.Area && Targets(r, dungeon.Arena)).Should().NotBeEmpty();
        _routes.Where(r => r.Source.Area == dungeon.Arena && Targets(r, dungeon.Area)).Should().NotBeEmpty();
    }

    private void AssertCreatureLoot(string resref, bool directSpawn = false)
    {
        var creature = Load("utc", resref);
        Text(creature, "TemplateResRef").Should().Be(resref);
        Text(creature, "ScriptSpawn").Should().Be("x2_def_spawn");
        // Spawn.AdjustScripts supplies this for table spawns. QuestEncounter creates its
        // creatures directly, so their blueprints must already carry the death script.
        if (directSpawn)
            Text(creature, "ScriptDeath").Should().Be("x2_def_ondeath");
        var locals = List(creature, "VarTable").Where(v => Text(v, "Name").StartsWith("LOOT_TABLE_")).ToArray();
        locals.Should().NotBeEmpty($"{resref} needs loot");
        locals.Select(v => Text(v, "Name")).Should().BeEquivalentTo(
            Enumerable.Range(1, locals.Length).Select(i => $"LOOT_TABLE_{i}"),
            "the runtime stops reading loot tables at the first missing index");
        foreach (var local in locals)
        {
            var args = Text(local, "Value").Split(',');
            _loot.Should().ContainKey(args[0], $"{resref} references this loot table");
            var table = _loot[args[0]];
            table.Should().NotBeEmpty().And.OnlyContain(i => i.Weight > 0 && i.MaxQuantity > 0);
            if (args.Length > 1) int.Parse(args[1]).Should().BeInRange(1, 100);
            if (args.Length > 2) int.Parse(args[2]).Should().BePositive();
            foreach (var item in table.Where(i => i.Resref != "nw_it_gold001"))
                File.Exists(Path.Combine(_root, "Module", "uti", $"{item.Resref}.uti.json")).Should().BeTrue($"{resref}/{args[0]} drops {item.Resref}");
        }
    }

    private static IEnumerable<T> Definitions<T>() => typeof(T).Assembly.GetTypes()
        .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
        .Select(t => (T)Activator.CreateInstance(t)!);
    private JsonElement Load(string type, string name) => Read(Path.Combine(_root, "Module", type, $"{name}.{type}.json"));
    private static JsonElement Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }
    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => o.TryGetProperty(key, out var value) ? value.GetProperty("value").ToString() : string.Empty;
    private static JsonElement[] List(JsonElement o, string key) => o.TryGetProperty(key, out var value) ? value.GetProperty("value").EnumerateArray().ToArray() : Array.Empty<JsonElement>();
    private static string Local(JsonElement o, string name) => List(o, "VarTable").Where(v => Text(v, "Name") == name).Select(v => Text(v, "Value")).SingleOrDefault() ?? string.Empty;
    private static IEnumerable<JsonElement> Objects(JsonElement area) => area.EnumerateObject()
        .Where(p => p.Name != "VarTable" && p.Value.ValueKind == JsonValueKind.Object && p.Value.TryGetProperty("type", out var type) && type.GetString() == "list")
        .SelectMany(p => p.Value.GetProperty("value").EnumerateArray());
}
