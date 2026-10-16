using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

public class HoloComCallTargetTests
{
    private const uint Character = 1;
    private const uint Controlled = 2;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionDMCheck()
    {
        _harness = Compile($$"""
            public static class DMPresence
            {
                {{Method("Service/DMPresence.cs", "IsControlledByDM")}}
            }
            """);
    }

    [SetUp]
    public void Reset() => _harness.Reset();

    [Test]
    public void DMAvatarOrPlayerDM_IsNotCallable()
    {
        _harness.AddPlayer(Character, isDM: true);

        IsControlledByDM(Character).Should().BeTrue();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void DMAvatar_IsNotCallableWhilePossessingACreature(int possessionType)
    {
        _harness.AddPlayer(Character, isDM: true);
        _harness.Possess(Character, Controlled, possessionType);

        IsControlledByDM(Character).Should().BeTrue();
    }

    [Test]
    public void Player_IsCallable()
    {
        _harness.AddPlayer(Character);

        IsControlledByDM(Character).Should().BeFalse();
    }

    [Test]
    public void PlayerPossessingAFamiliar_IsCallable()
    {
        _harness.AddPlayer(Character);
        _harness.Possess(Character, Controlled, Familiar);

        IsControlledByDM(Character).Should().BeFalse();
    }

    [Test]
    public void HoloComMenu_FiltersCallTargetsThroughTheDMCheck()
    {
        ReadServerFile("Feature/DialogDefinition/HoloComDialog.cs")
            .Should().Contain("DMPresence.IsControlledByDM(pc)");
    }

    private bool IsControlledByDM(uint player) => _harness.Invoke<bool>("DMPresence", "IsControlledByDM", player);
}
