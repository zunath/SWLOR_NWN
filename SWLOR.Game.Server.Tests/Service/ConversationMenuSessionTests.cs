using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Service;

public class ConversationMenuSessionTests
{
    [Test]
    public void NuiMenu_ShowsEveryResponseWithoutTwelveChoiceShellPagination()
    {
        var menu = new ConversationMenuBuilder()
            .AddPage("main", page =>
            {
                page.Header = "Choose a destination.";
                for (var index = 0; index < 20; index++)
                    page.AddResponse($"Destination {index + 1}", () => { });
            })
            .Build();
        var session = CreateSession(menu);

        session.Start().Should().BeTrue();

        session.CurrentNode.Text.Single().Text.Should().Be("Choose a destination.");
        session.VisibleChoices.Should().HaveCount(20);
        session.VisibleChoices[19].Text.Text.Should().Be("Destination 20");
    }

    [Test]
    public void NuiMenu_PageChangesAddOneBackChoiceAndRestoreThePreviousPage()
    {
        ConversationMenuSession session = null;
        var menu = new ConversationMenuBuilder()
            .AddPage("main", page => page.AddResponse("Details", () => session!.GoToPage("details")))
            .AddPage("details", page => page.Header = "The details page.")
            .Build();
        session = CreateSession(menu);
        session.Start();

        session.SelectChoice(0).Should().Be(ConversationSelectionResult.MovedToNextNode);
        session.CurrentNode.Id.Should().Be("details");
        session.VisibleChoices.Should().ContainSingle();
        session.VisibleChoices[0].Text.Text.Should().Be("Back");

        session.SelectChoice(0).Should().Be(ConversationSelectionResult.MovedToNextNode);
        session.CurrentNode.Id.Should().Be("main");
        session.VisibleChoices[0].Text.Text.Should().Be("Details");
    }

    [Test]
    public void NuiMenu_EndActionsRunExactlyOnce()
    {
        var endCount = 0;
        ConversationMenuSession session = null;
        var menu = new ConversationMenuBuilder()
            .AddEndAction(() => endCount++)
            .AddPage("main", page => page.AddResponse(
                "Finish",
                () => session!.End(ConversationEndReason.Completed)))
            .Build();
        session = CreateSession(menu);
        session.Start();

        session.SelectChoice(0).Should().Be(ConversationSelectionResult.ConversationEnded);
        session.End(ConversationEndReason.Aborted);

        session.HasEnded.Should().BeTrue();
        endCount.Should().Be(1);
    }

    [Test]
    public void NuiMenu_ConvertsLegacyColorTokensIntoExplicitNuiColor()
    {
        var menu = new ConversationMenuBuilder()
            .AddPage("main", page =>
            {
                page.Header = $"Status: {ColorToken.Red("danger")}";
                page.AddResponse(ColorToken.Green("Proceed"), () => { });
            })
            .Build();
        var session = CreateSession(menu);

        session.Start();

        session.CurrentNode.Text.Should().Contain(block =>
            block.Text == "danger" &&
            block.Style == ConversationTextStyle.Custom &&
            block.Color.Red == 255 &&
            block.Color.Green == 0 &&
            block.Color.Blue == 0);
        session.VisibleChoices[0].Text.Text.Should().Be("Proceed");
        session.VisibleChoices[0].Text.Style.Should().Be(ConversationTextStyle.Custom);
        session.VisibleChoices[0].Text.Color.Green.Should().Be(255);
    }

    [Test]
    public void NuiMenu_UsesItsAuthoredPortraitOnEveryPage()
    {
        var menu = new ConversationMenuBuilder()
            .WithPortrait("p_256x128_medic1")
            .AddPage("main", page => page.Header = "Register here.")
            .Build();
        var session = CreateSession(menu);

        session.Start();

        session.CurrentNode.PortraitResref.Should().Be("p_256x128_medic1");
    }

    private static ConversationMenuSession CreateSession(ConversationMenuSpec menu) =>
        new(menu, new ConversationContext(1, 2), new FakeRuntime());

    private sealed class FakeRuntime : IConversationRuntime
    {
        public bool EvaluateCondition(ConversationContext context, ConversationCondition condition) => true;
        public bool ExecuteAction(ConversationContext context, ConversationAction action) => true;
        public string ResolveText(ConversationContext context, string text) => text;
    }
}
