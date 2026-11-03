using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Feature;

// Guards the incubation field note system. All expectations are derived from the live mutation
// configuration (the beast definitions) rather than a hand-maintained list, so the tests stay
// valid as beasts and mutation paths are added or removed.
public class IncubationFieldNoteTests
{
    private static readonly IReadOnlyDictionary<BeastType, BeastDetail> Beasts = BeastDefinitionCatalog.All;

    // target beast -> the source beasts that can mutate into it.
    private static readonly Dictionary<BeastType, List<BeastType>> IncomingSources = BuildIncomingSources(Beasts);

    // Every distinct mutation target; there should be exactly one field note per entry.
    private static readonly HashSet<BeastType> MutationTargets = IncomingSources.Keys.ToHashSet();

    private static Dictionary<BeastType, List<BeastType>> BuildIncomingSources(IReadOnlyDictionary<BeastType, BeastDetail> beasts)
    {
        var incoming = new Dictionary<BeastType, List<BeastType>>();

        foreach (var (source, detail) in beasts)
        {
            foreach (var mutation in detail.PossibleMutations)
            {
                if (!incoming.TryGetValue(mutation.Type, out var sources))
                {
                    sources = new List<BeastType>();
                    incoming[mutation.Type] = sources;
                }

                sources.Add(source);
            }
        }

        return incoming;
    }

    // A second-level (tier-3) mutation is one whose source is itself a mutation product.
    private static bool IsSecondLevelMutation(BeastType target)
    {
        return IncomingSources[target].Any(MutationTargets.Contains);
    }

    private static KeyItemAttribute GetKeyItemAttribute(KeyItemType keyItem)
    {
        var field = typeof(KeyItemType).GetField(keyItem.ToString())!;
        return field.GetCustomAttribute<KeyItemAttribute>()!;
    }

    private static IEnumerable<KeyItemType> FieldNoteKeyItems()
    {
        return Enum.GetValues<KeyItemType>()
            .Where(keyItem => GetKeyItemAttribute(keyItem).Category == KeyItemCategoryType.FieldNotes);
    }

    [Test]
    public void EveryMutationTarget_HasAFieldNote()
    {
        var registeredTargets = IncubationFieldNote.GetAllNotes().Select(n => n.Target).ToHashSet();

        var missing = MutationTargets.Except(registeredTargets).ToList();

        missing.Should().BeEmpty("every beast reachable by mutation needs a field note");
    }

    [Test]
    public void EveryFieldNote_TargetsARealMutation()
    {
        var registeredTargets = IncubationFieldNote.GetAllNotes().Select(n => n.Target).ToHashSet();

        var stray = registeredTargets.Except(MutationTargets).ToList();

        stray.Should().BeEmpty("a field note should only exist for a beast that is actually a mutation product");
    }

    [Test]
    public void FieldNotesCategory_MatchesRegistry()
    {
        var categoryKeyItems = FieldNoteKeyItems().ToHashSet();
        var registeredKeyItems = IncubationFieldNote.GetAllNotes().Select(n => n.Note).ToHashSet();

        registeredKeyItems.Should().BeEquivalentTo(categoryKeyItems,
            "every FieldNotes key item must be registered and every registered note must be a FieldNotes key item");
    }

    [Test]
    public void RegisteredNotes_AreUniqueAndValid()
    {
        var notes = IncubationFieldNote.GetAllNotes();

        notes.Select(n => n.Note).Should().OnlyHaveUniqueItems();
        notes.Select(n => n.Target).Should().OnlyHaveUniqueItems();
        notes.Should().OnlyContain(n => n.Acquisition != FieldNoteAcquisitionType.Invalid,
            "every field note must declare a real acquisition type");
    }

    [Test]
    public void SecondLevelMutations_AreNeverSoldInStores()
    {
        var acquisitionByTarget = IncubationFieldNote.GetAllNotes().ToDictionary(n => n.Target, n => n.Acquisition);

        foreach (var target in MutationTargets.Where(IsSecondLevelMutation))
        {
            acquisitionByTarget[target].Should().NotBe(FieldNoteAcquisitionType.Store,
                $"{target} is a second-level mutation and its note must not be purchasable");
        }
    }

