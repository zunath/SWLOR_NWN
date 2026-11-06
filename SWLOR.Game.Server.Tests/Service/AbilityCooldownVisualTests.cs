using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityCooldownVisualTests
{
    // The code under test reaches converted services through their static facades, which resolve from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    /// <summary>Checks that feat artwork resolves to native recharge resource names.</summary>
    [Test]
    public void GetCooldownTextureName_BuildsScriptCompatibleNames()
    {
        AbilityCooldownVisual.GetCooldownTextureName("ife_absdef1", 0)
            .Should()
            .Be("pr0_absdef1");

        AbilityCooldownVisual.GetCooldownTextureName("ife_absdef1", 5)
            .Should()
            .Be("pr5_absdef1");
    }

    /// <summary>Checks that fitted inventory artwork uses the same recharge naming convention.</summary>
    [Test]
    public void GetCooldownTextureName_UsesEquippedShipModuleArtwork()
    {
        AbilityCooldownVisual.GetCooldownTextureName("iit_ess2_062", 3).Should().Be("pr3_ess2_062");
    }

    /// <summary>Checks that generated resource names respect the native length limit.</summary>
    [Test]
    public void GetCooldownTextureName_AllowsMaxLengthResourceNames()
    {
        var textureName = AbilityCooldownVisual.GetCooldownTextureName("ife_wtchflprsnc3", 5);

        textureName.Should().Be("pr5_wtchflprsnc3");
        textureName.Length.Should().Be(16);
    }

    /// <summary>Checks that unsupported texture categories do not create recharge resource names.</summary>
    [Test]
    public void GetCooldownTextureName_RejectsUnsupportedSourceTextures()
    {
        AbilityCooldownVisual.GetCooldownTextureName("is_spell_icon", 1)
            .Should()
            .BeNull();
    }

    /// <summary>Checks the six elapsed-time frames and the expiry boundary.</summary>
    [Test]
    public void CalculateCooldownStage_AdvancesThroughSixFrames()
    {
        var startedAt = new DateTime(2026, 5, 16, 12, 0, 0, DateTimeKind.Utc);
        var endsAt = startedAt.AddSeconds(60);

        AbilityCooldownVisual.CalculateCooldownStage(startedAt, startedAt, endsAt)
            .Should()
            .Be(0);

        AbilityCooldownVisual.CalculateCooldownStage(startedAt.AddSeconds(10), startedAt, endsAt)
            .Should()
            .Be(1);

        AbilityCooldownVisual.CalculateCooldownStage(startedAt.AddSeconds(50), startedAt, endsAt)
            .Should()
            .Be(5);

        AbilityCooldownVisual.CalculateCooldownStage(endsAt, startedAt, endsAt)
            .Should()
            .Be(-1);
    }
}
