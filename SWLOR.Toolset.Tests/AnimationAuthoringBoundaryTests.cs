using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class AnimationAuthoringBoundaryTests
{
    [Test]
    public void ImportedEndpointRoundingSamplesAtDeclaredLength()
    {
        var project = AnimationMdl.Import(Block("0.8333334"), Rig());
        project.Keys[^1].Time.Should().Be(project.Duration);
        project.Keys[^1].Pose[0].Position.X.Should().BeApproximately(1, 0.00001f);
        project.Validate();
    }

    [Test]
    public void ImportStillRejectsDataWellBeyondDeclaredLength()
    {
        Action import = () => AnimationMdl.Import(Block("1.5"), Rig());
        import.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void AnchoringSkipsShallowLimbsWhileKeepingCompleteLimbsPlanted()
    {
        var rig = Rig();
        rig.Joints.Add(new("lfoot_g", 0, new(new(-1, 0, 0), Quaternion.Identity, 1)));
        rig.Joints.Add(new("rthigh_g", 0, new(new(1, 0, 2), Quaternion.Identity, 1)));
        rig.Joints.Add(new("rshin_g", 2, new(new(0, .4f, -.8f), Quaternion.Identity, 1)));
        rig.Joints.Add(new("rfoot_g", 3, new(new(0, -.4f, -.8f), Quaternion.Identity, 1)));
        rig.Validate();
        var pose = rig.Joints.Select(j => j.Rest).ToArray();
        var before = AnimationRig.World(rig.Joints, pose);
        var moved = AnimationRig.SetJoint(rig.Joints, pose, 0,
            pose[0] with { Position = new(.1f, 0, 0) }, true);
        var after = AnimationRig.World(rig.Joints, moved);
        moved[0].Position.Should().Be(new Vector3(.1f, 0, 0));
        Vector3.Distance(after[1].Translation, before[1].Translation + new Vector3(.1f, 0, 0)).Should().BeLessThan(.0001f);
        Vector3.Distance(after[4].Translation, before[4].Translation).Should().BeLessThan(.0001f);
    }

    private static AnimationProject Rig() => new()
    {
        Joints = [new("rootdummy", -1, new(Vector3.Zero, Quaternion.Identity, 1))]
    };

    private static string Block(string lastTime) => $$"""
        newanim sw_test pmh0
          length 0.833333
          transtime 0.1
          animroot rootdummy
          node dummy rootdummy
            parent NULL
            positionkey 2
              0 0 0 0
              {{lastTime}} 1 0 0
          endnode
        doneanim sw_test pmh0
        """;
}
