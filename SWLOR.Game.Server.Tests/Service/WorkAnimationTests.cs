using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers which authored clip each station craft, gathering action and fishing move plays, and that every clip is installed for
/// both humanoid rigs with an editable project. The clip assets themselves are checked in the toolset tests.
/// </summary>
public class WorkAnimationTests
{
    private static readonly SkillType[] StationSkills =
    {
        SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture, SkillType.Espionage
    };

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static IEnumerable<AnimationClip> StationClips => StationSkills.Select(WorkAnimation.ForStation);

    private static IEnumerable<AnimationClip> GatheringClips =>
        Enum.GetValues<GatheringAction>().Select(WorkAnimation.ForGathering);

    private static IEnumerable<AnimationClip> FishingClips => new[]
    {
        WorkAnimation.ForFishing(LineActionIds.Reel),
        WorkAnimation.ForFishing(LineActionIds.GiveLine),
        WorkAnimation.ForFishing(LineActionIds.Hold),
        WorkAnimation.FishLanded
    };

    private static IEnumerable<AnimationClip> AllClips => StationClips.Concat(GatheringClips).Concat(FishingClips);

    [Test]
    public void EachStationCraftHasItsOwnLoopingClip()
    {
        StationClips.Should().OnlyContain(clip => clip != null);
        StationClips.Select(clip => clip.Name).Should().OnlyHaveUniqueItems();
        WorkAnimation.ForStation(SkillType.Smithery).Should().BeSameAs(AuthoredAnimation.ForgeWork);
        WorkAnimation.ForStation(SkillType.Engineering).Should().BeSameAs(AuthoredAnimation.Circuitry);
        WorkAnimation.ForStation(SkillType.Fabrication).Should().BeSameAs(AuthoredAnimation.Fabricate);
        WorkAnimation.ForStation(SkillType.Agriculture).Should().BeSameAs(AuthoredAnimation.Cooking);
        WorkAnimation.ForStation(SkillType.Espionage).Should().BeSameAs(AuthoredAnimation.Compound);
    }

    [Test]
    public void OnlyTheFiveStationCraftsHaveAStationClip()
    {
        foreach (var skill in Enum.GetValues<SkillType>().Except(StationSkills))
            WorkAnimation.ForStation(skill).Should().BeNull($"{skill} has no crafting station loop");
    }

    [Test]
    public void EveryGatheringActionHasItsOwnClip()
    {
        Enum.GetValues<GatheringAction>().Should().HaveCount(6);
        GatheringClips.Should().OnlyContain(clip => clip != null);
        GatheringClips.Select(clip => clip.Name).Should().OnlyHaveUniqueItems();
        WorkAnimation.ForGathering(GatheringAction.Survey).Should().BeSameAs(AuthoredAnimation.Survey);
        WorkAnimation.ForGathering(GatheringAction.Strike).Should().BeSameAs(AuthoredAnimation.Strike);
        WorkAnimation.ForGathering(GatheringAction.HeavyStrike).Should().BeSameAs(AuthoredAnimation.HvyStrike);
        WorkAnimation.ForGathering(GatheringAction.Extract).Should().BeSameAs(AuthoredAnimation.Extract);
        WorkAnimation.ForGathering(GatheringAction.Search).Should().BeSameAs(AuthoredAnimation.Search);
        WorkAnimation.ForGathering(GatheringAction.Dig).Should().BeSameAs(AuthoredAnimation.Dig);
    }

    [Test]
    public void EveryProspectingWorkActionPlaysItsGatheringClip_AndLeaveHasNone()
    {
        WorkAnimation.ForProspecting(ProspectingActionIds.Survey).Should().BeSameAs(AuthoredAnimation.Survey);
        WorkAnimation.ForProspecting(ProspectingActionIds.Strike).Should().BeSameAs(AuthoredAnimation.Strike);
        WorkAnimation.ForProspecting(ProspectingActionIds.HeavyStrike).Should().BeSameAs(AuthoredAnimation.HvyStrike);
        WorkAnimation.ForProspecting(ProspectingActionIds.Extract).Should().BeSameAs(AuthoredAnimation.Extract);
        WorkAnimation.ForProspecting(ProspectingActionIds.Leave).Should().BeNull();
        WorkAnimation.ForProspecting(null).Should().BeNull("a Quick Harvest unit has no authored clip");
    }

