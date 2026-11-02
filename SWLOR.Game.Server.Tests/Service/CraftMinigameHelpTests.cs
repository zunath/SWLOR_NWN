using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the crafting minigame help page: first-use records and the shared help layout.</summary>
public class CraftMinigameHelpTests
{
    private static readonly DateTime FirstOpened = new(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc);

    private sealed class HelpTestViewModel : CraftMinigameViewModel<HelpTestViewModel, GuiPayloadBase>
    {
        protected override CraftMinigameType Minigame => CraftMinigameType.Forge;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
        }
    }

    [Test]
    public void TheHelpPageOpensTheFirstTimeAndOnlyThen()
    {
        var player = new Player("p1");

        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Forge).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Forge, FirstOpened).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Forge, FirstOpened.AddDays(1)).Should().BeFalse();
        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Forge).Should().BeFalse();
        player.CraftMinigameHelpSeen[CraftMinigameType.Forge].Should().Be(FirstOpened, "the first use is kept");
    }

    [Test]
    public void EachMinigameKeepsItsOwnFirstUse()
    {
        var player = new Player("p1");
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Forge, FirstOpened);

        foreach (var minigame in Enum.GetValues<CraftMinigameType>().Where(m => m != CraftMinigameType.Forge))
            CraftMinigameHelp.ShouldOpenOnFirstUse(player, minigame).Should().BeTrue($"{minigame} was never opened");
    }

    [Test]
    public void FirstUseSurvivesALogout()
    {
        var player = new Player("p1");
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Circuit, FirstOpened);

        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

        CraftMinigameHelp.ShouldOpenOnFirstUse(restored, CraftMinigameType.Circuit).Should().BeFalse();
        CraftMinigameHelp.ShouldOpenOnFirstUse(restored, CraftMinigameType.Kitchen).Should().BeTrue();
    }

    [Test]
    public void ARecordWithoutTheFirstUseTableStillWorks()
    {
        var player = new Player("p1") { CraftMinigameHelpSeen = null };

        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Line).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Line, FirstOpened).Should().BeTrue();
        player.CraftMinigameHelpSeen.Should().ContainKey(CraftMinigameType.Line);
    }

    [Test]
    public void EveryMinigameHasAStablePositiveId()
    {
        // Stored per player, so these numbers must never change or be reused.
        ((int)CraftMinigameType.Forge).Should().Be(1);
        ((int)CraftMinigameType.Circuit).Should().Be(2);
        ((int)CraftMinigameType.Fabricator).Should().Be(3);
        ((int)CraftMinigameType.Kitchen).Should().Be(4);
        ((int)CraftMinigameType.Reaction).Should().Be(5);
        ((int)CraftMinigameType.Experiment).Should().Be(6);
        ((int)CraftMinigameType.Prospecting).Should().Be(7);
        ((int)CraftMinigameType.Search).Should().Be(8);
        ((int)CraftMinigameType.Line).Should().Be(9);
    }

    [Test]
    public void TheSharedHelpPartialAndButton_BuildWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();
        var builder = new GuiWindowBuilder<HelpTestViewModel>();

        var window = builder.CreateWindow(GuiWindowType.DebugEnmity)
            .SetInitialGeometry(0, 0, 500f, 400f)
            .SetTitle("Help test")
            .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                CraftMinigameHelpPartial.Build(group, "How it works", "Plain help text.", model => model.OnCloseHelp()))
            .DefinePartialView("HELP_TEST_MAIN", group =>
            {
                group.SetShowBorder(false);
                group.SetScrollbars(SWLOR.Game.Server.Core.Beamdog.NuiScrollbars.None);
                group.AddColumn(column => column.AddRow(row =>
                {
                    row.AddSpacer();
                    CraftMinigameHelpPartial.AddHelpButton(row, "How this works", model => model.OnHelp());
                }));
            });
        window.AddStandardLayout(layout => layout.SetContentPartialElement("help_test_content"));

        var constructed = builder.Build();

        constructed.LayoutFindings.Should().BeEmpty();
    }
}
