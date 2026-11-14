using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The queued swing animation instance on the engine runtime: a creature gets the clip on every melee swing key and a
/// session token, a stop restores the keys and tells the equipment owners, and a waiting ability owns the swing keys.
/// The engine is a recording fake with real local variables.
/// </summary>
[TestFixture]
public class QueuedAttackAnimationServiceTests
{
    private const uint Creature = 1001;
    private const uint Invalid = 0x7F000000;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private QueuedAttackAnimationService _service = null!;
    private readonly List<uint> _restored = new();

    [SetUp]
    public void SetUp()
    {
        _restored.Clear();
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        _engine.SetReturnHandler("GetObjectType", _ => ObjectType.Creature);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _service = new QueuedAttackAnimationService();
        _service.AddRestoreEquipmentAnimationsHandler(_restored.Add);
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    private IEnumerable<(string Source, string Replacement)> Replacements() =>
        _engine.CallsTo("ReplaceObjectAnimation").Select(call => ((string)call.Arguments[1]!, (string)call.Arguments[2]!));

    [Test]
    public void BeginMapsEverySwingKeyToTheClipAndOwnsThem()
    {
        _service.Begin(Creature, AuthoredAnimation.ShieldBash);

        var mapped = Replacements().Where(pair => pair.Replacement == AuthoredAnimation.ShieldBash.Name).Select(pair => pair.Source).ToList();
        mapped.Should().BeEquivalentTo(_service.GetActiveSwingKeys(Creature));
        mapped.Should().HaveCount(28).And.Contain("1hslashl").And.Contain("plreach");
        _locals.String(Creature, "_QUEUED_ATTACK_ANIMATION").Should().NotBeEmpty();
        _engine.CountOf("AssignCommand").Should().Be(1, "the 30 second fallback is scheduled on the module");
    }

    [Test]
    public void ACreatureWithoutAWaitingClipOwnsNoSwingKeys()
    {
        _service.GetActiveSwingKeys(Creature).Should().BeEmpty();
    }

    [Test]
    public void StopRestoresTheKeysAndNotifiesTheEquipmentOwners()
    {
        _service.Begin(Creature, AuthoredAnimation.ShieldBash);

        _service.Stop(Creature);

        Replacements().Count(pair => pair.Replacement == string.Empty).Should().Be(28);
        _locals.String(Creature, "_QUEUED_ATTACK_ANIMATION").Should().BeEmpty();
        _service.GetActiveSwingKeys(Creature).Should().BeEmpty();
        _restored.Should().Equal(Creature);
    }

    [Test]
    public void BeginningAgainReplacesThePreviousClip()
    {
        _service.Begin(Creature, AuthoredAnimation.ShieldBash);

        _service.Begin(Creature, AuthoredAnimation.RiotBlade);

        _restored.Should().Equal(Creature);
        Replacements().Last().Replacement.Should().Be(AuthoredAnimation.RiotBlade.Name);
        _service.GetActiveSwingKeys(Creature).Should().NotBeEmpty();
    }

    [Test]
    public void BeginningWithoutAClipOnlyStops()
    {
        _service.Begin(Creature, AuthoredAnimation.ShieldBash);

        _service.Begin(Creature, null!);

        _service.GetActiveSwingKeys(Creature).Should().BeEmpty();
        _restored.Should().Equal(Creature);
    }

    [Test]
    public void StoppingACreatureThatIsNotValidDoesNothing()
    {
        _service.Stop(Invalid);

        _engine.CountOf("ReplaceObjectAnimation").Should().Be(0);
        _restored.Should().BeEmpty();
    }

    [Test]
    public void ARemovedHandlerIsNotNotified()
    {
        _service.RemoveRestoreEquipmentAnimationsHandler(_restored.Add);
        _service.Begin(Creature, AuthoredAnimation.ShieldBash);

        _service.Stop(Creature);

        _restored.Should().BeEmpty();
    }

    [Test]
    public void ThePlaybackRuntimeReadsAndClearsTheCreatureActions()
    {
        var swingKeys = _service.GetActiveSwingKeys(Creature);
        swingKeys.Should().BeEmpty();
        var playback = new QueuedAttackAnimationPlayback(new NativeRuntimeProbe());
        playback.SwingKeys.Should().HaveCount(28).And.OnlyHaveUniqueItems();
    }

    [Test]
    public void TheAccessorEventHandlersAndMembersReachTheContainersInstance()
    {
        using var container = new FacadeContainer();
        using var scope = container.Install();
        var seen = new List<uint>();
        void Handler(uint creature) => seen.Add(creature);

        QueuedAttackAnimation.AddRestoreEquipmentAnimationsHandler(Handler);
        QueuedAttackAnimation.AddRestoreEquipmentAnimationsHandler(Handler);
        QueuedAttackAnimation.Begin(Creature, AuthoredAnimation.ShieldBash);
        QueuedAttackAnimation.GetActiveSwingKeys(Creature).Should().NotBeEmpty();
        QueuedAttackAnimation.Stop(Creature);
        seen.Should().Equal(Creature, Creature);

        QueuedAttackAnimation.RemoveRestoreEquipmentAnimationsHandler(Handler);
        QueuedAttackAnimation.RemoveRestoreEquipmentAnimationsHandler(Handler);
        QueuedAttackAnimation.Begin(Creature, AuthoredAnimation.ShieldBash);
        QueuedAttackAnimation.Stop(Creature);
        seen.Should().HaveCount(2);
    }

    private sealed class NativeRuntimeProbe : INamedAnimationRuntime
    {
        public bool IsValid(uint creature) => true;
        public string GetToken(uint creature) => string.Empty;
        public void SetToken(uint creature, string token) { }
        public void Replace(uint creature, string source, string replacement) { }
        public ActionType CurrentAction(uint creature) => ActionType.Invalid;
        public void ClearActions(uint creature) { }
        public void Schedule(float seconds, Action callback) { }
    }
}
