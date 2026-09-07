using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AnimationService;
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
    public void WindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var window = new AnimationDebugDefinition().BuildWindow();
        window.Type.Should().Be(GuiWindowType.AnimationDebug);
        window.LayoutFindings.Should().BeEmpty();
    }
}
