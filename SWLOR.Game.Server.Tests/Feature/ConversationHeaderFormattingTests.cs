using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Feature;

public sealed class ConversationHeaderFormattingTests
{
    [Test]
    public void IdentityBrokerSummary_IsPlainContinuousHeaderText()
    {
        var method = typeof(IdentityBrokerDialog).GetMethod(
            "BuildDisguiseSummary",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        var disguise = new PlayerDisguise
        {
            PrivateName = "Quiet Ledger",
            Descriptor = "A guarded traveler"
        };
        var summary = (string)method!.Invoke(null, new object[] { disguise })!;
        var blocks = ConversationMarkup.ParseLegacyColors(summary, ConversationTextStyle.Normal);

        blocks.Should().ContainSingle();
        blocks[0].Text.Should().Be(
            "Private Slot Label: Quiet Ledger\nPublic Description: A guarded traveler");
        blocks[0].Style.Should().Be(ConversationTextStyle.Normal);
    }

    [TestCase(1, "BLUE")]
    [TestCase(2, "GREEN")]
    [TestCase(3, "RED")]
    public void CoxxionDoorColors_DoNotInjectHeaderColorRuns(int colorId, string expected)
    {
        var method = typeof(CoxxionTerminalDialog).GetMethod(
            "GetColorString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();

        var value = (string)method!.Invoke(new CoxxionTerminalDialog(), new object[] { colorId })!;
        var blocks = ConversationMarkup.ParseLegacyColors(value, ConversationTextStyle.Normal);

        blocks.Should().ContainSingle();
        blocks[0].Text.Should().Be(expected);
        blocks[0].Style.Should().Be(ConversationTextStyle.Normal);
    }
}
