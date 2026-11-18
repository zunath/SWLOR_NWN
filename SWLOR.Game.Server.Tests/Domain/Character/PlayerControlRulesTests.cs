using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Who is a player and who fights for one, which reads only the engine.</summary>
[TestFixture]
public class PlayerControlRulesTests
{
    private const uint PlayerOne = 1001;
    private const uint PlayerTwo = 1002;
    private const uint Pet = 2001;
    private const uint Npc = 3001;
    private const uint DmPlayer = 4001;
    private const uint ObjectInvalid = 0x7F000000;

    private IDisposable _scope = null!;

    [SetUp]
    public void SetUp()
    {
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is PlayerOne or PlayerTwo or DmPlayer);
        engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == DmPlayer);
        engine.SetReturnHandler("GetIsDMPossessed", _ => false);
        engine.SetReturnHandler("GetMaster", args => (uint)args[0]! == Pet ? PlayerOne : ObjectInvalid);
        _scope = NWScriptRuntime.Override(engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [Test]
    public void OnlyAValidNonDmPlayerIsAPlayer()
    {
        PlayerControlRules.IsPlayer(PlayerOne).Should().BeTrue();
        PlayerControlRules.IsPlayer(DmPlayer).Should().BeFalse();
        PlayerControlRules.IsPlayer(Npc).Should().BeFalse();
        PlayerControlRules.IsPlayer(ObjectInvalid).Should().BeFalse();
    }

    [Test]
    public void APlayerOrAPlayersPetIsControlledByThePlayer()
    {
        PlayerControlRules.GetControllingPlayer(PlayerOne).Should().Be(PlayerOne);
        PlayerControlRules.GetControllingPlayer(Pet).Should().Be(PlayerOne);
        PlayerControlRules.GetControllingPlayer(Npc).Should().Be(ObjectInvalid);
        PlayerControlRules.GetControllingPlayer(ObjectInvalid).Should().Be(ObjectInvalid);
    }

    [Test]
    public void PlayerAgainstPlayerNeedsAPlayerTargetAndADifferentControllingPlayer()
    {
        PlayerControlRules.IsPlayerAgainstPlayer(PlayerTwo, PlayerOne).Should().BeTrue();
        PlayerControlRules.IsPlayerAgainstPlayer(Pet, PlayerTwo).Should().BeTrue();
        PlayerControlRules.IsPlayerAgainstPlayer(PlayerOne, PlayerOne).Should().BeFalse();
        PlayerControlRules.IsPlayerAgainstPlayer(Pet, PlayerOne).Should().BeFalse();
        PlayerControlRules.IsPlayerAgainstPlayer(Npc, PlayerOne).Should().BeFalse();
        PlayerControlRules.IsPlayerAgainstPlayer(PlayerOne, Npc).Should().BeFalse();
    }
}
