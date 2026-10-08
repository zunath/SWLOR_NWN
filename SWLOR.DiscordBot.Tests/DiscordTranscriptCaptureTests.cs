using System.Reflection;
using System.Text.Json;
using Discord;
using NUnit.Framework;
using SWLOR.DiscordBot.Discord;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class DiscordTranscriptCaptureTests
{
    [Test]
    public void PollOnlyMessagePreservesQuestionAnswersEmojisAndVoteResults()
    {
        var question = Sdk<PollMedia>("Where is the evidence?", new Emoji("🔎"));
        var answer = Sdk<PollAnswer>(1U, Sdk<PollMedia>("In the screenshot", null));
        var count = Sdk<PollAnswerCounts>(7UL, 1U, false);
        var results = Sdk<PollResults>(true, new[] { count });
        var poll = Sdk<Poll>(question, new[] { answer }, DateTimeOffset.UnixEpoch.AddDays(1), true, (PollLayout)1, results);
        var retained = DiscordTranscriptCapture.Capture(Message(poll: poll));
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var captured = metadata.RootElement.GetProperty("Poll");
        Assert.That(captured.GetProperty("Question").GetProperty("Text").GetString(), Is.EqualTo("Where is the evidence?"));
        Assert.That(captured.GetProperty("Question").GetProperty("Emoji").GetProperty("Name").GetString(), Is.EqualTo("🔎"));
        Assert.That(captured.GetProperty("Answers")[0].GetProperty("PollMedia").GetProperty("Text").GetString(), Is.EqualTo("In the screenshot"));
        Assert.That(captured.GetProperty("Results").GetProperty("IsFinalized").GetBoolean(), Is.True);
        Assert.That(captured.GetProperty("Results").GetProperty("AnswerCounts")[0].EnumerateObject(), Is.Not.Empty);
        Assert.That(retained.Content, Is.Empty);
    }

    [Test]
    public void StickerOnlyMessageKeepsItsIdNameAndFormat()
    {
        var sticker = Proxy<IStickerItem>(new Dictionary<string, object?>
        {
            ["Id"] = 42UL, ["Name"] = "Evidence sticker", ["Format"] = StickerFormatType.Png
        });
        var retained = DiscordTranscriptCapture.Capture(Message(stickers: [sticker]));
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var item = metadata.RootElement.GetProperty("Stickers")[0];
        Assert.That(item.GetProperty("Id").GetUInt64(), Is.EqualTo(42));
        Assert.That(item.GetProperty("Name").GetString(), Is.EqualTo("Evidence sticker"));
        Assert.That(item.GetProperty("Format").GetInt32(), Is.EqualTo((int)StickerFormatType.Png));
    }

    [Test]
    public void NestedButtonsAndComponentsV2KeepVisibleLabelsAndText()
    {
        var components = new ComponentBuilder().WithButton("Close ticket", "v1:close:evidence", ButtonStyle.Danger).Build().Components;
        var text = new TextDisplayBuilder("<important> evidence").Build();
        var message = Message(components: components.Concat(new IMessageComponent[] { text }).ToArray());
        var retained = DiscordTranscriptCapture.Capture(message);
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var captured = metadata.RootElement.GetProperty("Components");
        Assert.That(captured[0].GetProperty("Components")[0].GetProperty("Label").GetString(), Is.EqualTo("Close ticket"));
        Assert.That(captured[0].GetProperty("Components")[0].GetProperty("CustomId").GetString(), Is.EqualTo("v1:close:evidence"));
        Assert.That(captured[1].GetProperty("Content").GetString(), Is.EqualTo("<important> evidence"));
    }

    [Test]
    public void ForwardedOnlyMessageKeepsContentAndCopiesItsAttachmentIntoTheArchivePlan()
    {
        var attachment = Proxy<IAttachment>(new Dictionary<string, object?>
        {
            ["Id"] = 88UL, ["Filename"] = "evidence.txt", ["Size"] = 7,
            ["Url"] = "https://cdn.discordapp.com/attachments/88/evidence.txt?ex=old"
        });
        var snapshot = Sdk<MessageSnapshot>(Message("Forwarded evidence", attachments: [attachment]));
        var retained = DiscordTranscriptCapture.Capture(Message(forwarded: [snapshot]));
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var forwarded = metadata.RootElement.GetProperty("ForwardedMessages")[0];
        Assert.That(forwarded.GetProperty("Content").GetString(), Is.EqualTo("Forwarded evidence"));
        Assert.That(forwarded.GetProperty("Attachments")[0].GetProperty("Resource").GetString(),
            Is.EqualTo("https://cdn.discordapp.com/attachments/88/evidence.txt"));
        Assert.That(retained.Attachments.Single().Id, Is.EqualTo(88));
        Assert.That(retained.Attachments.Single().Url, Does.EndWith("?ex=old"), "Downloads retain the signed resource URL.");
        Assert.That(retained.MetadataJson, Does.Not.Contain("Channel"), "Do not serialize SDK channel or client graphs.");
    }

    [Test]
    public void ForwardedAttachmentSignatureRefreshDoesNotChangeMessageIdentity()
    {
        var values = new Dictionary<string, object?>
        {
            ["Id"] = 88UL, ["Filename"] = "evidence.txt", ["Size"] = 7,
            ["Url"] = "https://cdn.discordapp.com/attachments/88/evidence.txt?ex=old"
        };
        var attachment = Proxy<IAttachment>(values);
        var message = Message(forwarded: [Sdk<MessageSnapshot>(Message("Evidence", attachments: [attachment]))]);
        var first = DiscordTranscriptCapture.Capture(message);
        values["Url"] = "https://cdn.discordapp.com/attachments/88/evidence.txt?ex=fresh";
        var current = DiscordTranscriptCapture.Capture(message);
        Assert.That(current.MetadataJson, Is.EqualTo(first.MetadataJson));
        Assert.That(current.Attachments.Single().Url, Is.Not.EqualTo(first.Attachments.Single().Url));
    }

    [Test]
    public void OverlyNestedForwardedContentFailsBeforeClaimingACompleteSnapshot()
    {
        var nested = Message("evidence");
        for (var level = 0; level < 5; level++) nested = Message(forwarded: [Sdk<MessageSnapshot>(nested)]);
        Assert.Throws<InvalidDataException>(() => DiscordTranscriptCapture.Capture(nested));
    }

    private static IUserMessage Message(string content = "", Poll? poll = null,
        IReadOnlyCollection<IMessageComponent>? components = null, IReadOnlyCollection<IStickerItem>? stickers = null,
        IReadOnlyCollection<MessageSnapshot>? forwarded = null, IReadOnlyCollection<IAttachment>? attachments = null) =>
        Proxy<IUserMessage>(new Dictionary<string, object?>
        {
            ["Id"] = 10UL, ["Author"] = Proxy<IUser>(new Dictionary<string, object?> { ["Id"] = 55UL, ["Username"] = "member" }),
            ["Content"] = content, ["Timestamp"] = DateTimeOffset.UnixEpoch, ["Attachments"] = attachments ?? [],
            ["Embeds"] = Array.Empty<IEmbed>(), ["Components"] = components ?? [], ["Stickers"] = stickers ?? [],
            ["ForwardedMessages"] = forwarded ?? [], ["Poll"] = poll, ["Type"] = MessageType.Default,
            ["Flags"] = (MessageFlags?)MessageFlags.None, ["EditedTimestamp"] = (DateTimeOffset?)null
        });

    private static T Sdk<T>(params object?[] arguments) => (T)Activator.CreateInstance(typeof(T),
        BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null)!;

    private static T Proxy<T>(Dictionary<string, object?> properties) where T : class
    {
        var instance = DispatchProxy.Create<T, TranscriptPayloadProxy>();
        ((TranscriptPayloadProxy)(object)instance).Properties = properties;
        return instance;
    }
}
