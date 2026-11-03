using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceCueGeneratorTests
{
    private const int SampleSize = 4000;

    private static List<PerformanceBeat> Generate(
        PerformanceKind kind, PerformanceTempo tempo, PerformanceInstrumentFamily family, int seed = 11)
    {
        var random = new Random(seed);
        var beats = new List<PerformanceBeat>();
        PerformanceBeat? previous = null;
        for (var index = 0; index < SampleSize; index++)
        {
            var beat = PerformanceCueGenerator.Next(random, kind, tempo, family, previous);
            beats.Add(beat);
            previous = beat;
        }

        return beats;
    }

    [Test]
    public void Dance_UsesOnlyStepLanesWithNoRestsOrChords()
    {
        var beats = Generate(PerformanceKind.Dance, PerformanceTempo.Upbeat, PerformanceInstrumentFamily.None);

        beats.Should().OnlyContain(beat => !beat.IsRest && !beat.IsChord);
        beats.Select(beat => beat.Primary).Distinct().Should().BeEquivalentTo(new[]
        {
            PerformanceCue.Left, PerformanceCue.Up, PerformanceCue.Down, PerformanceCue.Right
        });
    }

    [Test]
    public void Music_UsesOnlyNoteLanes()
    {
        var beats = Generate(PerformanceKind.Music, PerformanceTempo.Upbeat, PerformanceInstrumentFamily.Strings);

        beats.SelectMany(beat => beat.Second.HasValue ? new[] { beat.Primary, beat.Second.Value } : new[] { beat.Primary })
            .Distinct()
            .Should().BeEquivalentTo(new[] { PerformanceCue.Low, PerformanceCue.Mid, PerformanceCue.High });
    }

    [Test]
    public void Chords_AppearOnlyInMusicAtUpbeatAboutTwentyPercentOfTheTime()
    {
        Generate(PerformanceKind.Music, PerformanceTempo.Relaxed, PerformanceInstrumentFamily.Strings)
            .Should().OnlyContain(beat => !beat.IsChord);
        Generate(PerformanceKind.Music, PerformanceTempo.Lively, PerformanceInstrumentFamily.Strings)
            .Should().OnlyContain(beat => !beat.IsChord);

        var upbeat = Generate(PerformanceKind.Music, PerformanceTempo.Upbeat, PerformanceInstrumentFamily.Strings);
        var chords = upbeat.Where(beat => beat.IsChord).ToList();

        ((double)chords.Count / upbeat.Count).Should().BeInRange(0.16, 0.24);
        chords.Should().OnlyContain(beat => beat.Primary != beat.Second);
    }

    [Test]
    public void Strings_PlayACueEveryBeatWithOccasionalSameLaneRepeats()
    {
        var beats = Generate(PerformanceKind.Music, PerformanceTempo.Relaxed, PerformanceInstrumentFamily.Strings);

        beats.Should().OnlyContain(beat => !beat.IsRest);
        var repeats = beats.Zip(beats.Skip(1), (a, b) => a.Primary == b.Primary).Count(same => same);
        ((double)repeats / (beats.Count - 1)).Should().BeInRange(0.20, 0.30);
    }

    [Test]
    public void Horns_HoldSomeBeatsAsRestsNeverBackToBackAndNeverFirst()
    {
        var beats = Generate(PerformanceKind.Music, PerformanceTempo.Relaxed, PerformanceInstrumentFamily.Horns);

        beats[0].IsRest.Should().BeFalse();
        var rests = beats.Count(beat => beat.IsRest);
        ((double)rests / beats.Count).Should().BeInRange(0.22, 0.34);
        beats.Zip(beats.Skip(1), (a, b) => a.IsRest && b.IsRest).Should().NotContain(true);
    }

    [Test]
    public void Horns_ChangeNoteBetweenAdjacentNotes()
    {
        var beats = Generate(PerformanceKind.Music, PerformanceTempo.Relaxed, PerformanceInstrumentFamily.Horns);

        beats.Zip(beats.Skip(1), (a, b) => !a.IsRest && !b.IsRest && a.Primary == b.Primary)
            .Should().NotContain(true);
    }

    [Test]
    public void SameSeed_ProducesTheSameCues()
    {
        var first = Generate(PerformanceKind.Music, PerformanceTempo.Upbeat, PerformanceInstrumentFamily.Horns, 99);
        var second = Generate(PerformanceKind.Music, PerformanceTempo.Upbeat, PerformanceInstrumentFamily.Horns, 99);

        first.Should().Equal(second);
    }
}
