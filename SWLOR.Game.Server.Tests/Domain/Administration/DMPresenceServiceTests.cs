using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM presence instance against the engine model of DM possession (<see cref="DMPossessionEngine"/>),
/// which replaces the Roslyn harness that used to recompile these methods. The native "Appear" state and the avatar
/// fade read nwserver's own structures, so the in-engine DM toolbar tests cover those.
/// </summary>
[TestFixture]
public class DMPresenceServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint Pet = 4;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private DMPossessionEngine _world = null!;
    private DMPresenceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _service = new DMPresenceService();
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    private void PossessingDM(int possessionType)
    {
        _world.AddPlayer(DM, isDM: true, area: Spaceport);
        _world.Possess(DM, Creature, possessionType, Cantina);
    }

    // ---- who is who

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void PossessingDMsAvatar_FailsEveryNativeDMCheck(int possessionType)
    {
        PossessingDM(possessionType);

        GetIsDM(DM).Should().BeFalse();
        GetIsDMPossessed(DM).Should().BeFalse();
        GetIsPC(DM).Should().BeFalse();
        _world.ReceivingPlayer(DM).Should().Be(Invalid, "messages to the avatar no longer reach the DM");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void GetDM_ReturnsTheAvatarBehindAPossessedCreature(int possessionType)
    {
        PossessingDM(possessionType);

        _service.GetDM(Creature).Should().Be(DM);
        _service.GetDM(DM).Should().Be(DM);
    }

    [Test]
    public void GetDM_ReturnsAnythingElseUnchanged()
    {
        _world.AddPlayer(Player);
        _world.Possess(Player, Pet, Familiar);

        _service.GetDM(Player).Should().Be(Player);
        _service.GetDM(Pet).Should().Be(Pet);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void GetControlledCreature_ReturnsTheCreatureAPossessingDMControls(int possessionType)
    {
        PossessingDM(possessionType);

        _service.GetControlledCreature(DM).Should().Be(Creature);
        _service.GetControlledCreature(Creature).Should().Be(Creature);
    }

    [Test]
    public void GetControlledCreature_ReturnsTheAvatarWhenTheDMIsNotPossessing()
    {
        _world.AddPlayer(DM, isDM: true);

        _service.GetControlledCreature(DM).Should().Be(DM);
    }

    [Test]
    public void GetControlledCreature_ReturnsPlayersAndIdleDMsWithoutAnNWNXCall()
    {
        _world.AddPlayer(Player);
        _world.AddPlayer(DM, isDM: true);

        _service.GetControlledCreature(Player).Should().Be(Player);
        _service.GetControlledCreature(DM).Should().Be(DM);
        _world.GameObjectLookups.Should().Be(0, "broadcast loops resolve every online player through this");
    }

    [Test]
    public void GetControlledCreature_IgnoresAPlayerPossessingAFamiliar()
    {
        _world.AddPlayer(Player);
        _world.Possess(Player, Pet, Familiar);

        _service.GetControlledCreature(Player).Should().Be(Player);
    }

    [Test]
    public void IsControlledByDM_IsTrueForADMAvatarAndFalseForPlayers()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.AddPlayer(Player);

        _service.IsControlledByDM(DM).Should().BeTrue();
        _service.IsControlledByDM(Player).Should().BeFalse();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void IsControlledByDM_IsTrueForAPossessingDMsAvatarAndTheCreature(int possessionType)
    {
        PossessingDM(possessionType);

        _service.IsControlledByDM(DM).Should().BeTrue();
        _service.IsControlledByDM(Creature).Should().BeTrue();
    }

    [Test]
    public void IsControlledByDM_IsFalseForAPlayerPossessingAFamiliar()
    {
        _world.AddPlayer(Player);
        _world.Possess(Player, Pet, Familiar);

        _service.IsControlledByDM(Player).Should().BeFalse();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void IsDMPresentInArea_CountsTheCreatureAPossessingDMControls(int possessionType)
    {
        PossessingDM(possessionType);

        _service.IsDMPresentInArea(Cantina).Should().BeTrue();
        _service.IsDMPresentInArea(Spaceport).Should().BeTrue("the DM's avatar is still there");
    }

    [Test]
    public void IsDMPresentInArea_CountsADMAvatar()
    {
        _world.AddPlayer(DM, isDM: true, area: Cantina);

        _service.IsDMPresentInArea(Cantina).Should().BeTrue();
        _service.IsDMPresentInArea(Spaceport).Should().BeFalse();
    }

    [Test]
    public void IsDMPresentInArea_IgnoresPlayers()
    {
        _world.AddPlayer(Player, area: Cantina);
        _world.AddPlayer(DM, isDM: true, area: Spaceport);
        _world.Possess(Player, Pet, Familiar, Cantina);

        _service.IsDMPresentInArea(Cantina).Should().BeFalse();
    }

    // ---- feedback and audit text

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void SendMessage_ReachesAPossessingDM(int possessionType)
    {
        PossessingDM(possessionType);

        _service.SendMessage(DM, "Restarting soon.");

        _world.Messages.Should().Equal((DM, "Restarting soon."));
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void DescribeDM_KeepsTheAccountAndCDKeyOfAPossessingDM(int possessionType)
    {
        PossessingDM(possessionType);

        _service.DescribeDM(DM).Should().Be("Name1 (account1) [KEY1]");
        _service.DescribeDM(Creature).Should().Be("Name1 (account1) [KEY1]");
    }

    // ---- moving people

    [Test]
    public void JumpTo_SendsTheCreatureTheDMControlsToTheLocation()
    {
        PossessingDM(DMPossess);
        var destination = EngineStructures.Create<Location>(77);

        _service.JumpTo(DM, destination, "the cantina");

        var assigned = _world.Engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Creature);
        ((Action)assigned.Arguments[1]!)();
        _world.Engine.CallsTo("ClearAllActions").Should().ContainSingle();
        _world.Engine.CallsTo("JumpToLocation").Should().ContainSingle().Which.Arguments[0].Should().BeSameAs(destination);
    }

    [Test]
    public void Summon_RefusesATargetThatNoLongerExists()
    {
        _world.AddPlayer(DM, isDM: true);

        _service.Summon(DM, Invalid).Should().Be("That target no longer exists.");

        _world.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void Summon_RefusesACreatureAnotherDMIsPossessing()
    {
        const uint otherDM = 5;
        _world.AddPlayer(DM, isDM: true);
        _world.AddPlayer(otherDM, isDM: true);
        _world.Possess(otherDM, Creature, DMPossess);

        _service.Summon(DM, Creature).Should().Be("A DM is possessing that creature.");

        _world.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void Summon_BringsACreatureToTheCreatureTheDMControls()
    {
        PossessingDM(DMPossess);
        const uint npc = 50;
        var spot = EngineStructures.Create<Location>(88);
        _world.Engine.SetReturn("GetLocation", spot);

        var error = _service.Summon(DM, npc);

        error.Should().BeEmpty();
        var assigned = _world.Engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(npc);
        ((Action)assigned.Arguments[1]!)();
        _world.Engine.CallsTo("GetLocation").Should().ContainSingle().Which.Arguments[0].Should().Be(Creature);
        _world.Engine.CallsTo("JumpToLocation").Should().ContainSingle().Which.Arguments[0].Should().BeSameAs(spot);
    }

    [Test]
    public void Boot_DisconnectsThePlayerWithAReason()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.AddPlayer(Player);

        _service.Boot(DM, Player);

        var boot = _world.Engine.CallsTo("BootPC").Should().ContainSingle().Subject;
        boot.Arguments[0].Should().Be(Player);
        boot.Arguments[1].Should().Be("You have been disconnected by a DM.");
    }

    // ---- DM windows

    [Test]
    public void ShowWindow_LeavesAWindowAlreadyShownOnTheControlledCreatureOpen()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.OpenWindow(DM, Gui.BuildWindowId(GuiWindowType.DMToolbar));

        _service.ShowWindow(DM, GuiWindowType.DMToolbar);

        _world.Engine.CallsTo("NuiDestroy").Should().BeEmpty("the window is already where the DM controls");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void ShowWindow_ClosesACopyLeftOnTheAvatarAndKeepsTheOneOnTheCreature(int possessionType)
    {
        _world.AddPlayer(DM, isDM: true);
        var windowId = Gui.BuildWindowId(GuiWindowType.DMToolbar);
        _world.OpenWindow(DM, windowId);
        _service.ShowWindow(DM, GuiWindowType.DMToolbar);
        _world.Possess(DM, Creature, possessionType);
        _world.OpenWindow(Creature, windowId);

        _service.ShowWindow(DM, GuiWindowType.DMToolbar);

        var destroyed = _world.Engine.CallsTo("NuiDestroy").Should().ContainSingle().Subject;
        destroyed.Arguments[0].Should().Be(DM, "the avatar's copy is closed");
    }

    [Test]
    public void ToggleWindow_ClosesTheCopyOnAnObjectTheDMNoLongerControls()
    {
        _world.AddPlayer(DM, isDM: true);
        var windowId = Gui.BuildWindowId(GuiWindowType.DMToolbar);
        _world.OpenWindow(DM, windowId);
        _service.ShowWindow(DM, GuiWindowType.DMToolbar);
        _world.Possess(DM, Creature, DMPossess);
        _world.OpenWindow(Creature, windowId);

        _service.ToggleWindow(DM, GuiWindowType.DMToolbar);

        var destroyed = _world.Engine.CallsTo("NuiDestroy").Should().ContainSingle().Subject;
        destroyed.Arguments[0].Should().Be(DM, "the copy on the avatar is moved to the creature");
    }

    [Test]
    public void CloseWindow_ClosesTheWindowOnTheControlledCreatureAndTheAvatar()
    {
        PossessingDM(DMPossess);
        var windowId = Gui.BuildWindowId(GuiWindowType.DMToolbar);
        _world.OpenWindow(Creature, windowId);
        _world.OpenWindow(DM, windowId);

        _service.CloseWindow(Creature, GuiWindowType.DMToolbar);

        _world.Engine.CallsTo("NuiDestroy").Select(call => (uint)call.Arguments[0]!).Should().BeEquivalentTo(new[] { Creature, DM });
    }

    [Test]
    public void GetShownOn_ReturnsOnlyTheActorForAPlayer()
    {
        _world.AddPlayer(Player);

        _service.GetShownOn(Player, GuiWindowType.DMToolbar).Should().Equal(Player);
    }

    [Test]
    public void GetShownOn_ReturnsNothingForAnObjectThatIsGone()
    {
        _service.GetShownOn(Invalid, GuiWindowType.DMToolbar).Should().BeEmpty();
    }

    [Test]
    public void GetShownOn_ForADMListsTheAvatarAndTheObjectTheWindowWasLastShownOn()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.OpenWindow(DM, Gui.BuildWindowId(GuiWindowType.DMToolbar));
        _service.ShowWindow(DM, GuiWindowType.DMToolbar);
        _world.Possess(DM, Creature, DMPossess);

        _service.GetShownOn(DM, GuiWindowType.DMToolbar).Should().Equal(DM);

        _world.Release(DM);
        _world.AddPlayer(Player);
        _service.GetShownOn(Player, GuiWindowType.DMToolbar).Should().Equal(Player);
    }
}
