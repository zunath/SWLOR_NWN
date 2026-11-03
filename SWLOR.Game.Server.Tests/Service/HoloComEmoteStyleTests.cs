using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class HoloComEmoteStyleTests
{
    [Test]
    public void ChatSplitting_UsesTheHologramOwnersEmoteStyle()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "Communication.cs")).Replace("\r\n", "\n");

        // HoloCom relays arrive with the hologram creature as sender; its owner holds the emote style.
        source.Should().Contain("GetEmoteStyle(GetEffectiveChatSpeaker(sender)) == EmoteStyle.Regular");
        source.Should().NotContain("GetEmoteStyle(sender) == EmoteStyle.Regular");
        source.Should().Contain("HoloCom.GetHoloGramOwner(sender)");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
