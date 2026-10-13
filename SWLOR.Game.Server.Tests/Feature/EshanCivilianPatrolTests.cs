using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanCivilianPatrolTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("eoq_ilya_1")]
    [TestCase("eoq_ilya_1,,eoq_ilya_3")]
    [TestCase("eoq_ilya_1,eoq_ilya_1")]
    [TestCase("eoq_ilya_1,EOQ_ILYA_1")]
    public void AbsentOrMalformedOptInDoesNotCreateARoute(string route)
    {
        Invoke<string[]>("ParseRoute", route).Should().BeEmpty();
    }

    [Test]
    public void RoutePreservesAuthoredConsecutiveOrder()
    {
        Invoke<string[]>("ParseRoute", " eoq_ilya_1,eoq_ilya_2, eoq_ilya_3,eoq_ilya_4 ")
            .Should().Equal("eoq_ilya_1", "eoq_ilya_2", "eoq_ilya_3", "eoq_ilya_4");
    }

    [Test]
    public void RouteRejectsUnboundedWaypointLists()
    {
        var route = string.Join(',', Enumerable.Range(1, 17).Select(index => $"eoq_patrol_{index}"));
        Invoke<string[]>("ParseRoute", route).Should().BeEmpty();
    }

    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 0)]
    public void ArrivalAdvancesOnlyToTheNextConsecutivePoint(int current, int expected)
    {
        Invoke<int>("NextIndex", current, 4).Should().Be(expected);
    }

    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(int.MaxValue)]
    public void InvalidSavedIndexReturnsToTheFirstPoint(int current)
    {
        Invoke<int>("NormalizeIndex", current, 4).Should().Be(0);
    }

    [Test]
    public void IdleAvailableCivilianCanStartItsAuthoredRoute()
    {
        CanMove().Should().BeTrue();
    }

    [Test]
    public void MissingRouteCombatConversationInteractionAndBusyStatesSuspendPatrol()
    {
        CanMove(available: false).Should().BeFalse();
        CanMove(routeAvailable: false).Should().BeFalse();
        CanMove(inCombat: true).Should().BeFalse();
        CanMove(inConversation: true).Should().BeFalse();
        CanMove(busy: true).Should().BeFalse();
    }

    [TestCase(ActionType.MoveToPoint)]
    [TestCase(ActionType.RandomWalk)]
    [TestCase(ActionType.DialogObject)]
    [TestCase(ActionType.AttackObject)]
    [TestCase(ActionType.CastSpell)]
    [TestCase(ActionType.Wait)]
    [TestCase(ActionType.Sit)]
    public void CurrentActionsAreNeverReplacedOrGivenDuplicateMovement(ActionType currentAction)
    {
        CanMove(currentAction: currentAction).Should().BeFalse();
    }

    [Test]
    public void RouteRequiresAnExistingUniqueWaypointInTheSameValidArea()
    {
        ValidPoint(1, 1).Should().BeTrue();
        ValidPoint(OBJECT_INVALID, OBJECT_INVALID).Should().BeFalse();
        ValidPoint(1, 2).Should().BeFalse();
        ValidPoint(1, OBJECT_INVALID).Should().BeFalse();
        ValidPoint(1, 1, type: ObjectType.Creature).Should().BeFalse();
        ValidPoint(1, 1, waypointValid: false).Should().BeFalse();
        ValidPoint(1, 1, duplicateTag: true).Should().BeFalse();
    }

    [TestCase(0, 6)]
    [TestCase(-1, 6)]
    [TestCase(1, 1)]
    [TestCase(6, 6)]
    [TestCase(120, 30)]
    public void ArrivalPauseUsesABoundedAuthoredDuration(int authored, int expected)
    {
        Invoke<int>("PauseSeconds", authored).Should().Be(expected);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NpcInteractionChecksQueryOnlyWindowsTetheredToThatNpc(bool conversationOpen)
    {
        const uint npc = 42;
        var windows = Enum.GetValues<GuiWindowType>().Select(type =>
            (type, type is GuiWindowType.Conversation or GuiWindowType.GuildTasks ? npc : 99u));
        var queries = new List<GuiWindowType>();
        Func<GuiWindowType, bool> isOpen = type =>
        {
            queries.Add(type);
            return conversationOpen && type == GuiWindowType.Conversation;
        };

        InvokeTetherCheck(windows, npc, isOpen).Should().Be(conversationOpen);
        queries.Should().Equal(GuiWindowType.GuildTasks, GuiWindowType.Conversation);
    }

    [Test]
    public void UninitializedWindowCacheDoesNotQueryNativeWindows()
    {
        var queries = 0;
        InvokeTetherCheck(null, 42, _ => { queries++; return true; }).Should().BeFalse();
        queries.Should().Be(0);
    }

    private static bool InvokeTetherCheck(IEnumerable<(GuiWindowType Type, uint Tether)> windows,
        uint tether, Func<GuiWindowType, bool> isOpen)
    {
        var method = typeof(Gui).GetMethod("HasOpenTetheredWindow", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(Gui).FullName, "HasOpenTetheredWindow");
        return (bool)method.Invoke(null, new object[] { windows, tether, isOpen })!;
    }

    private static bool CanMove(bool available = true, bool routeAvailable = true, bool inCombat = false,
        bool inConversation = false, bool busy = false, ActionType currentAction = ActionType.Invalid) =>
        Invoke<bool>("ShouldIssuePatrolAction", available, routeAvailable, inCombat, inConversation, busy, currentAction);

    private static bool ValidPoint(uint area, uint waypointArea, ObjectType type = ObjectType.Waypoint,
        bool waypointValid = true, bool duplicateTag = false) =>
        Invoke<bool>("IsRoutePointValid", area, waypointArea, type, waypointValid, duplicateTag);

    private static T Invoke<T>(string methodName, params object[] arguments)
    {
        var method = typeof(EshanCivilianPatrol).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(EshanCivilianPatrol).FullName, methodName);
        return (T)method.Invoke(null, arguments)!;
    }
}