    [Test]
    public void EveryMutationRequirement_DescribesItself()
    {
        foreach (var (source, detail) in Beasts)
        {
            foreach (var mutation in detail.PossibleMutations)
            {
                foreach (var requirement in mutation.Requirements)
                {
                    requirement.GetRequirementDescription().Should().NotBeNullOrWhiteSpace(
                        $"{requirement.GetType().Name} on {source} -> {mutation.Type} must produce a field-note description");
                }
            }
        }
    }

    [Test]
    public void EnzymeRequirement_ExposesEachEnzymeAsASeparateDescription()
    {
        var requirement = new MutationRequirementEnzyme();
        requirement.LyaseEnzymeColors[EnzymeColorType.Black] = 2;
        requirement.IsomeraseEnzymeColors[EnzymeColorType.Red] = 2;
        requirement.HydrolaseEnzymeColors[EnzymeColorType.Red] = 1;

        requirement.GetEnzymeDescriptions().Should().Equal(
            "2x Black Lyase",
            "2x Red Isomerase",
            "1x Red Hydrolase");
    }

    private static string NoteResref(FieldNoteDetail note) => $"fnote_{(int)note.Note}";

    private sealed record NoteSources(
        HashSet<string> Store,
        HashSet<string> Slicing,
        Dictionary<string, HashSet<string>> CreaturesByNote,
        Dictionary<string, List<(string Creature, int Chance, int Attempts)>> TableRollsByTable,
        Dictionary<string, LootTable> NoteTables);

    private static readonly Lazy<NoteSources> Sources = new(BuildSources);

