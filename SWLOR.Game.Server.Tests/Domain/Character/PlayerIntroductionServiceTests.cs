using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the introduction instance: the engine is a fake with a small world of players (their ids, areas,
/// distances and line of sight), <c>PlayerName</c> is a scripted stand-in and the clock, the disguise, the recast and
/// the log are the instance's own seams.
/// </summary>
[TestFixture]
public class PlayerIntroductionServiceTests
{
    private const uint Presenter = 1001;
    private const uint Observer = 1002;
    private const uint FarObserver = 1003;
    private const uint Dm = 1004;
    private const uint OtherAreaObserver = 1005;
    private const uint HiddenObserver = 1006;
    private const uint AreaOne = 5001;
    private const uint AreaTwo = 5002;
    private const uint ObjectInvalid = 0x7F000000;

    private FakeNWScript _engine = null!;
    private FakeUtilPlugin _util = null!;
    private ScriptedProxy _nameScript = null!;
    private BenchIntroductionService _service = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly List<(uint Observer, uint Target, string Name)> _knownNameWrites = new();
    private readonly Dictionary<string, string> _knownNames = new();
    private readonly Dictionary<uint, string> _ids = new()
    {
        [Presenter] = "presenter-id", [Observer] = "observer-id", [FarObserver] = "far-id", [Dm] = "dm-id",
        [OtherAreaObserver] = "other-area-id", [HiddenObserver] = "hidden-id"
    };

    private string _assignmentError = string.Empty;
    private string _inputError = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _knownNameWrites.Clear();
        _knownNames.Clear();
        _assignmentError = string.Empty;
        _inputError = string.Empty;

        _engine = new FakeNWScript();
        var all = new[] { Presenter, Observer, FarObserver, Dm, OtherAreaObserver, HiddenObserver };
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsPC", arguments => all.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Dm);
        _engine.SetReturnHandler("GetObjectUUID", arguments => _ids.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        _engine.SetReturnHandler("GetArea", arguments => (uint)arguments[0]! == OtherAreaObserver ? AreaTwo : AreaOne);
        _engine.SetReturnHandler("GetDistanceBetween", arguments => (uint)arguments[1]! == FarObserver ? 25f : 5f);
        _engine.SetReturnHandler("GetObjectSeen", arguments => (uint)arguments[1]! != HiddenObserver);
        _engine.SetReturnHandler("GetName", arguments => "Name" + arguments[0]);
        var pcs = new Queue<uint>(all);
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            pcs = new Queue<uint>(all);
            return pcs.Dequeue();
        });
        _engine.SetReturnHandler("GetNextPC", _ => pcs.Count > 0 ? pcs.Dequeue() : ObjectInvalid);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _util = new FakeUtilPlugin();
        _util.SetReturnHandler("StripColors", arguments => System.Text.RegularExpressions.Regex.Replace((string)arguments[0]!, "<c[^>]*>|</c>", string.Empty));
        _scopes.Add(UtilPluginRuntime.Override(_util));

        var names = ScriptedProxy.Create<IPlayerNameService>(out _nameScript);
        _nameScript
            .On("ValidateKnownNameInput", _ => _inputError)
            .On("SanitizeKnownName", arguments => ((string?)arguments[0])?.Trim() ?? string.Empty)
            .On("GetDisplayName", arguments => "Someone" + arguments[1])
            .On("GetColoredDisplayName", arguments => "[" + arguments[1] + "]")
            .On("GetAuditName", arguments => "Audit" + arguments[0])
            .On("ValidateKnownNameAssignment", _ => _assignmentError)
            .On("SetKnownName", arguments =>
            {
                _knownNameWrites.Add(((uint)arguments[0]!, (uint)arguments[1]!, (string)arguments[2]!));
                _knownNames[arguments[0] + ":" + arguments[1]] = (string)arguments[2]!;
                return null;
            })
            .On("TryGetKnownName", arguments =>
            {
                var found = _knownNames.TryGetValue(arguments[0] + ":" + arguments[1], out var name);
                arguments[2] = name ?? string.Empty;
                return found;
            });
        _service = new BenchIntroductionService(names);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    private sealed class BenchIntroductionService : PlayerIntroductionService
    {
        public BenchIntroductionService(IPlayerNameService names)
            : base(names, FacadeForwarder.For<IDisguiseService>(), FacadeForwarder.For<IRecastService>())
        {
        }

        public DateTime Now { get; set; } = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        public string RecastWait { get; set; } = string.Empty;
        public Dictionary<uint, string> IdentityKeys { get; } = new();
        public List<(uint Presenter, float Seconds)> Recasts { get; } = new();
        public List<(LogGroup Group, string Template, object[] Values)> Logs { get; } = new();

        protected override DateTime UtcNow() => Now;

        protected override string GetDisguiseIdentityKey(uint player) => IdentityKeys.GetValueOrDefault(player, "key-" + player);

        protected override (bool, string) IsOnIntroductionRecastDelay(uint presenter) => (RecastWait != string.Empty, RecastWait);

        protected override void ApplyIntroductionRecastDelay(uint presenter, float delaySeconds) => Recasts.Add((presenter, delaySeconds));

        protected override void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues) =>
            Logs.Add((group, messageTemplate, propertyValues));
    }

    private IEnumerable<string> MessagesTo(uint player) => _engine.CallsTo("SendMessageToPC")
        .Where(call => (uint)call.Arguments[0]! == player).Select(call => (string)call.Arguments[1]!);

    // ---- validating

    [TestCase(ObjectInvalid)]
    [TestCase(Dm)]
    [TestCase(12345u)]
    public void Only_player_characters_can_introduce_themselves(uint presenter)
    {
        _service.ValidateIntroduction(presenter, "Kira").Should().Be("Only player characters can introduce themselves.");
    }

    [Test]
    public void A_name_the_name_service_refuses_is_returned_as_the_error()
    {
        _inputError = "Names may not contain color codes.";

        _service.ValidateIntroduction(Presenter, "Kira").Should().Be("Names may not contain color codes.");
    }

    [Test]
    public void An_introduction_on_cooldown_says_how_long_to_wait()
    {
        _service.RecastWait = "1 minute";

        _service.ValidateIntroduction(Presenter, "Kira").Should().Be("You can introduce yourself again in 1 minute.");
    }

    [Test]
    public void A_valid_introduction_has_no_error()
    {
        _service.ValidateIntroduction(Presenter, "Kira").Should().BeEmpty();
    }

    // ---- introducing

    [Test]
    public void A_refused_introduction_tells_the_presenter_in_red_and_does_nothing_else()
    {
        _inputError = "Please enter a name.";

        _service.Introduce(Presenter, " ");

        MessagesTo(Presenter).Should().ContainSingle().Which.Should().Contain("Please enter a name.");
        _service.Recasts.Should().BeEmpty();
        _service.GetPending(Observer).Should().BeEmpty();
    }

    [Test]
    public void An_introduction_reaches_only_nearby_visible_players_in_the_same_area()
    {
        _service.Introduce(Presenter, "  Kira  ");

        _service.GetPending(Observer).Should().ContainSingle().Which.Should().Match<SWLOR.Game.Server.Domain.Character.PlayerIntroductionOffer>(
            offer => offer.Name == "Kira" && offer.Presenter == Presenter && offer.PresenterPlayerId == "presenter-id" &&
                     offer.IdentityKey == "key-" + Presenter && offer.DisplayName == "Someone" + Presenter &&
                     offer.ExpiresAt == _service.Now.AddMinutes(10));
        _service.GetPending(FarObserver).Should().BeEmpty("too far away");
        _service.GetPending(OtherAreaObserver).Should().BeEmpty("in another area");
        _service.GetPending(HiddenObserver).Should().BeEmpty("cannot see the presenter");
        _service.GetPending(Dm).Should().BeEmpty("DMs are skipped");
        _service.GetPending(Presenter).Should().BeEmpty("the presenter is not an observer of themselves");
    }

    [Test]
    public void The_observer_is_told_and_the_presenter_gets_a_confirmation_and_a_cooldown()
    {
        _service.Introduce(Presenter, "Kira");

        MessagesTo(Observer).Should().ContainSingle().Which.Should().Contain("[1001] introduces themselves as 'Kira'.")
            .And.Contain("/introductions");
        MessagesTo(Presenter).Should().ContainSingle().Which.Should().Contain("You introduced yourself as 'Kira' to nearby players.");
        _service.Recasts.Should().Equal((Presenter, 120f));
    }

    [Test]
    public void The_same_introduction_is_not_announced_to_an_observer_twice()
    {
        _service.Introduce(Presenter, "Kira");
        _service.Introduce(Presenter, "Kira");

        MessagesTo(Observer).Should().ContainSingle();
        _service.GetPending(Observer).Should().ContainSingle();
    }

    [Test]
    public void A_new_name_replaces_the_older_offer_with_a_new_id()
    {
        _service.Introduce(Presenter, "Kira");
        var first = _service.GetPending(Observer).Single();

        _service.Introduce(Presenter, "Kira Vos");

        var second = _service.GetPending(Observer).Should().ContainSingle().Subject;
        second.Id.Should().NotBe(first.Id);
        second.Name.Should().Be("Kira Vos");
    }

    [Test]
    public void Offers_expire_after_ten_minutes()
    {
        _service.Introduce(Presenter, "Kira");

        _service.Now = _service.Now.AddMinutes(10);

        _service.GetPending(Observer).Should().BeEmpty();
    }

    // ---- pending offers

    [Test]
    public void An_offer_from_a_presenter_whose_identity_changed_is_dropped_when_listed()
    {
        _service.Introduce(Presenter, "Kira");

        _service.IdentityKeys[Presenter] = "disguise:other";

        _service.GetPending(Observer).Should().BeEmpty();
        _service.IdentityKeys[Presenter] = "key-" + Presenter;
        _service.GetPending(Observer).Should().BeEmpty("the dropped offer does not come back");
    }

    [Test]
    public void An_offer_from_a_presenter_who_left_is_dropped_when_listed()
    {
        _service.Introduce(Presenter, "Kira");

        _ids[Presenter] = "someone-else";

        _service.GetPending(Observer).Should().BeEmpty();
    }

    // ---- accepting and dismissing

    [Test]
    public void Accepting_remembers_the_name_logs_it_and_removes_the_offer()
    {
        _service.Introduce(Presenter, "Kira");
        var offer = _service.GetPending(Observer).Single();

        var error = _service.Accept(Observer, offer.Id, string.Empty);

        error.Should().BeEmpty();
        _knownNameWrites.Should().Equal((Observer, Presenter, "Kira"));
        _service.GetPending(Observer).Should().BeEmpty();
        var log = _service.Logs.Should().ContainSingle().Subject;
        log.Group.Should().Be(LogGroup.PrivateNames);
        log.Values.Should().Equal("introduction-accepted", "observer-id", "Audit" + Observer, "presenter-id",
            "Audit" + Presenter, "key-" + Presenter, string.Empty, "Kira");
    }

    [Test]
    public void Accepting_logs_the_name_the_observer_had_before()
    {
        _knownNames[Observer + ":" + Presenter] = "Old Label";
        _service.Introduce(Presenter, "Kira");
        var offer = _service.GetPending(Observer).Single();

        _service.Accept(Observer, offer.Id, "Old Label").Should().BeEmpty();

        _service.Logs.Single().Values[6].Should().Be("Old Label");
    }

    [Test]
    public void Accepting_after_the_private_label_changed_is_refused()
    {
        _knownNames[Observer + ":" + Presenter] = "Changed Label";
        _service.Introduce(Presenter, "Kira");
        var offer = _service.GetPending(Observer).Single();

        _service.Accept(Observer, offer.Id, "Old Label").Should().Be("Your private label changed. Review the introduction again before remembering it.");

        _knownNameWrites.Should().BeEmpty();
        _service.GetPending(Observer).Should().ContainSingle("a refused offer stays pending");
    }

    [Test]
    public void Accepting_an_offer_the_name_service_rejects_returns_its_error()
    {
        _service.Introduce(Presenter, "Kira");
        var offer = _service.GetPending(Observer).Single();
        _assignmentError = "You already use that name for another character who is online.";

        _service.Accept(Observer, offer.Id, string.Empty).Should().Be(_assignmentError);

        _knownNameWrites.Should().BeEmpty();
    }

    [Test]
    public void Accepting_an_unknown_offer_is_refused()
    {
        _service.Accept(Observer, Guid.NewGuid(), string.Empty).Should().Be("This introduction is no longer available.");
    }

    [Test]
    public void Accepting_when_the_presenter_is_no_longer_the_same_identity_is_refused()
    {
        _service.Introduce(Presenter, "Kira");
        var offer = _service.GetPending(Observer).Single();
        _service.IdentityKeys[Presenter] = "disguise:other";

        _service.Accept(Observer, offer.Id, string.Empty).Should().Be("This introduction is no longer available.");
    }

    [Test]
    public void Dismissing_removes_an_offer_only_for_that_observer()
    {
        _service.Introduce(Presenter, "Kira");
        var offer = _service.GetPending(Observer).Single();

        _service.Dismiss(Observer, offer.Id);

        _service.GetPending(Observer).Should().BeEmpty();
    }

    [Test]
    public void Leaving_the_module_clears_the_offers_the_player_made_and_received()
    {
        _service.Introduce(Presenter, "Kira");
        _engine.SetReturnHandler("GetExitingObject", _ => Presenter);

        _service.ClearOnExit();

        _service.GetPending(Observer).Should().BeEmpty();
    }

    [Test]
    public void A_player_without_an_id_leaving_changes_nothing()
    {
        _service.Introduce(Presenter, "Kira");
        _engine.SetReturnHandler("GetExitingObject", _ => 12345u);

        _service.ClearOnExit();

        _service.GetPending(Observer).Should().ContainSingle();
    }

    [Test]
    public void The_default_seams_read_the_real_clock()
    {
        var plain = new TestableDefaultSeams();

        plain.Clock().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private sealed class TestableDefaultSeams : PlayerIntroductionService
    {
        public TestableDefaultSeams()
            : base(null!, FacadeForwarder.For<IDisguiseService>(), FacadeForwarder.For<IRecastService>())
        {
        }

        public DateTime Clock() => UtcNow();
    }
}
