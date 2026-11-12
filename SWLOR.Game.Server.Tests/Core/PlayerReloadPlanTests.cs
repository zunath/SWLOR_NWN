using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The per-player steps of a hot reload (<see cref="PlayerReloadPlan"/>) and the players' screens (<see cref="PlayerScreenCarry"/>):
/// each step runs for every online player in order and goes on past a player who fails; a step binds only to a public per-player
/// method of the handler's class; the screens the old build closes are the ones the new build reopens, by player id.
/// </summary>
[TestFixture]
public class PlayerReloadPlanTests
{
    [Test]
    public void A_step_runs_for_every_player_and_goes_on_past_one_that_fails()
    {
        // The failure is logged with the player's name, which the engine answers.
        using var engine = new PlayerEngine();
        var ran = new List<uint>();
        var run = () => PlayerReloadPlan.RunForEach(new uint[] { 1, 2, 3 }, player =>
        {
            ran.Add(player);
            if (player == 2)
                throw new InvalidOperationException("two");
        }, "Fixture.Step");

        run.Should().Throw<InvalidOperationException>().WithMessage("Fixture.Step failed for 1 of 3 players.")
            .WithInnerException<InvalidOperationException>().WithMessage("two");
        ran.Should().Equal(1u, 2u, 3u);
    }

    [Test]
    public void A_step_binds_to_the_handlers_public_per_player_method()
    {
        var owner = new StepOwner();
        var (method, run) = PlayerReloadPlan.BindStep(owner.Handler, nameof(StepOwner.HandlerFor));
        method.Name.Should().Be(nameof(StepOwner.HandlerFor));
        run(9);
        owner.Players.Should().Equal(9u);

        var (_, staticRun) = PlayerReloadPlan.BindStep(StepOwner.StaticHandler, nameof(StepOwner.StaticHandlerFor));
        staticRun(4);
        StepOwner.StaticPlayers.Should().Contain(4u);

        var missing = () => PlayerReloadPlan.BindStep(owner.Handler, "Nothing");
        missing.Should().Throw<InvalidOperationException>().WithMessage("*names player reload step 'Nothing'*");
        var wrongKind = () => PlayerReloadPlan.BindStep(owner.Handler, nameof(StepOwner.StaticHandlerFor));
        wrongKind.Should().Throw<InvalidOperationException>("an instance handler's step is an instance method");
        var unnamed = () => PlayerReloadPlan.BindStep(owner.Handler, null);
        unnamed.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Only_mod_enter_and_mod_exit_have_a_run_class()
    {
        PlayerReloadPlan.RunClassOf(ScriptName.OnModuleEnter).Should().Be(PlayerReload.Restore);
        PlayerReloadPlan.RunClassOf(ScriptName.OnModuleExit).Should().Be(PlayerReload.Quiesce);
        var other = () => PlayerReloadPlan.RunClassOf(ScriptName.OnModuleLoad);
        other.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void The_screens_the_old_build_closes_are_reopened_for_the_same_players()
    {
        using var engine = new PlayerEngine();
        engine.Engine.SetReturnHandler("GetObjectUUID", args => "uuid-" + args[0]);
        var conversation = new ConversationReloadState { GraphId = "graph", NodeId = "start", Owner = 77 };
        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy
            .On(nameof(IGuiService.CloseWindowsForReload), args => (uint)args[0]! == 1
                ? new[] { new GuiReloadWindow(GuiWindowType.Quests, 1, 0) }
                : Array.Empty<GuiReloadWindow>())
            .On(nameof(IGuiService.ReopenWindowsForReload), _ => null);
        var conversations = ScriptedProxy.Create<IConversationService>(out var conversationProxy);
        conversationProxy
            .On(nameof(IConversationService.CarryForReload), args => (uint)args[0]! == 2 ? conversation : null)
            .On(nameof(IConversationService.ResumeAfterReload), _ => null);
        var carry = new PlayerScreenCarry(gui, conversations);

        var text = carry.Close(new uint[] { 1, 2 });
        guiProxy.Calls.Select(call => (uint)call.Arguments[0]!).Should().Equal(1u, 2u);
        conversationProxy.Calls.Select(call => call.Member).Should().Equal(new[] { "CarryForReload", "CarryForReload" },
            "the conversation is taken out before the windows close, since closing it would end it");

        carry.Reopen(new uint[] { 2, 1, 3 }, text);

        var reopened = guiProxy.Calls.Where(call => call.Member == nameof(IGuiService.ReopenWindowsForReload)).ToArray();
        reopened.Select(call => (uint)call.Arguments[0]!).Should().Equal(2u, 1u);
        ((IReadOnlyList<GuiReloadWindow>)reopened[1].Arguments[1]!).Should().Equal(new GuiReloadWindow(GuiWindowType.Quests, 1, 0));
        var resumed = conversationProxy.Calls.Single(call => call.Member == nameof(IConversationService.ResumeAfterReload));
        resumed.Arguments[0].Should().Be(2u);
        ((ConversationReloadState)resumed.Arguments[1]!).Should().BeEquivalentTo(conversation);
    }

    [Test]
    public void A_player_whose_screen_fails_does_not_stop_the_others()
    {
        using var engine = new PlayerEngine();
        engine.Engine.SetReturnHandler("GetObjectUUID", args => "uuid-" + args[0]);
        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy.On(nameof(IGuiService.CloseWindowsForReload), args => (uint)args[0]! == 1
            ? throw new InvalidOperationException("broken window")
            : new[] { new GuiReloadWindow(GuiWindowType.Quests, 2, 0) });
        var conversations = ScriptedProxy.Create<IConversationService>(out var conversationProxy);
        conversationProxy.On(nameof(IConversationService.CarryForReload), _ => null);

        var text = new PlayerScreenCarry(gui, conversations).Close(new uint[] { 1, 2 });

        text.Should().Contain("uuid-2").And.Contain("Quests");
        new PlayerScreenCarry(gui, conversations).Invoking(carry => carry.Reopen(new uint[] { 1 }, null)).Should().NotThrow(
            "a reload whose old build carried no screens reopens nothing");
    }

    private sealed class StepOwner
    {
        public static readonly List<uint> StaticPlayers = new();
        public List<uint> Players { get; } = new();

        public void Handler() => HandlerFor(0);

        public void HandlerFor(uint player) => Players.Add(player);

        public static void StaticHandler() => StaticHandlerFor(0);

        public static void StaticHandlerFor(uint player) => StaticPlayers.Add(player);
    }
}