    private static NoteSources BuildSources()
    {
        var root = FindRepositoryRoot();

        var store = Regex.Matches(
                File.ReadAllText(Path.Combine(root.FullName, "Module", "utm", "fnote_store.utm.json")),
                @"fnote_\d+")
            .Select(match => match.Value)
            .ToHashSet();

        var slicing = SlicingRewardCatalog.Entries
            .Where(entry => entry.Category == SlicingRewardCategory.FieldNote)
            .Select(entry => entry.Resref)
            .ToHashSet();

        var noteTables = typeof(ILootTableDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ILootTableDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables())
            .Where(pair => pair.Value.Any(item => item.Resref.StartsWith("fnote_", StringComparison.Ordinal)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var creaturesByNote = new Dictionary<string, HashSet<string>>();
        var tableRolls = new Dictionary<string, List<(string Creature, int Chance, int Attempts)>>();

        foreach (var file in Directory.GetFiles(Path.Combine(root.FullName, "Module", "utc"), "*.utc.json"))
        {
            var creature = Path.GetFileName(file)[..^".utc.json".Length];
            var text = File.ReadAllText(file, Encoding.Latin1);

            foreach (Match match in Regex.Matches(text, @"""value"": ""([^"",]+),(\d+),(\d+)"""))
            {
                var table = match.Groups[1].Value;
                if (!noteTables.TryGetValue(table, out var lootTable))
                    continue;

                if (!tableRolls.TryGetValue(table, out var rolls))
                {
                    rolls = new List<(string, int, int)>();
                    tableRolls[table] = rolls;
                }

                rolls.Add((creature, int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value)));

                foreach (var item in lootTable.Where(item => item.Resref.StartsWith("fnote_", StringComparison.Ordinal)))
                {
                    if (!creaturesByNote.TryGetValue(item.Resref, out var creatures))
                    {
                        creatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        creaturesByNote[item.Resref] = creatures;
                    }

                    creatures.Add(creature);
                }
            }
        }

        return new NoteSources(store, slicing, creaturesByNote, tableRolls, noteTables);
    }

    // Creatures that spawn through a rare entry of a spawn table.
    private static HashSet<string> RareSpawnCreatures()
    {
        return typeof(ISpawnListDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ISpawnListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((ISpawnListDefinition)Activator.CreateInstance(type)!).BuildSpawnTables().Values)
            .SelectMany(table => table.Spawns)
            .Where(spawn => spawn.IsRare)
            .Select(spawn => spawn.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    [Test]
    public void EveryFieldNote_HasABlueprintForItsKeyItemAndBeast()
    {
        var root = FindRepositoryRoot();

        foreach (var note in IncubationFieldNote.GetAllNotes())
        {
            var resref = NoteResref(note);
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            File.Exists(path).Should().BeTrue($"{note.Target} needs the {resref} item blueprint");

            using var blueprint = JsonDocument.Parse(File.ReadAllText(path, Encoding.Latin1));
            var json = blueprint.RootElement;

            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(resref);
            json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!.Replace("'", "")
                .Should().Be($"Field Note: {Beasts[note.Target].Name}".Replace("'", ""));

            var keyItemId = json.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Single(variable => variable.GetProperty("Name").GetProperty("value").GetString() == "KEY_ITEM_ID")
                .GetProperty("Value").GetProperty("value").GetInt32();
            keyItemId.Should().Be((int)note.Note, $"{resref} must grant the {note.Note} key item");
        }
    }

    [Test]
    public void EveryFieldNote_CanBeObtainedWithoutSlicing()
    {
        var sources = Sources.Value;

        foreach (var note in IncubationFieldNote.GetAllNotes())
        {
            var resref = NoteResref(note);
            var obtainable = sources.Store.Contains(resref) || sources.CreaturesByNote.ContainsKey(resref);

            obtainable.Should().BeTrue(
                $"{note.Target}'s field note ({resref}) must be sold or dropped by a creature, not only found by slicing or by performing the mutation");
        }
    }

    [Test]
    public void AcquisitionTypes_MatchWhereNotesAreObtained()
    {
        var sources = Sources.Value;
        var rareSpawns = RareSpawnCreatures();

        foreach (var note in IncubationFieldNote.GetAllNotes())
        {
            var resref = NoteResref(note);
            var sold = sources.Store.Contains(resref);
            var droppers = sources.CreaturesByNote.GetValueOrDefault(resref) ?? new HashSet<string>();
            var droppedByRareSpawn = droppers.Any(rareSpawns.Contains);

            switch (note.Acquisition)
            {
                case FieldNoteAcquisitionType.Store:
                    sold.Should().BeTrue($"{note.Target} is labelled Store but the field note vendor does not stock {resref}");
                    break;
                case FieldNoteAcquisitionType.RareSpawnDrop:
                    sold.Should().BeFalse($"{note.Target} is labelled RareSpawnDrop but {resref} is sold");
                    droppedByRareSpawn.Should().BeTrue($"{note.Target} is labelled RareSpawnDrop but no rare spawn drops {resref}");
                    break;
                case FieldNoteAcquisitionType.BossDrop:
                    sold.Should().BeFalse($"{note.Target} is labelled BossDrop but {resref} is sold");
                    droppers.Should().NotBeEmpty($"{note.Target} is labelled BossDrop but no creature drops {resref}");
                    droppedByRareSpawn.Should().BeFalse(
                        $"{note.Target} is labelled BossDrop but a rare spawn drops {resref}; label it RareSpawnDrop");
                    break;
                default:
                    Assert.Fail($"{note.Target} has acquisition type {note.Acquisition}");
                    break;
            }
        }
    }

    [Test]
    public void EveryFieldNoteSource_IsARegisteredFieldNote()
    {
        var sources = Sources.Value;
        var registered = IncubationFieldNote.GetAllNotes().Select(NoteResref).ToHashSet();

        var referenced = sources.Store
            .Concat(sources.Slicing)
            .Concat(sources.CreaturesByNote.Keys)
            .Distinct();

        referenced.Should().BeSubsetOf(registered, "every stocked or dropped field note must be a registered note");
    }

    [Test]
    public void RareSpawnNoteTables_DropTenPercentPerNote()
    {
        var sources = Sources.Value;

        foreach (var (table, lootTable) in sources.NoteTables.Where(pair => pair.Key.StartsWith("FNOTE_", StringComparison.Ordinal)))
        {
            lootTable.IsRare.Should().BeTrue($"{table} should respond to rare find");
            lootTable.Should().OnlyContain(item =>
                item.Resref.StartsWith("fnote_", StringComparison.Ordinal) && item.IsRare && item.Weight == 1 && item.MaxQuantity == 1);

            var rolls = sources.TableRollsByTable.GetValueOrDefault(table);
            rolls.Should().NotBeNullOrEmpty($"{table} must be rolled by a creature");

            foreach (var (creature, chance, attempts) in rolls!)
            {
                attempts.Should().Be(1, $"{creature} rolls {table} once per kill");
                chance.Should().Be(10 * lootTable.Count, $"{creature} should drop each note in {table} at 10%");
            }
        }
    }
}
