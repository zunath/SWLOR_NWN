using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

/// <summary>
/// Checks the installed work clips of the crafting revamp: five station loops, six gathering actions and the four Line moves.
/// Whether the motion reads well still needs the client (Closeout joint pass); these tests guard that the assets are complete,
/// installed on both humanoid rigs and well formed.
/// </summary>
public class WorkAnimationAssetTests
{
    private static readonly SkillType[] StationSkills =
    {
        SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture, SkillType.Espionage
    };

    private static IEnumerable<AnimationClip> Stations => StationSkills.Select(WorkAnimation.ForStation);

    private static IEnumerable<AnimationClip> Gathering => Enum.GetValues<GatheringAction>().Select(WorkAnimation.ForGathering);

    private static IEnumerable<AnimationClip> Fishing => new[]
    {
        WorkAnimation.ForFishing(LineActionIds.Reel),
        WorkAnimation.ForFishing(LineActionIds.GiveLine),
        WorkAnimation.ForFishing(LineActionIds.Hold),
        WorkAnimation.FishLanded
    };

    private static IEnumerable<TestCaseData> EveryClip() =>
        Stations.Concat(Gathering).Concat(Fishing).Select(clip => new TestCaseData(clip).SetArgDisplayNames(clip.Name));

    private static IEnumerable<TestCaseData> StationClips() =>
        Stations.Select(clip => new TestCaseData(clip).SetArgDisplayNames(clip.Name));

    [TestCaseSource(nameof(EveryClip))]
    public void EveryClipIsInstalledOnBothHumanoidRigsAndEndsInTheModelsOwnIdle(AnimationClip clip)
    {
        FishingAnimationTests.AssertInstalledOnBothHumanoidRigs(clip);
    }

    [TestCaseSource(nameof(StationClips))]
    public void AStationLoopClosesOnItsOwnFirstPose_SoItRepeatsWithoutAHitch(AnimationClip clip)
    {
        var project = LoadProject(clip);

        var start = project.Sample(0);
        var finish = project.Sample(project.Duration);

        for (var i = 0; i < start.Length; i++)
        {
            Vector3.Distance(start[i].Position, finish[i].Position).Should().BeLessThan(.0001f, project.Joints[i].Name);
            Math.Abs(Quaternion.Dot(start[i].Orientation, finish[i].Orientation)).Should().BeGreaterThan(.99999f, project.Joints[i].Name);
        }
    }

    [TestCaseSource(nameof(StationClips))]
    public void AStationLoopKeepsMovingThroughItsWholeCycle(AnimationClip clip)
    {
        var project = LoadProject(clip);
        var hand = project.Joints.FindIndex(joint => joint.Name == "rhand_g");
        var first = AnimationRig.World(project.Joints, project.Sample(0))[hand].Translation;

        var farthest = 0f;
        for (var time = 0f; time <= project.Duration; time += 1f / 20)
        {
            var position = AnimationRig.World(project.Joints, project.Sample(time))[hand].Translation;
            farthest = Math.Max(farthest, Vector3.Distance(first, position));
        }

        farthest.Should().BeGreaterThan(.05f, "a work loop that barely moves reads as frozen from the game camera");
    }

    [TestCase("crafting-stations", 5)]
    [TestCase("gathering", 6)]
    [TestCase("fishing", 7)]
    public void EachFolderManifestListsExactlyTheClipsOfItsRecipe(string folder, int count)
    {
        var directory = Path.Combine(FishingAnimationTests.Root, "design", "animations", folder);
        using var recipe = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "recipe.json")));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));

        var recipeIds = recipe.RootElement.GetProperty("motions").EnumerateArray().Select(m => m.GetProperty("id").GetString()).ToArray();
        var manifestIds = manifest.RootElement.GetProperty("Animations").EnumerateArray().Select(m => m.GetProperty("Id").GetString()).ToArray();

        recipeIds.Should().HaveCount(count);
        manifestIds.Should().Equal(recipeIds);
        foreach (var id in recipeIds)
            File.Exists(Path.Combine(directory, id + ".swlanim")).Should().BeTrue(id);
    }

    private static AnimationProject LoadProject(AnimationClip clip)
    {
        var registrations = JsonSerializer.Deserialize<AnimationRegistration[]>(
            File.ReadAllText(Path.Combine(FishingAnimationTests.Root, "design/animations/registry.json")))!;
        var registration = registrations.Single(r => r.AnimationName == clip.Name);
        return AnimationProject.Deserialize(File.ReadAllText(Path.Combine(FishingAnimationTests.Root, registration.ProjectPath!)));
    }
}
