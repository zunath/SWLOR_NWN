using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the descriptor instance. <c>PlayerName</c> is a scripted stand-in (sanitizing strips surrounding
/// white space and the refresh is recorded), the database is in memory and the log and the disguise are the two
/// <c>protected virtual</c> seams.
/// </summary>
[TestFixture]
public class PlayerDescriptorServiceTests
{
    private const uint PlayerObject = 1001;
    private const string PlayerId = "player-uuid-1";

    private FakeNWScript _engine = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _nameScript = null!;
    private BenchDescriptorService _service = null!;
    private IDisposable _scope = null!;
    private int _refreshes;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturn("GetIsDM", false);
        _engine.SetReturn("GetObjectUUID", PlayerId);
        _engine.SetReturn("GetName", "Canonical Name");
        _engine.SetReturn("Get2DAString", "Human");
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService();
        _refreshes = 0;
        var names = ScriptedProxy.Create<IPlayerNameService>(out _nameScript);
        _nameScript.On("SanitizeKnownName", arguments => ((string?)arguments[0])?.Trim() ?? string.Empty);
        _nameScript.On("ValidateKnownNameInput", arguments => string.IsNullOrWhiteSpace((string?)arguments[0]) ? "Please enter a name." : string.Empty);
        _nameScript.On("RefreshNameOverridesForPlayer", _ =>
        {
            _refreshes++;
            return null;
        });
        _service = new BenchDescriptorService(_db, names);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private sealed class BenchDescriptorService : PlayerDescriptorService
    {
        public BenchDescriptorService(IDBService db, IPlayerNameService names)
            : base(db, names)
        {
        }

        public List<(LogGroup Group, string Template, object[] Values)> Logs { get; } = new();

        protected override void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues) =>
            Logs.Add((group, messageTemplate, propertyValues));

        protected override string GetDisguiseIdentityKey(uint player) => "disguise:active";
    }

    // ---- setting the descriptor

    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void Only_valid_non_DM_players_can_be_given_a_descriptor(bool valid, bool isPlayer, bool isDM)
    {
        _engine.SetReturn("GetIsObjectValid", valid);
        _engine.SetReturn("GetIsPC", isPlayer);
        _engine.SetReturn("GetIsDM", isDM);

        var act = () => _service.SetUnknownDisplayName(PlayerObject, "Tall Stranger");

        act.Should().Throw<ArgumentException>().WithMessage("Unknown display names may only be set for player characters.");
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void An_invalid_name_is_refused_with_the_validation_message()
    {
        var act = () => _service.SetUnknownDisplayName(PlayerObject, "  ");

        act.Should().Throw<ArgumentException>().WithMessage("Please enter a name.");
        _db.Saved.Should().BeEmpty();
        _refreshes.Should().Be(0);
    }

    [Test]
    public void A_valid_descriptor_is_saved_sanitized_logged_and_the_name_overrides_refreshed()
    {
        _db.With(new Player(PlayerId) { UnknownDisplayName = "Old Look" });

        _service.SetUnknownDisplayName(PlayerObject, "  Tall Stranger  ");

        _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>()
            .Which.UnknownDisplayName.Should().Be("Tall Stranger");
        var log = _service.Logs.Should().ContainSingle().Subject;
        log.Group.Should().Be(LogGroup.PublicDescriptors);
        log.Template.Should().StartWith("Base descriptor changed");
        log.Values.Should().Equal(PlayerId, "Canonical Name", PlayerId, "disguise:active", "Old Look", "Tall Stranger");
        _refreshes.Should().Be(1);
    }

    [Test]
    public void A_player_without_a_previous_descriptor_logs_an_empty_one()
    {
        _db.With(new Player(PlayerId) { UnknownDisplayName = null! });

        _service.SetUnknownDisplayName(PlayerObject, "Tall Stranger");

        _service.Logs.Single().Values[4].Should().Be(string.Empty);
    }

    // ---- generating a missing descriptor

    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void A_descriptor_is_only_generated_for_valid_non_DM_players(bool valid, bool isPlayer, bool isDM)
    {
        _engine.SetReturn("GetIsObjectValid", valid);
        _engine.SetReturn("GetIsPC", isPlayer);
        _engine.SetReturn("GetIsDM", isDM);
        _db.With(new Player(PlayerId));

        _service.EnsureUnknownDisplayName(PlayerObject).Should().BeFalse();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void A_player_without_an_id_is_skipped()
    {
        _engine.SetReturn("GetObjectUUID", " ");

        _service.EnsureUnknownDisplayName(PlayerObject).Should().BeFalse();
    }

    [Test]
    public void A_player_record_that_does_not_exist_is_skipped()
    {
        _service.EnsureUnknownDisplayName(PlayerObject).Should().BeFalse();
    }

    [Test]
    public void A_player_who_already_has_a_descriptor_keeps_it()
    {
        _db.With(new Player(PlayerId) { UnknownDisplayName = "Tall Stranger" });

        _service.EnsureUnknownDisplayName(PlayerObject).Should().BeFalse();

        _db.Saved.Should().BeEmpty();
        _service.Logs.Should().BeEmpty();
    }

    [Test]
    public void A_missing_descriptor_is_generated_saved_and_logged()
    {
        _db.With(new Player(PlayerId) { UnknownDisplayName = string.Empty, OriginalAppearanceType = AppearanceType.Human });

        _service.EnsureUnknownDisplayName(PlayerObject).Should().BeTrue();

        var saved = _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>().Subject;
        saved.UnknownDisplayName.Should().EndWith(" Human").And.NotStartWith(" ");
        var log = _service.Logs.Should().ContainSingle().Subject;
        log.Template.Should().StartWith("Base descriptor generated");
        log.Values[4].Should().Be(string.Empty);
        log.Values[5].Should().Be(saved.UnknownDisplayName);
    }

    // ---- generated names

    [Test]
    public void Generated_names_are_stable_for_the_same_player()
    {
        var player = new Player("some-player") { OriginalAppearanceType = AppearanceType.Human };
        player.BaseStats[AbilityType.Might] = 12;

        _service.GenerateUnknownDisplayName(player).Should().Be(_service.GenerateUnknownDisplayName(player));
    }

    [Test]
    public void A_player_with_a_missing_record_gets_a_generic_adjective_and_the_humanoid_species()
    {
        var generated = _service.GenerateUnknownDisplayName(null);

        generated.Should().EndWith(" Humanoid");
    }

    [Test]
    public void The_adjective_follows_the_highest_stat_for_most_players_and_is_generic_for_the_rest()
    {
        var mightAdjectives = new[] { "Strong", "Powerful", "Sturdy", "Athletic", "Broad-Shouldered" };
        var results = Enumerable.Range(0, 300).Select(index =>
        {
            var player = new Player("player-" + index) { OriginalAppearanceType = AppearanceType.Human };
            player.BaseStats[AbilityType.Might] = 9;
            return _service.GenerateUnknownDisplayName(player);
        }).ToList();

        results.Should().OnlyContain(name => name.EndsWith(" Human"));
        var statDriven = results.Count(name => mightAdjectives.Any(adjective => name.StartsWith(adjective + " ")));
        statDriven.Should().BeGreaterThan(150).And.BeLessThan(300, "a quarter of the players get a generic adjective");
        results.Count(name => !mightAdjectives.Any(adjective => name.StartsWith(adjective + " "))).Should().BeGreaterThan(20);
    }

    [Test]
    public void The_highest_stat_wins_and_ties_pick_one_of_the_tied_stats()
    {
        var agilityAdjectives = new[] { "Nimble", "Quick", "Graceful", "Light-Footed", "Lithe" };
        var socialAdjectives = new[] { "Charming", "Commanding", "Well-Spoken", "Poised", "Courteous" };
        var seen = new HashSet<string>();
        for (var index = 0; index < 300; index++)
        {
            var player = new Player("tie-" + index) { OriginalAppearanceType = AppearanceType.Human };
            player.BaseStats[AbilityType.Agility] = 7;
            player.BaseStats[AbilityType.Social] = 7;
            player.BaseStats[AbilityType.Might] = 3;
            var adjective = _service.GenerateUnknownDisplayName(player).Split(' ')[0];
            if (agilityAdjectives.Contains(adjective))
                seen.Add("agility");
            else if (socialAdjectives.Contains(adjective))
                seen.Add("social");
        }

        seen.Should().BeEquivalentTo("agility", "social");
    }

    [Test]
    public void A_player_without_base_stats_or_with_all_zero_stats_gets_generic_adjectives()
    {
        var generic = new[] { "Unfamiliar", "Quiet", "Calm", "Reserved", "Unassuming", "Curious", "Notable", "Steady", "Composed", "Balanced" };
        var noStats = new Player("no-stats") { OriginalAppearanceType = AppearanceType.Human, BaseStats = null! };
        var zeroStats = new Player("zero-stats") { OriginalAppearanceType = AppearanceType.Human };

        generic.Should().Contain(_service.GenerateUnknownDisplayName(noStats).Split(' ')[0]);
        generic.Should().Contain(_service.GenerateUnknownDisplayName(zeroStats).Split(' ')[0]);
    }

    [Test]
    public void A_player_without_an_id_is_seeded_with_the_name()
    {
        var player = new Player { Name = "Seeded" , OriginalAppearanceType = AppearanceType.Human };
        player.Id = " ";

        _service.GenerateUnknownDisplayName(player).Should().EndWith(" Human");
    }

    [TestCase(AppearanceType.Invalid)]
    [TestCase(AppearanceType.DogWolfDire)]
    public void An_appearance_that_is_not_a_species_is_humanoid(AppearanceType appearance)
    {
        var player = new Player("species") { OriginalAppearanceType = appearance };

        _service.GenerateUnknownDisplayName(player).Should().EndWith(" Humanoid");
    }

    [TestCase("Wookiee", "Wookiee")]
    [TestCase("\"(Dynamic) Cathar\"", "Cathar")]
    [TestCase("(dynamic)", "Humanoid")]
    [TestCase("****", "Humanoid")]
    [TestCase("  ", "Humanoid")]
    public void The_species_is_the_cleaned_up_appearance_label(string label, string species)
    {
        _engine.SetReturnHandler("Get2DAString", arguments => label);
        var player = new Player("label") { OriginalAppearanceType = AppearanceType.Human };

        _service.GenerateUnknownDisplayName(player).Should().EndWith(" " + species);
        _engine.CallsTo("Get2DAString").Last().Arguments.Should().Equal("appearance", "LABEL", (int)AppearanceType.Human);
    }

    // ---- reading descriptors

    [Test]
    public void The_descriptor_of_a_loaded_player_is_its_sanitized_name_or_someone()
    {
        _service.GetUnknownDisplayName(new Player { UnknownDisplayName = "  Tall Stranger " }).Should().Be("Tall Stranger");
        _service.GetUnknownDisplayName(new Player { UnknownDisplayName = " " }).Should().Be("Someone");
        _service.GetUnknownDisplayName((Player)null!).Should().Be("Someone");
    }

    [Test]
    public void The_descriptor_of_a_player_object_is_read_from_the_database()
    {
        _db.With(new Player(PlayerId) { UnknownDisplayName = "Tall Stranger" });

        _service.GetUnknownDisplayName(PlayerObject).Should().Be("Tall Stranger");
        _db.Reads.Should().Equal(PlayerId);
    }

    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void An_object_that_is_not_a_player_has_the_default_descriptor(bool valid, bool isPlayer, bool isDM)
    {
        _engine.SetReturn("GetIsObjectValid", valid);
        _engine.SetReturn("GetIsPC", isPlayer);
        _engine.SetReturn("GetIsDM", isDM);
        _db.With(new Player(PlayerId) { UnknownDisplayName = "Tall Stranger" });

        _service.GetUnknownDisplayName(PlayerObject).Should().Be("Someone");
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void The_descriptor_by_player_id_is_read_from_the_database()
    {
        _db.With(new Player("other") { UnknownDisplayName = " Tall Stranger" });

        _service.GetUnknownDisplayNameByPlayerId("other").Should().Be("Tall Stranger");
        _service.GetUnknownDisplayNameByPlayerId("missing").Should().Be("Someone");
        _service.GetUnknownDisplayNameByPlayerId(" ").Should().Be("Someone");
        _service.GetUnknownDisplayNameByPlayerId(null!).Should().Be("Someone");
        _db.Reads.Should().Equal("other", "missing");
    }
}
