using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The carried statuses when a hot reload has activated a new build (P4-15 closure audit): the old build's closures that settled an
/// ended status, kept a status's own checks going and recomputed the movement rate were dropped, so adoption does their work.
/// </summary>
[TestFixture]
public class StatusEffectServiceReloadTests : StatusEffectServiceTestBase
{
    private const uint Online = 500;

    [SetUp]
    public void NobodyOnline()
    {
        Bench.Engine.SetReturn("GetFirstPC", Invalid);
        Bench.Engine.SetReturn("GetNextPC", Invalid);
    }

    [Test]
    public void Adoption_settles_a_status_whose_native_effect_ended_and_resumes_the_others()
    {
        Applied<ProbeEffect>(Target, Caster, effect => effect.NameValue = "Expired");
        var running = Applied<ResumingProbe>(Target, Caster);
        Bench.NativeTags.Add(running.Id);

        Service.OnCarriedStateAdopted();

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().ContainSingle().Which.Should().BeSameAs(running);
        Log.Should().Contain($"Remove:Expired:{Target}:False", "its settle closure would have removed it");
        running.Resumed.Should().Equal(new[] { Target });
        Log.Should().Contain($"ApplyCreatureMovementRate:{Target}");
    }

    [Test]
    public void Adoption_recomputes_the_movement_rate_of_an_online_player_whose_last_status_just_ended()
    {
        Bench.Players.Add(Online);
        Bench.Engine.SetReturn("GetFirstPC", Online);

        Service.OnCarriedStateAdopted();

        Log.Should().Contain($"ApplyCreatureMovementRate:{Online}");
    }

    private sealed class ResumingProbe : ProbeEffect
    {
        public List<uint> Resumed { get; } = new();

        public override void ResumeAfterReload(uint creature) => Resumed.Add(creature);
    }
}
