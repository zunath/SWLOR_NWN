using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the production DMPresence helpers against the engine model of DM possession.
/// </summary>
public class DMPresencePossessionTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint Pet = 4;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionDMPresence()
    {
        _harness = Compile($$"""
            public static class DMPresence
            {
                {{Method("Service/DMPresence.cs", "GetControlledCreature")}}
                {{Method("Service/DMPresence.cs", "IsControlledByDM")}}
                {{Method("Service/DMPresence.cs", "IsDMPresentInArea")}}
                {{Method("Service/DMPresence.cs", "SendMessage")}}
            }
            """);
    }

    [SetUp]
    public void Reset() => _harness.Reset();

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void PossessingDMsAvatar_FailsEveryNativeDMCheck(int possessionType)
    {
        PossessingDM(possessionType);

        _harness.Invoke<bool>("World", "GetIsDM", DM).Should().BeFalse();
        _harness.Invoke<bool>("World", "GetIsDMPossessed", DM).Should().BeFalse();
        _harness.Invoke<bool>("World", "GetIsPC", DM).Should().BeFalse();
        _harness.ReceivingPlayer(DM).Should().Be(Invalid, "messages to the avatar no longer reach the DM");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void GetControlledCreature_ReturnsTheCreatureAPossessingDMControls(int possessionType)
    {
        PossessingDM(possessionType);

        GetControlledCreature(DM).Should().Be(Creature);
        GetControlledCreature(Creature).Should().Be(Creature);
    }

    [Test]
    public void GetControlledCreature_ReturnsTheAvatarWhenTheDMIsNotPossessing()
    {
        _harness.AddPlayer(DM, isDM: true);

        GetControlledCreature(DM).Should().Be(DM);
    }

    [Test]
    public void GetControlledCreature_IgnoresAPlayerPossessingAFamiliar()
    {
        _harness.AddPlayer(Player);
        _harness.Possess(Player, Pet, Familiar);

        GetControlledCreature(Player).Should().Be(Player);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void SendMessage_ReachesAPossessingDM(int possessionType)
    {
        PossessingDM(possessionType);

        _harness.Invoke("DMPresence", "SendMessage", DM, "Restarting soon.");

        _harness.Messages.Should().Equal((DM, "Restarting soon."));
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void IsDMPresentInArea_CountsTheCreatureAPossessingDMControls(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);

        IsDMPresentInArea(Cantina).Should().BeTrue();
        IsDMPresentInArea(Spaceport).Should().BeTrue("the DM's avatar is still there");
    }

    [Test]
    public void IsDMPresentInArea_CountsADMAvatar()
    {
        _harness.AddPlayer(DM, isDM: true, area: Cantina);

        IsDMPresentInArea(Cantina).Should().BeTrue();
        IsDMPresentInArea(Spaceport).Should().BeFalse();
    }

    [Test]
    public void IsDMPresentInArea_IgnoresPlayers()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(Player, Pet, Familiar, Cantina);

        IsDMPresentInArea(Cantina).Should().BeFalse();
    }

    private void PossessingDM(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);
    }

    private uint GetControlledCreature(uint actor) => _harness.Invoke<uint>("DMPresence", "GetControlledCreature", actor);

    private bool IsDMPresentInArea(uint area) => _harness.Invoke<bool>("DMPresence", "IsDMPresentInArea", area);
}
