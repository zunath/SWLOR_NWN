using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The notice stealth sends the creature AI when a detection reveals a stealthed player: who is told, in what order, and the
/// loud failure when nobody registered.
/// </summary>
[TestFixture]
public class StealthAggroListenerTests
{
    private const uint Observer = 11;
    private const uint Target = 12;

    private static ListenerStealth CreateStealth() => new();

    [Test]
    public void A_detection_is_handed_to_every_registered_listener_in_registration_order()
    {
        var stealth = CreateStealth();
        var calls = new List<string>();
        stealth.RegisterAggroListener(new RecordingListener("first", calls));
        stealth.RegisterAggroListener(new RecordingListener("second", calls));

        stealth.NotifyDetection(Observer, Target);

        calls.Should().Equal($"first:detected({Observer}, {Target})", $"second:detected({Observer}, {Target})");
    }

    [Test]
    public void A_player_becoming_visible_is_handed_to_every_registered_listener()
    {
        var stealth = CreateStealth();
        var calls = new List<string>();
        stealth.RegisterAggroListener(new RecordingListener("only", calls));

        stealth.NotifyVisible(Target);

        calls.Should().Equal($"only:visible({Target})");
    }

    [Test]
    public void Registering_the_same_listener_twice_keeps_one_entry()
    {
        var stealth = CreateStealth();
        var calls = new List<string>();
        var listener = new RecordingListener("only", calls);
        stealth.RegisterAggroListener(listener);
        stealth.RegisterAggroListener(listener);

        stealth.NotifyDetection(Observer, Target);

        calls.Should().ContainSingle();
    }

    [Test]
    public void Notifying_with_no_listener_registered_fails_at_once()
    {
        var stealth = CreateStealth();

        var detection = () => stealth.NotifyDetection(Observer, Target);
        var visible = () => stealth.NotifyVisible(Target);

        detection.Should().Throw<InvalidOperationException>().WithMessage("*aggro listener*");
        visible.Should().Throw<InvalidOperationException>().WithMessage("*aggro listener*");
    }

    private sealed class RecordingListener : IStealthAggroListener
    {
        private readonly string _name;
        private readonly List<string> _calls;

        public RecordingListener(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public void TryAcquireAggroAfterDetection(uint observer, uint target) => _calls.Add($"{_name}:detected({observer}, {target})");

        public void AcquireAggroFromNearbyEnemies(uint target) => _calls.Add($"{_name}:visible({target})");
    }

    /// <summary>The real service with its two notice seams exposed and nothing else overridden.</summary>
    private sealed class ListenerStealth : StealthService
    {
        public ListenerStealth()
            : base(FacadeForwarder.For<IPerkService>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IStatusEffectService>(), FacadeForwarder.LazyFor<IStatusEffectApplicationService>(), FacadeForwarder.LazyFor<IEnmityService>(),
                FacadeForwarder.LazyFor<IEspionageInfiltrationService>())
        {
        }

        public void NotifyDetection(uint observer, uint target) => TryAcquireAggroAfterDetection(observer, target);

        public void NotifyVisible(uint target) => AcquireAggroFromNearbyEnemies(target);
    }
}
