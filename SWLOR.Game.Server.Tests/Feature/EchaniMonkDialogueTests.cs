using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;

namespace SWLOR.Game.Server.Tests.Feature;

public class EchaniMonkDialogueTests
{
    [Test]
    public void ResidentsReplyAfterBothDisciplesInsteadOfReplacingSai()
    {
        Enumerable.Range(0, 6).Select(beat => Speaker(beat, true))
            .Should().Equal(new[] { 0, 1, 2, 3, -1, -1 },
                "Vaelis, Ren and Sai each speak before the resident's separate reply");
    }

    [Test]
    public void MissingOrBusyHostDoesNotSilenceEitherDisciple()
    {
        Enumerable.Range(0, 6).Select(beat => Speaker(beat, false))
            .Should().Equal(0, 1, 2, -1, -1, -1);
    }

    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(12)]
    [TestCase(30)]
    public void RestAndInvalidBeatsDoNotRepeatDialogue(int beat)
    {
        Speaker(beat, true).Should().Be(-1);
    }

    [TestCase("errands")]
    [TestCase("cloth")]
    [TestCase("teacher")]
    [TestCase("grove")]
    [TestCase("sentinel")]
    public void BothDisciplesHaveDistinctLinesForEachResident(string scene)
    {
        var ren = Invoke<string>("Line", scene, 1);
        var sai = Invoke<string>("Line", scene, 2);
        ren.Should().NotBeNullOrWhiteSpace().And.NotBe(Invoke<string>("Line", "unknown", 1));
        sai.Should().NotBeNullOrWhiteSpace().And.NotBe(Invoke<string>("Line", "unknown", 2));
        sai.Should().NotBe(ren);
        Invoke<string>("Reply", scene).Should().NotBe(Invoke<string>("Reply", "unknown"));
    }

    private static int Speaker(int beat, bool hostCanReply) =>
        Invoke<int>("SceneSpeakerIndex", beat, hostCanReply);

    [Test]
    public void RiverMeditationFollowsDialogueAndIncludesBothDisciples()
    {
        var meditation = (from beat in Enumerable.Range(0, 6)
                          from monk in Enumerable.Range(0, 3)
                          where Invoke<bool>("ShouldMeditate", "river", beat, monk)
                          select (Beat: beat, Monk: monk)).ToArray();

        meditation.Should().Equal((3, 1), (3, 2));
        Speaker(3, false).Should().Be(-1, "the disciples finish speaking before sitting down");
    }

    [TestCase("temple")]
    [TestCase("spa")]
    public void ExistingMeditationStillIncludesTheWholeGroup(string scene)
    {
        Enumerable.Range(0, 3).Where(monk => Invoke<bool>("ShouldMeditate", scene, 3, monk))
            .Should().Equal(0, 1, 2);
    }

    [TestCase("grove")]
    [TestCase("sentinel")]
    public void ResidentStopsKeepTheirReplyBeat(string scene)
    {
        Enumerable.Range(0, 3).Any(monk => Invoke<bool>("ShouldMeditate", scene, 3, monk))
            .Should().BeFalse();
        Speaker(3, true).Should().Be(3);
    }

    private static T Invoke<T>(string name, params object[] arguments)
    {
        var method = typeof(EchaniTempleMonks).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
                     ?? throw new MissingMethodException(typeof(EchaniTempleMonks).FullName, name);
        return (T)method.Invoke(null, arguments)!;
    }
}
