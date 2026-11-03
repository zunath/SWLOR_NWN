using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// Unit tests of the incubation field note instance: the key items are the real key item instance over the in-memory
/// database, and the beast catalog and the log (state and files of services that are not converted yet) are answered
/// by a subclass. The registry itself is checked against the live beast definitions by
/// <c>IncubationFieldNoteTests</c>; here it is the behaviour of the instance.
/// </summary>
[TestFixture]
public class IncubationFieldNoteServiceTests
{
    private const string PlayerId = PlayerEngine.PlayerId;
    private const uint PlayerObject = 1001;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private KeyItemService _keyItems = null!;
    private CatalogService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _db.With(new Player(PlayerId));
        _keyItems = new KeyItemService(_db);
        _keyItems.LoadData();
        _service = new CatalogService(_keyItems);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    private static string Lines(params string[] lines) => string.Join("\n", lines);

    private static string Normalized(string text) => text.Replace("\r\n", "\n");

    private string DescriptionOf(BeastType target) =>
        Normalized(_keyItems.GetKeyItem(_service.GetNote(target).Note).Description);

    // The three beasts of the small catalog: Frog can mutate into Aardvark (with a chance, an enzyme and a blank
    // requirement), Bhalir can too (with an enzyme requirement that names no enzyme), and Aardvark can mutate into
    // Frog (no requirements at all).
    private void UseSmallCatalog()
    {
        _service.Beasts[BeastType.Aardvark] = new BeastDetail
        {
            Name = "Aardvark",
            PossibleMutations = { new MutationDetail(BeastType.Frog) { Requirements = null } }
        };
        _service.Beasts[BeastType.Frog] = new BeastDetail
        {
            Name = "Frog",
            PossibleMutations =
            {
                new MutationDetail(BeastType.Aardvark)
                {
                    Requirements =
                    {
                        new MutationRequirementMinimumChance(25),
                        new MutationRequirementEnzyme { LyaseEnzymeColors = { [EnzymeColorType.Black] = 2 } },
                        new BlankRequirement()
                    }
                }
            }
        };
        _service.Beasts[BeastType.Bhalir] = new BeastDetail
        {
            Name = "Bhalir",
            PossibleMutations =
            {
                new MutationDetail(BeastType.Aardvark) { Requirements = { new MutationRequirementEnzyme() } }
            }
        };
    }

    // ---- the registry

    [Test]
    public void TheConstructorDoesNoWork()
    {
        var create = () => new IncubationFieldNoteService(null!);

        create.Should().NotThrow();
    }

    [Test]
    public void EnsureRegistered_FillsTheRegistryOnceAndGetAllNotesHandsOutACopy()
    {
        _service.EnsureRegistered();
        var count = _service.GetAllNotes().Count;
        _service.EnsureRegistered();

        count.Should().BeGreaterThan(100);
        _service.GetAllNotes().Should().HaveCount(count);
        _service.GetAllNotes().Should().NotBeSameAs(_service.GetAllNotes());
    }

    [Test]
    public void EveryReaderRegistersTheNotesOnFirstUse()
    {
        new CatalogService(_keyItems).TryGetNoteForTarget(BeastType.Aardvark, out _).Should().BeTrue();
        new CatalogService(_keyItems).TryGetNoteForKeyItem(KeyItemType.IncubationFieldNoteAardvark, out _).Should().BeTrue();
        new CatalogService(_keyItems).GetAllNotes().Should().NotBeEmpty();
    }

    [Test]
    public void TryGetNoteForTarget_FindsTheNoteOfARegisteredBeastOnly()
    {
        _service.TryGetNoteForTarget(BeastType.Aardvark, out var note).Should().BeTrue();
        note.Target.Should().Be(BeastType.Aardvark);
        note.Note.Should().Be(KeyItemType.IncubationFieldNoteAardvark);
        note.Acquisition.Should().Be(FieldNoteAcquisitionType.Store);

        _service.TryGetNoteForTarget(BeastType.Invalid, out var missing).Should().BeFalse();
        missing.Should().BeNull();
    }

    [Test]
    public void TryGetNoteForKeyItem_FindsTheNoteOfARegisteredKeyItemOnly()
    {
        _service.TryGetNoteForKeyItem(KeyItemType.IncubationFieldNoteAbysswebRavager, out var note).Should().BeTrue();
        note.Target.Should().Be(BeastType.AbysswebRavager);
        note.Acquisition.Should().Be(FieldNoteAcquisitionType.BossDrop);

        _service.TryGetNoteForKeyItem(KeyItemType.Invalid, out var missing).Should().BeFalse();
        missing.Should().BeNull();
    }

