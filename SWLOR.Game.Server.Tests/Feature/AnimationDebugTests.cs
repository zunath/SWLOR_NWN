using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Feature;

public class AnimationDebugTests
{
    [Test]
    public void CommandsAllowStaffOrAnyoneOnTestOnly()
    {
        var commands = new DebuggingChatCommand().BuildChatCommands();
        var preview = new AnimationPreviewChatCommand().BuildChatCommands();
        foreach (var command in new[] { commands["animations"], preview["animtest"] })
        {
            command.Authorization.Should().Be(AuthorizationLevel.DM | AuthorizationLevel.Admin);
            command.AvailableToAllOnTestEnvironment.Should().BeTrue();
            command.RequiresTarget.Should().BeFalse();
        }
        foreach (var environment in Enum.GetValues<ServerEnvironmentType>())
        {
            AnimationPreviewCatalog.IsAllowed(environment, AuthorizationLevel.Player).Should().Be(environment == ServerEnvironmentType.Test);
            AnimationPreviewCatalog.IsAllowed(environment, AuthorizationLevel.DM).Should().BeTrue();
            AnimationPreviewCatalog.IsAllowed(environment, AuthorizationLevel.Admin).Should().BeTrue();
        }
    }

    [TestCase("covering strike", "CoveringStrike")]
    [TestCase("RIOTBLADE", "RiotBlade")]
    [TestCase("sw_savagecle", "SavageCleave")]
    public void SearchMatchesReadableNamesAndInstalledNames(string query, string expected) =>
        AnimationPreviewCatalog.Search(query).Should().ContainSingle().Which.Id.Should().Be(expected);

    [Test]
    public void SearchClearsAndHandlesNoMatches()
    {
        var model = new AnimationDebugViewModel { SearchText = "strike" };
        model.Names.Should().Contain("Covering Strike").And.Contain("Rending Strike");
        model.Durations.Should().HaveCount(model.Names.Count);
        model.SearchText = "does not exist";
        model.Names.Should().BeEmpty();
        model.HasNext.Should().BeFalse();
        model.OnNext()();
        model.PageText.Should().Be("Page 1 / 1");
        model.SearchText = "";
        model.Names.Should().HaveCount(Math.Min(AnimationDebugViewModel.PageSize, AnimationPreviewCatalog.Entries.Count));
        model.OnPrevious()();
        model.HasPrevious.Should().BeFalse();
    }

    [Test]
    public void CategoriesFollowPlaybackSkillsAndRetainSharedAndUnassignedClips()
    {
        var entries = AnimationPreviewCatalog.CreateEntries(new[]
        {
            new AbilityDetail { SkillType = SkillType.Vibroblade, QueuedAttackAnimation = AuthoredAnimation.RiotBlade },
            new AbilityDetail { SkillType = SkillType.Lightsaber, AuthoredAnimation = AuthoredAnimation.RiotBlade },
            new AbilityDetail { SkillType = SkillType.Vibroblade, AuthoredAnimation = AuthoredAnimation.RiotBlade },
        }, authoredEntries: Array.Empty<AbilityAnimationEntry>());
        var riot = entries.Single(entry => entry.Id == "RiotBlade");
        riot.Categories.Should().BeEquivalentTo("Vibroblade", "Lightsaber");
        AnimationPreviewCatalog.Search("riot", "Vibroblade", entries).Should().ContainSingle();
        AnimationPreviewCatalog.Search("riot", "Lightsaber", entries).Should().ContainSingle();
        AnimationPreviewCatalog.Search("riot", "Other", entries).Should().BeEmpty();
        AnimationPreviewCatalog.Search("shield", "Other", entries).Select(entry => entry.Id)
            .Should().Contain("ShieldBash").And.Contain("ShieldWall");
    }

    [Test]
    public void EveryGeneratedClipHasItsReadableNameAndCategoryWithoutRequiringPlaybackOverrides()
    {
        var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities().Values);
        var entries = AnimationPreviewCatalog.CreateEntries(abilities,
            AnimationPlanningTests.CurrentPerks().ToDictionary(perk => perk.Type));
        entries.Should().NotBeEmpty();
        foreach (var entry in ActiveAbilityAnimationCatalog.Entries)
            entries.Should().ContainSingle(preview => preview.Id == entry.Id && preview.DisplayName == entry.DisplayName &&
                preview.Categories.Contains(entry.Category));
    }

    [Test]
    public void CategoriesComposeWithSearchAndResetPagesForLargeCatalogs()
    {
        var entries = Enumerable.Range(0, 1050).Select(i => new AnimationPreviewCatalog.Entry(
            $"Move{i}", $"Move {i:D4}", AuthoredAnimation.RiotBlade, new[] { i < 25 ? "Vibroblade" : "Force" })).ToArray();
        var model = new AnimationDebugViewModel();
        model.LoadCatalog(entries);
        model.CategoryNames.Should().Equal("All animations (1050)", "Force (1025)", "Vibroblade (25)");
        model.SelectCategory(2);
        model.OnNext()();
        model.Names.Should().HaveCount(5);
        model.SearchText = "Move 000";
        model.PageText.Should().Be("Page 1 / 1");
        model.Names.Should().HaveCount(10);
        model.SelectCategory(1);
        model.Names.Should().BeEmpty();
        model.CategorySelected.Should().Equal(false, true, false);
        model.SearchText = "";
        model.Names.Should().HaveCount(20);
        model.OnNext()();
        model.SelectCategory(2);
        model.PageText.Should().Be("Page 1 / 2");
        model.SelectCategory(-1);
        model.SelectCategory(999);
        model.SelectedCategory.Should().Be("Vibroblade");
        model.SelectCategory(0);
        model.OnNext()();
        model.Names.Should().HaveCount(20);
        model.PageText.Should().Be("Page 2 / 53");
    }

    [Test]
    public void WindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var window = new AnimationDebugDefinition().BuildWindow();
        window.Type.Should().Be(GuiWindowType.AnimationDebug);
        window.LayoutFindings.Should().BeEmpty();
    }
}