    [Test]
    public void AnUnknownGatheringActionIsAnError_SoACallerNeverPlaysTheWrongClipSilently()
    {
        var unknown = () => WorkAnimation.ForGathering((GatheringAction)99);

        unknown.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TheLineMovesReelGiveLineAndHoldPlayTheirOwnClips_AndLandingPlaysItsOwn()
    {
        WorkAnimation.ForFishing(LineActionIds.Reel).Should().BeSameAs(AuthoredAnimation.Reel);
        WorkAnimation.ForFishing(LineActionIds.GiveLine).Should().BeSameAs(AuthoredAnimation.GiveLine);
        WorkAnimation.ForFishing(LineActionIds.Hold).Should().BeSameAs(AuthoredAnimation.HoldRod);
        WorkAnimation.FishLanded.Should().BeSameAs(AuthoredAnimation.LandFish);
        FishingClips.Select(clip => clip.Name).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void SetHookAndAnUnknownMoveHaveNoClip()
    {
        WorkAnimation.ForFishing(LineActionIds.SetHook).Should().BeNull();
        WorkAnimation.ForFishing("unknown").Should().BeNull();
        WorkAnimation.ForFishing(null).Should().BeNull();
    }

    [Test]
    public void EveryFishingMoveFinishesInsideOneTurnOfTheFight()
    {
        FishingClips.Should().OnlyContain(clip => clip.Duration < LineRules.TurnSeconds);
    }

    [Test]
    public void NoClipRepeatsAnotherClip_NorTheFishingCastClips()
    {
        var names = AllClips.Select(clip => clip.Name)
            .Concat(new[] { AuthoredAnimation.Fishing6.Name, AuthoredAnimation.Fishing7.Name, AuthoredAnimation.Fishing8.Name })
            .ToList();

        names.Should().OnlyHaveUniqueItems();
        names.Should().OnlyContain(name => name.StartsWith("sw_") && name.Length <= AnimationClip.MaxNameLength);
    }

    [Test]
    public void EveryClipIsInstalledForBothHumanoidRigs_WithItsEditableProject()
    {
        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/registry.json")));
        var entries = registry.RootElement.EnumerateArray().ToDictionary(entry => entry.GetProperty("AnimationName").GetString()!);

        foreach (var clip in AllClips)
        {
            entries.Should().ContainKey(clip.Name);
            var entry = entries[clip.Name];
            entry.GetProperty("Duration").GetSingle().Should().BeApproximately(clip.Duration, .01f);
            entry.GetProperty("Targets").EnumerateArray().Select(target => target.GetString())
                .Should().BeEquivalentTo("SWLOR_Haks/sw_cr_creature/a_ba.mdl", "SWLOR_Haks/sw_cr_creature/a_fa.mdl");
            File.Exists(Path.Combine(Root, entry.GetProperty("ProjectPath").GetString()!)).Should().BeTrue(clip.Name);
        }
    }

    [Test]
    public void TheRecipesNameTheirActivity_AndOnlyTheStationClipsLoop()
    {
        var stations = ReadRecipe("crafting-stations");
        stations.Should().HaveCount(5);
        stations.Values.Should().OnlyContain(motion => motion.Activity == "Crafting" && motion.Loop);

        var gathering = ReadRecipe("gathering");
        gathering.Should().HaveCount(6);
        gathering.Values.Should().OnlyContain(motion => motion.Activity == "Gathering" && !motion.Loop);

        var fishing = ReadRecipe("fishing");
        fishing.Keys.Should().Contain(new[] { "Fishing6", "Fishing7", "Fishing8", "Reel", "GiveLine", "HoldRod", "LandFish" });
        fishing.Values.Should().OnlyContain(motion => motion.Activity == "Fishing" && !motion.Loop);
    }

    private sealed record RecipeMotion(string Activity, bool Loop);

    private static Dictionary<string, RecipeMotion> ReadRecipe(string folder)
    {
        using var recipe = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design", "animations", folder, "recipe.json")));
        return recipe.RootElement.GetProperty("motions").EnumerateArray().ToDictionary(
            motion => motion.GetProperty("id").GetString()!,
            motion => new RecipeMotion(
                motion.GetProperty("activity").GetString()!,
                motion.TryGetProperty("loop", out var loop) && loop.GetBoolean()));
    }
}