    [Test]
    public void EachInstanceKeepsItsOwnRegistry()
    {
        var other = new CatalogService(_keyItems);
        other.EnsureRegistered();
        other.GetAllNotes().Should().NotBeEmpty();

        var detailOfFirst = _service.GetNote(BeastType.Aardvark);
        var detailOfOther = other.GetNote(BeastType.Aardvark);

        detailOfFirst.Should().NotBeSameAs(detailOfOther, "each instance builds its own details");
    }

    // ---- granting the note of a mutation the player produced

    [Test]
    public void GrantDiscoveredNote_GivesThePlayerTheNoteOfTheProducedBeastOnce()
    {
        _service.GrantDiscoveredNote(PlayerObject, BeastType.Aardvark);
        _service.GrantDiscoveredNote(PlayerObject, BeastType.Aardvark);

        var saved = _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>().Subject;
        saved.KeyItems.Should().ContainKey(KeyItemType.IncubationFieldNoteAardvark);
        _engine.Messages().Should().Equal(
            $"You acquire the '{_keyItems.GetKeyItem(KeyItemType.IncubationFieldNoteAardvark).Name}' key item.");
    }

    [Test]
    public void GrantDiscoveredNote_ForABeastWithoutANote_DoesNothing()
    {
        _service.GrantDiscoveredNote(PlayerObject, BeastType.Invalid);

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void GrantDiscoveredNote_IgnoresCreaturesThatAreNotPlayers()
    {
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.GrantDiscoveredNote(PlayerObject, BeastType.Aardvark);

        _db.Saved.Should().BeEmpty();
    }

    // ---- the display text of the notes

    [Test]
    public void LoadDisplayText_NamesEachNoteAfterItsBeastAndListsEveryKnownMethod()
    {
        UseSmallCatalog();

        _service.LoadDisplayText();

        _keyItems.GetKeyItem(KeyItemType.IncubationFieldNoteAardvark).Name.Should().Be("Aardvark");
        DescriptionOf(BeastType.Aardvark).Should().Be(Lines(
            "Field research on how to incubate a Aardvark.",
            "",
            "Known incubation methods (2):",
            "From Bhalir:",
            "- No special requirements",
            "",
            "From Frog:",
            "- Mutation chance at least 25%",
            "- 2x Black Lyase"));
    }

    [Test]
    public void LoadDisplayText_OfASingleMethodWithoutRequirements_UsesTheSingularHeader()
    {
        UseSmallCatalog();

        _service.LoadDisplayText();

        _keyItems.GetKeyItem(KeyItemType.IncubationFieldNoteFrog).Name.Should().Be("Frog");
        DescriptionOf(BeastType.Frog).Should().Be(Lines(
            "Field research on how to incubate a Frog.",
            "",
            "Known incubation method:",
            "From Aardvark:",
            "- No special requirements"));
    }

    [Test]
    public void LoadDisplayText_OfABeastNothingMutatesInto_SaysNoMethodIsKnown()
    {
        UseSmallCatalog();

        _service.LoadDisplayText();

        DescriptionOf(BeastType.Bhalir).Should().Be(Lines(
            "Field research on how to incubate a Bhalir.",
            "",
            "No known incubation method."));
    }

    [Test]
    public void LoadDisplayText_LogsEachNoteWhoseBeastIsMissingFromTheCatalogAndCountsTheRest()
    {
        UseSmallCatalog();
        var registered = _service.GetAllNotes().Count;

        _service.LoadDisplayText();

        var failures = _service.Logs.Where(entry => entry.Details.StartsWith("Failed to populate", StringComparison.Ordinal)).ToList();
        failures.Should().HaveCount(registered - 3);
        failures.Should().OnlyContain(entry => entry.Group == LogGroup.Incubation && entry.PrintToConsole);
        failures.Should().Contain(entry => entry.Details.StartsWith(
            "Failed to populate incubation field note for beast 'AbysswebRavager' (key item 'IncubationFieldNoteAbysswebRavager'): ",
            StringComparison.Ordinal));
        _service.Logs[^1].Should().Be((LogGroup.Incubation, $"Populated 3/{registered} incubation field notes.", false));
    }

    [Test]
    public void LoadDisplayText_BeforeTheKeyItemsAreCached_PopulatesNothing()
    {
        UseSmallCatalog();
        var uncached = new CatalogService(new KeyItemService(_db));
        foreach (var (type, detail) in _service.Beasts)
            uncached.Beasts[type] = detail;
        var registered = uncached.GetAllNotes().Count;

        uncached.LoadDisplayText();

        uncached.Logs.Should().HaveCount(registered + 1);
        uncached.Logs[^1].Details.Should().Be($"Populated 0/{registered} incubation field notes.");
    }

    [Test]
    public void LoadDisplayText_RegistersTheNotesWhenNothingElseHas()
    {
        UseSmallCatalog();
        var fresh = new CatalogService(_keyItems);
        foreach (var (type, detail) in _service.Beasts)
            fresh.Beasts[type] = detail;

        fresh.LoadDisplayText();

        _keyItems.GetKeyItem(KeyItemType.IncubationFieldNoteAardvark).Name.Should().Be("Aardvark");
    }

    [Test]
    public void LoadDisplayText_WithTheLiveBeastDefinitions_PopulatesEveryNote()
    {
        foreach (var (type, detail) in BuildAllBeasts())
            _service.Beasts[type] = detail;
        var notes = _service.GetAllNotes();

        _service.LoadDisplayText();

        _service.Logs.Where(entry => entry.Details.StartsWith("Failed", StringComparison.Ordinal)).Should().BeEmpty();
        _service.Logs[^1].Details.Should().Be($"Populated {notes.Count}/{notes.Count} incubation field notes.");
        foreach (var note in notes)
        {
            var attribute = _keyItems.GetKeyItem(note.Note);
            attribute.Name.Should().Be(_service.Beasts[note.Target].Name);
            Normalized(attribute.Description).Should().StartWith($"Field research on how to incubate a {attribute.Name}.\n\nKnown incubation method");
        }
    }

    // ---- the facade

    [Test]
    public void TheFacadeReadsTheContainersRegistry()
    {
        using var container = new FacadeContainer();
        using var scope = container.Install();

        IncubationFieldNote.GetAllNotes().Should().NotBeEmpty();
        IncubationFieldNote.TryGetNoteForTarget(BeastType.Aardvark, out var byTarget).Should().BeTrue();
        IncubationFieldNote.TryGetNoteForKeyItem(byTarget.Note, out var byKeyItem).Should().BeTrue();
        byKeyItem.Should().BeSameAs(byTarget);
        IncubationFieldNote.EnsureRegistered();
    }

    // The live beast definitions, built the way BeastMastery.LoadBeasts builds them without the console line.
    private static Dictionary<BeastType, BeastDetail> BuildAllBeasts()
    {
        var result = new Dictionary<BeastType, BeastDetail>();
        var definitions = typeof(IBeastListDefinition).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IBeastListDefinition).IsAssignableFrom(type));

