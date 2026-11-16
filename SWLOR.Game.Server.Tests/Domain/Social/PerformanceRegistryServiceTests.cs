using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>The live sessions and the preferences, over a fake engine that names the players.</summary>
[TestFixture]
public class PerformanceRegistryServiceTests
{
    private const uint Player = 1001;
    private const uint Other = 1002;

    private IDisposable _scope = null!;
    private PerformanceRegistryService _registry = null!;

    [SetUp]
    public void SetUp()
    {
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        _scope = NWScriptRuntime.Override(engine);
        _registry = new PerformanceRegistryService();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private static PerformanceSession Session(uint performer) =>
        new(performer, PerformanceKind.Dance, PerformanceInstrumentFamily.None, PerformanceTempo.Relaxed, DateTime.UtcNow, new System.Random(1));

    [Test]
    public void ASessionIsFoundUntilItIsRemoved()
    {
        var session = Session(Player);

        _registry.Add(session);

        _registry.Count.Should().Be(1);
        _registry.IsPerforming(Player).Should().BeTrue();
        _registry.IsPerforming(Other).Should().BeFalse();
        _registry.TryGetSession(Player, out var found).Should().BeTrue();
        found.Should().BeSameAs(session);
        _registry.Remove(Player, out var removed).Should().BeTrue();
        removed.Should().BeSameAs(session);
        _registry.Remove(Player, out _).Should().BeFalse();
        _registry.Count.Should().Be(0);
    }

    [Test]
    public void TheSessionListIsACopy()
    {
        _registry.Add(Session(Player));

        var sessions = _registry.GetSessions();
        _registry.Remove(Player, out _);

        sessions.Should().ContainSingle();
        _registry.GetSessions().Should().BeEmpty();
    }

    [Test]
    public void APlayersPreferencesAreCreatedOnceAndKeptPerPlayer()
    {
        var first = _registry.GetPreference(Player);
        first.Roleplay = true;

        _registry.GetPreference(Player).Should().BeSameAs(first);
        _registry.GetPreference(Other).Should().NotBeSameAs(first);
        _registry.GetPreference(Other).Roleplay.Should().BeFalse();
    }
}
