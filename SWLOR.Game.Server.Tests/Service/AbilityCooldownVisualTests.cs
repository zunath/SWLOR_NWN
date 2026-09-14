using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityCooldownVisualTests
{
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

    [Test]
    public void GetCooldownTextureName_AllowsMaxLengthResourceNames()
    {
        var textureName = AbilityCooldownVisual.GetCooldownTextureName("ife_wtchflprsnc3", 5);

        textureName.Should().Be("pr5_wtchflprsnc3");
        textureName.Length.Should().Be(16);
    }

    [Test]
    public void GetCooldownTextureName_RejectsUnsupportedSourceTextures()
    {
        AbilityCooldownVisual.GetCooldownTextureName("is_spell_icon", 1)
            .Should()
            .BeNull();
    }

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