        foreach (var definitionType in definitions)
        {
            var definition = (IBeastListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (beastType, detail) in definition.Build())
                result[beastType] = detail;
        }

        return result;
    }

    /// <summary>A requirement whose description is blank, which the field note leaves out.</summary>
    private sealed class BlankRequirement : IMutationRequirement
    {
        public string CheckRequirements(IncubationJob job) => string.Empty;

        public string GetRequirementDescription() => "  ";
    }

    /// <summary>The instance with the beast catalog and the log answered by the test.</summary>
    private sealed class CatalogService : IncubationFieldNoteService
    {
        public CatalogService(IKeyItemService keyItem)
            : base(keyItem)
        {
        }

        public Dictionary<BeastType, BeastDetail> Beasts { get; } = new();

        public List<(LogGroup Group, string Details, bool PrintToConsole)> Logs { get; } = new();

        public FieldNoteDetail GetNote(BeastType target)
        {
            TryGetNoteForTarget(target, out var note).Should().BeTrue();
            return note;
        }

        protected override IEnumerable<BeastType> GetAllBeastTypes() => Beasts.Keys.ToList();

        protected override BeastDetail GetBeastDetail(BeastType type) => Beasts[type];

        protected override void WriteLog(LogGroup group, string details, bool printToConsole = false) =>
            Logs.Add((group, details, printToConsole));
    }
}
