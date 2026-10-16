using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the appearance editor's height and head size feedback against the engine model of DM possession, where
/// messages sent to a possessing DM's avatar are dropped.
/// </summary>
public class AppearanceEditorPossessionFeedbackTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionScaleFeedback()
    {
        _harness = Compile($$"""
            public static class DMPresence
            {
                {{Method("Service/DMPresence.cs", "GetControlledCreature")}}
                {{Method("Service/DMPresence.cs", "SendMessage")}}
            }

            public sealed class Editor
            {
                private readonly uint Player;
                private Editor(uint player) => Player = player;

                {{Method("Feature/GuiDefinition/ViewModel/AppearanceEditorViewModel.cs", "SendScaleMessage")}}

                public static void Report(uint windowOwner, string message) => new Editor(windowOwner).SendScaleMessage(message);
            }
            """);
    }

    [SetUp]
    public void Reset() => _harness.Reset();

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void FeedbackReachesADMEditingThePossessedCreature(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        _harness.Possess(DM, Creature, possessionType);

        _harness.Invoke("Editor", "Report", Creature, "Height: 1.01");

        _harness.Messages.Should().Equal((DM, "Height: 1.01"));
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void FeedbackReachesAPossessingDMWhoseEditorWasOpenedOnTheirAvatar(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        _harness.Possess(DM, Creature, possessionType);

        _harness.Invoke("Editor", "Report", DM, "You cannot increase your head size any further.");

        _harness.Messages.Should().Equal((DM, "You cannot increase your head size any further."));
    }

    [Test]
    public void FeedbackReachesAPlayer()
    {
        _harness.AddPlayer(Player);

        _harness.Invoke("Editor", "Report", Player, "Head Size: 0.99");

        _harness.Messages.Should().Equal((Player, "Head Size: 0.99"));
    }
}
