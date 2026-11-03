using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the production DM area shout against the engine model of DM possession. The DM chat routing of the chat
/// service itself (the Comms filter, the DM emote style, the DM Talk and Whisper channels, who overhears local speech)
/// is tested on the instance in <c>Tests/Domain/Core/CommunicationChatTests.cs</c> and <c>CommunicationCommsTests.cs</c>.
/// </summary>
public class DMChatRoutingTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint OtherDM = 4;
    private const uint FarPlayer = 5;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionAreaShout()
    {
        _harness = Compile($$"""
            public static class DMPresence
            {
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetControlledCreature")}}
            }

            public static class AreaShout
            {
                {{Method("Feature/ChatCommandDefinition/DMChatCommand.cs", "GetAreaShoutRecipients")}}
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void AreaShout_ReachesEveryoneInTheAreaIncludingAPossessingDM(int possessionType)
    {
        _harness.AddPlayer(OtherDM, isDM: true, area: Cantina);
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(FarPlayer, area: Spaceport);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);

        var recipients = _harness.Invoke<List<uint>>("AreaShout", "GetAreaShoutRecipients", OtherDM);

        recipients.Should().Equal(OtherDM, Player, Creature);
        recipients.Select(_harness.ReceivingPlayer).Should().Equal(OtherDM, Player, DM);
    }

    [Test]
    public void AreaShout_FromAPossessedCreatureStaysInItsArea()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(FarPlayer, area: Spaceport);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, DMPossess, Cantina);

        _harness.Invoke<List<uint>>("AreaShout", "GetAreaShoutRecipients", Creature)
            .Should().Equal(Creature, Player);
    }
}
