using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The defense memory part of <c>Combat</c>: who was guarded or deflected recently. The clock is held still and the
/// engine answers only whether a creature is valid.
/// </summary>
[TestFixture]
public class CombatDefenseMemoryServiceTests
{
    private const uint Defender = 22;
    private const uint Other = 33;
    private const uint Gone = 44;

    private readonly List<IDisposable> _scopes = new();
    private DateTime _now;
    private CombatDefenseMemoryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID or Gone));
        _scopes.Add(NWScriptRuntime.Override(engine));
        _service = new CombatDefenseMemoryService();
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    [Test]
    public void ARecentGuardedHitIsReadWithinItsWindowAndForgottenAfter()
    {
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeFalse();
        _service.NoteGuardedHit(Defender);
        _service.HasRecentGuardedHit(Gone, 6f).Should().BeFalse();
        _service.HasRecentGuardedHit(Defender, 0f).Should().BeFalse();

        _now = _now.AddSeconds(6);
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeTrue();
        _now = _now.AddSeconds(1);
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeFalse();
        _now = _now.AddSeconds(-1);
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeFalse("the stale entry was purged by the read that found it old");
    }

    [Test]
    public void ARecentDeflectionIsReadPerSourceWithinItsWindowAndForgottenAfter()
    {
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse();
        _service.NoteDeflection(Defender, DeflectionSource.Melee);
        _service.HasRecentDeflection(Gone, DeflectionSource.Melee, 6f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.None, 6f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 0f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.Ranged, 6f).Should().BeFalse();

        _now = _now.AddSeconds(6);
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeTrue();
        _now = _now.AddSeconds(1);
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse();
        _now = _now.AddSeconds(-1);
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse("the stale entry was purged by the read that found it old");
    }

    [Test]
    public void ForgettingACreatureDropsOnlyItsGuardedHitAndDeflections()
    {
        _service.NoteGuardedHit(Defender);
        _service.NoteGuardedHit(Other);
        _service.NoteDeflection(Defender, DeflectionSource.Melee);
        _service.NoteDeflection(Defender, DeflectionSource.Ranged);
        _service.NoteDeflection(Other, DeflectionSource.Melee);

        _service.ForgetDefenseMemory(Defender);

        _service.HasRecentGuardedHit(Defender, 6f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.Ranged, 6f).Should().BeFalse();
        _service.HasRecentGuardedHit(Other, 6f).Should().BeTrue();
        _service.HasRecentDeflection(Other, DeflectionSource.Melee, 6f).Should().BeTrue();
    }

    [Test]
    public void ANoteOverwritesTheEarlierOneSoTheWindowRestarts()
    {
        _service.NoteGuardedHit(Defender);
        _service.NoteDeflection(Defender, DeflectionSource.Melee);
        _now = _now.AddSeconds(5);
        _service.NoteGuardedHit(Defender);
        _service.NoteDeflection(Defender, DeflectionSource.Melee);
        _now = _now.AddSeconds(5);

        _service.HasRecentGuardedHit(Defender, 6f).Should().BeTrue();
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeTrue();
    }
}
