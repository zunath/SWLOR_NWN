using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat log lines of <c>Combat</c>: which observer gets which line, the names each observer sees, and the floating
/// text that only testing builds show. The engine is a recording fake; the player name, messaging and feedback services
/// are scripted proxies that record what the service asked of them.
/// </summary>
[TestFixture]
public class CombatFeedbackServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Observer = 33;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private ScriptedProxy _names = null!;
    private ScriptedProxy _messaging = null!;
    private ScriptedProxy _feedback = null!;
    private CombatFeedbackService _service = null!;
    private HashSet<uint> _players = null!;
    private HashSet<uint> _valid = null!;
    private List<EffectTypeScript> _effects = null!;

    [SetUp]
    public void SetUp()
    {
        _players = new HashSet<uint>();
        _valid = new HashSet<uint> { Attacker, Defender, Observer };
        _effects = new List<EffectTypeScript>();
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsObjectValid", args => _valid.Contains((uint)args[0]!));
        var cursor = 0;
        _engine.SetReturnHandler("GetFirstEffect", _ => { cursor = 0; return EngineStructures.Create<Effect>(_effects.Count > 0 ? 1 : 0); });
        _engine.SetReturnHandler("GetNextEffect", _ => { cursor++; return EngineStructures.Create<Effect>(cursor < _effects.Count ? cursor + 1 : 0); });
        _engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        _engine.SetReturnHandler("GetEffectType", _ => _effects[cursor]);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var names = ScriptedProxy.Create<IPlayerNameService>(out _names);
        _names.On(nameof(IPlayerNameService.GetColoredDisplayName), args => $"[{args[1]} to {args[0]}]");
        var messaging = ScriptedProxy.Create<IMessagingService>(out _messaging);
        _messaging.On(nameof(IMessagingService.SendMessageNearbyToPlayers), _ => null);
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out _feedback);
        _feedback.On(nameof(IPlayerFeedbackService.ShowDiagnosticFloatingText), _ => null);
        _service = new CombatFeedbackService(names, messaging, feedback);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private IReadOnlyList<(uint Player, string Message)> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!)).ToArray();

    private static string Combat(string text) => ColorToken.Combat(text);

    [Test]
    public void AGuardedHitTellsBothPlayersAndFloatsTheGuardForTheDefender()
    {
        _players.Add(Attacker);
        _players.Add(Defender);

        _service.SendGuardedHitFeedback(Defender, Attacker, 9);

        Messages().Should().Equal(
            (Defender, Combat("[22 to 22] guards against [11 to 22]'s attack, preventing 9 damage.")),
            (Attacker, Combat("[22 to 11] guards against [11 to 11]'s attack, preventing 9 damage.")));
        var floating = _feedback.Calls.Should().ContainSingle().Subject;
        floating.Arguments.Should().Equal(Combat("Guard (-9)"), Defender, false);
    }

    [Test]
    public void AGuardedHitBetweenCreaturesSendsNothing()
    {
        _service.SendGuardedHitFeedback(Defender, Attacker, 9);

        Messages().Should().BeEmpty();
        _feedback.Calls.Should().BeEmpty();
    }

    [Test]
    public void CriticalWardTellsTheDefenderAndTheAttackerWhoIsAnotherPlayer()
    {
        _players.Add(Attacker);
        _players.Add(Defender);

        _service.SendIncomingCriticalHitDowngradeFeedback(Attacker, Defender);

        Messages().Should().Equal(
            (Defender, Combat("[22 to 22]'s Critical Ward negates [11 to 22]'s critical hit.")),
            (Attacker, Combat("[22 to 11]'s Critical Ward negates [11 to 11]'s critical hit.")));
        _feedback.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Combat("Critical Ward"), Defender, false);
    }

    [Test]
    public void CriticalWardWithoutAnotherAttackerNamesOnlyTheDefender()
    {
        _players.Add(Defender);

        _service.SendIncomingCriticalHitDowngradeFeedback(Defender, Defender);
        _valid.Remove(Attacker);
        _service.SendIncomingCriticalHitDowngradeFeedback(Attacker, Defender);

        Messages().Should().Equal(
            (Defender, Combat("[22 to 22]'s Critical Ward negates the critical hit.")),
            (Defender, Combat("[22 to 22]'s Critical Ward negates the critical hit.")));
    }

    [Test]
    public void CriticalWardOnAnInvalidDefenderSendsNothing()
    {
        _players.Add(Defender);
        _valid.Remove(Defender);

        _service.SendIncomingCriticalHitDowngradeFeedback(Attacker, Defender);

        Messages().Should().BeEmpty();
        _feedback.Calls.Should().BeEmpty();
    }

    [TestCase(1, ": *hit*")]
    [TestCase(7, ": *hit*")]
    [TestCase(3, ": *critical*")]
    [TestCase(4, ": *miss*")]
    [TestCase(2, "")]
    public void AnAttackLineNamesTheResultAndTheHitChance(int result, string type)
    {
        _service.BuildCombatLogMessage(Observer, Attacker, Defender, result, 63)
            .Should().Be(Combat($"[11 to 33] attacks [22 to 33]{type} : (63% chance to hit)"));
    }

    [TestCase(1, ": *hit*")]
    [TestCase(7, ": *hit*")]
    [TestCase(3, ": *critical*")]
    [TestCase(4, ": *miss*")]
    [TestCase(9, "")]
    public void AnAbilityLineNamesTheAbilityTheResultAndTheHitChance(int result, string type)
    {
        _service.BuildAbilityCombatLogMessage(Observer, Attacker, Defender, "Rapid Shot", result, 80)
            .Should().Be(Combat($"[11 to 33] uses Rapid Shot on [22 to 33]{type} : (80% chance to hit)"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void AnAbilityWithoutANameIsAnAbility(string? name)
    {
        _service.BuildAbilityCombatLogMessage(Observer, Attacker, Defender, name!, 4, 10)
            .Should().Be(Combat("[11 to 33] uses an ability on [22 to 33]: *miss* : (10% chance to hit)"));
        _service.BuildAbilityNoTargetCombatLogMessage(Observer, Attacker, name!)
            .Should().Be(Combat("[11 to 33] uses an ability, but it hits no targets."));
        _service.BuildAbilityCriticalHitCombatLogMessage(Observer, Attacker, Defender, name!)
            .Should().Be(Combat("[11 to 33]'s Ability critically hits [22 to 33]."));
    }

    [Test]
    public void AnAbilityThatHitsNothingAndAnAbilityCriticalNameTheAbility()
    {
        _service.BuildAbilityNoTargetCombatLogMessage(Observer, Attacker, "Shockwave")
            .Should().Be(Combat("[11 to 33] uses Shockwave, but it hits no targets."));
        _service.BuildAbilityCriticalHitCombatLogMessage(Observer, Attacker, Defender, "Shockwave")
            .Should().Be(Combat("[11 to 33]'s Shockwave critically hits [22 to 33]."));
    }

    [Test]
    public void AnAbilityCriticalGoesToPlayersNearTheDefenderEachWithTheirOwnNames()
    {
        _service.SendAbilityCriticalHitFeedback(Attacker, Defender, "Shockwave");

        var call = _messaging.Calls.Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(Defender);
        call.Arguments[2].Should().Be(60f);
        var build = (BuildMessageDelegate)call.Arguments[1]!;
        build(Observer).Should().Be(Combat("[11 to 33]'s Shockwave critically hits [22 to 33]."));
    }

    [Test]
    public void AnAbilityCriticalWithAnInvalidSideSendsNothing()
    {
        _valid.Remove(Attacker);
        _service.SendAbilityCriticalHitFeedback(Attacker, Defender, "Shockwave");
        _valid.Add(Attacker);
        _valid.Remove(Defender);
        _service.SendAbilityCriticalHitFeedback(Attacker, Defender, "Shockwave");

        _messaging.Calls.Should().BeEmpty();
    }

    [Test]
    public void TemporaryHPDamageIsReportedOnlyWhileTheDefenderHasTemporaryHP()
    {
        _effects.Add(EffectTypeScript.Haste);
        _service.SendTemporaryHitPointDamageFeedback(Attacker, Defender, 5);
        _messaging.Calls.Should().BeEmpty("no effect is temporary hit points");

        _effects.Add(EffectTypeScript.TemporaryHitpoints);
        _service.SendTemporaryHitPointDamageFeedback(Attacker, Defender, 5);

        var call = _messaging.Calls.Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(Defender);
        call.Arguments[2].Should().Be(60f);
        var build = (BuildMessageDelegate)call.Arguments[1]!;
        build(Observer).Should().Be(Combat("[11 to 33] deals 5 damage against [22 to 33]'s temporary HP."));
        _valid.Remove(Attacker);
        build(Observer).Should().Be(Combat("[22 to 33]'s temporary HP absorbs 5 damage."));
    }

    [Test]
    public void TemporaryHPDamageAgainstOneselfNamesOnlyTheDefender()
    {
        _effects.Add(EffectTypeScript.TemporaryHitpoints);

        _service.SendTemporaryHitPointDamageFeedback(Defender, Defender, 4);

        var build = (BuildMessageDelegate)_messaging.Calls.Single().Arguments[1]!;
        build(Observer).Should().Be(Combat("[22 to 33]'s temporary HP absorbs 4 damage."));
    }

    [TestCase(0, true)]
    [TestCase(-3, true)]
    [TestCase(5, false)]
    public void TemporaryHPDamageNeedsDamageAndAValidDefender(int damage, bool defenderValid)
    {
        _effects.Add(EffectTypeScript.TemporaryHitpoints);
        if (!defenderValid)
            _valid.Remove(Defender);

        _service.SendTemporaryHitPointDamageFeedback(Attacker, Defender, damage);

        _messaging.Calls.Should().BeEmpty();
    }
}
