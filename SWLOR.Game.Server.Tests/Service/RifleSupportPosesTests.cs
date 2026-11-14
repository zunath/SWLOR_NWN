using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Tests.Service;

public class RifleSupportPosesTests
{
    [Test]
    public void FittedAimedShotKeepsItsDurationAndUsesItsOwnPlaybackPhases()
    {
        var poses = new RifleSupportPoses("rf266_318_i", "rf266_318_r", "rf266_318_s");
        var clip = poses.ResolveAuthored(AuthoredAnimation.AimedShot);
        clip.Name.Should().Be("rf266_318_a");
        clip.StartName.Should().Be("rf266_318_a_in");
        clip.EndName.Should().Be("rf266_318_a_out");
        clip.Duration.Should().Be(AuthoredAnimation.AimedShot.Duration);
    }

    [Test]
    public void OtherAuthoredAbilitiesRetainTheirOriginalClip()
    {
        var poses = new RifleSupportPoses("rf266_318_i", "rf266_318_r", "rf266_318_s");
        var clip = new AnimationClip("sw_other", 2f);
        poses.ResolveAuthored(clip).Should().BeSameAs(clip);
    }
}
