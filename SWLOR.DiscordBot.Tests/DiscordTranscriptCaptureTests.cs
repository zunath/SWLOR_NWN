using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Discord;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Discord;
using SWLOR.DiscordBot.Persistence;

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
    public void ReactionOnlyMessageKeepsEmojiIdentityAndNormalAndSuperReactionMetadata()
    {
        var retained = DiscordTranscriptCapture.Capture(Message(reactions: new Dictionary<IEmote, ReactionMetadata>
        {
            [new Emoji("✅")] = Reaction(5, 3, 2, true, [new Color(0xFFAA11U), new Color(0x112233U)]),
            [new Emote(42, "acknowledged", true)] = Reaction(2, 2, 0, false)
        }));
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var reactions = metadata.RootElement.GetProperty("Reactions");
        var unicode = reactions[0];
        var custom = reactions[1];
        Assert.Multiple(() =>
        {
            Assert.That(retained.Content, Is.Empty);
            Assert.That(unicode.GetProperty("Emoji").GetProperty("Id").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(unicode.GetProperty("Emoji").GetProperty("Name").GetString(), Is.EqualTo("✅"));
            Assert.That(unicode.GetProperty("ReactionCount").GetInt32(), Is.EqualTo(5));
            Assert.That(unicode.GetProperty("NormalCount").GetInt32(), Is.EqualTo(3));
            Assert.That(unicode.GetProperty("BurstCount").GetInt32(), Is.EqualTo(2));
            Assert.That(unicode.GetProperty("IsMe").GetBoolean(), Is.True);
            Assert.That(unicode.GetProperty("BurstColors").EnumerateArray().Select(color => color.GetUInt32()),
                Is.EqualTo(new[] { 0x112233U, 0xFFAA11U }));
            Assert.That(custom.GetProperty("Emoji").GetProperty("Id").GetUInt64(), Is.EqualTo(42));
            Assert.That(custom.GetProperty("Emoji").GetProperty("Name").GetString(), Is.EqualTo("acknowledged"));
            Assert.That(custom.GetProperty("Emoji").GetProperty("Animated").GetBoolean(), Is.True);
            Assert.That(custom.GetProperty("IsMe").GetBoolean(), Is.False);
            Assert.That(custom.GetProperty("BurstColors").EnumerateArray(), Is.Empty);
            Assert.That(custom.GetProperty("Emoji").EnumerateObject().Select(property => property.Name),
                Is.EquivalentTo(new[] { "Id", "Name", "Animated" }), "Do not capture emote user/client graphs.");
        });
    }

    [Test]
    public void ReactionAndSuperReactionColorOrderingDoesNotChangeTranscriptMetadata()
    {
        var firstReactions = new Dictionary<IEmote, ReactionMetadata>
        {
            [new Emote(43, "acknowledged", true)] = Reaction(1, 1, 0, false),
            [new Emoji("✅")] = Reaction(3, 1, 2, true, [new Color(0xFFAA11U), new Color(0x112233U)]),
            [new Emote(42, "acknowledged", false)] = Reaction(1, 1, 0, true),
            [new Emoji("👍")] = Reaction(1, 1, 0, false)
        };
        var reordered = firstReactions.Reverse().ToDictionary(reaction => reaction.Key, reaction => reaction.Value);
        reordered[new Emoji("✅")] = Reaction(3, 1, 2, true, [new Color(0x112233U), new Color(0xFFAA11U)]);
        var first = DiscordTranscriptCapture.Capture(Message(reactions: firstReactions));
        var current = DiscordTranscriptCapture.Capture(Message(reactions: reordered));
        Assert.That(current.MetadataJson, Is.EqualTo(first.MetadataJson));
        using var metadata = JsonDocument.Parse(current.MetadataJson!);
        Assert.That(metadata.RootElement.GetProperty("Reactions").EnumerateArray()
            .Where(reaction => reaction.GetProperty("Emoji").GetProperty("Id").ValueKind != JsonValueKind.Null)
            .Select(reaction => reaction.GetProperty("Emoji").GetProperty("Id").GetUInt64()), Is.EqualTo(new[] { 42UL, 43UL }));
    }

    [TestCase("count")]
    [TestCase("normal-count")]
    [TestCase("super-count")]
    [TestCase("self")]
    [TestCase("color")]
    [TestCase("added")]
    [TestCase("removed")]
    [TestCase("emoji-id")]
    [TestCase("emoji-name")]
    [TestCase("animated")]
    public void ReactionOnlyChangesAlterTranscriptMetadata(string change)
    {
        var emote = new Emote(42, "acknowledged", false);
        var reactions = new Dictionary<IEmote, ReactionMetadata>
        {
            [emote] = Reaction(5, 3, 2, true, [new Color(0x112233U)])
        };
        var message = Message(reactions: reactions);
        var original = DiscordTranscriptCapture.Capture(message);
        switch (change)
        {
            case "count": reactions[emote] = Reaction(6, 3, 2, true, [new Color(0x112233U)]); break;
            case "normal-count": reactions[emote] = Reaction(5, 4, 2, true, [new Color(0x112233U)]); break;
            case "super-count": reactions[emote] = Reaction(5, 3, 3, true, [new Color(0x112233U)]); break;
            case "self": reactions[emote] = Reaction(5, 3, 2, false, [new Color(0x112233U)]); break;
            case "color": reactions[emote] = Reaction(5, 3, 2, true, [new Color(0xFFAA11U)]); break;
            case "added": reactions.Add(new Emoji("👍"), Reaction(1, 1, 0, false)); break;
            case "removed": reactions.Clear(); break;
            default:
                var replacement = change switch
                {
                    "emoji-id" => new Emote(43, "acknowledged", false),
                    "emoji-name" => new Emote(42, "resolved", false),
                    _ => new Emote(42, "acknowledged", true)
                };
                var value = reactions[emote];
                reactions.Clear();
                reactions.Add(replacement, value);
                break;
        }
        var current = DiscordTranscriptCapture.Capture(message);
        Assert.That(current.MetadataJson, Is.Not.EqualTo(original.MetadataJson));
        Assert.That(current.Content, Is.EqualTo(original.Content));
        Assert.That(current.Timestamp, Is.EqualTo(original.Timestamp));
    }

    [Test]
    public void ForwardedMessageReactionsArePreservedWhenTheSdkSnapshotIncludesThem()
    {
        var reactions = new Dictionary<IEmote, ReactionMetadata> { [new Emoji("✅")] = Reaction(2, 2, 0, false) };
        var message = Message(forwarded: [Sdk<MessageSnapshot>(Message("Forwarded evidence", reactions: reactions))]);
        var retained = DiscordTranscriptCapture.Capture(message);
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var forwarded = metadata.RootElement.GetProperty("ForwardedMessages")[0].GetProperty("Metadata").GetProperty("Reactions")[0];
        Assert.That(forwarded.GetProperty("Emoji").GetProperty("Name").GetString(), Is.EqualTo("✅"));
        Assert.That(forwarded.GetProperty("ReactionCount").GetInt32(), Is.EqualTo(2));
        reactions[new Emoji("✅")] = Reaction(3, 3, 0, false);
        Assert.That(DiscordTranscriptCapture.Capture(message).MetadataJson, Is.Not.EqualTo(retained.MetadataJson));
    }

    [Test]
    public void ReactionMetadataUsesTheExistingCumulativeTranscriptBudget()
    {
        var retained = DiscordTranscriptCapture.Capture(Message(reactions: new Dictionary<IEmote, ReactionMetadata>
        {
            [new Emoji("✅")] = Reaction(1, 1, 0, false)
        }));
        var budget = new TranscriptContentBudget(1024);
        Assert.DoesNotThrow(() => budget.Add(retained));
        Assert.Throws<InvalidDataException>(() => budget.Add(retained with { Id = 11 }));

        var manyReactions = Enumerable.Range(1, 8).ToDictionary(id => (IEmote)new Emote((ulong)id, "acknowledged", false),
            _ => Reaction(1, 1, 0, false));
        Assert.DoesNotThrow(() => new TranscriptContentBudget(1024).Add(DiscordTranscriptCapture.Capture(Message())));
        Assert.Throws<InvalidDataException>(() => new TranscriptContentBudget(1024)
            .Add(DiscordTranscriptCapture.Capture(Message(reactions: manyReactions))));
    }

    [Test]
    public async Task CapturedReactionMetadataIsPreservedInArchiveJsonAndEncodedInHtml()
    {
        var archiveRoot = Path.Combine(Path.GetTempPath(), "swlor-discord-reaction-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient();
            var archive = new FileTranscriptArchive(new BotConfiguration
            {
                Tickets = new TicketOptions { ArchiveDirectory = archiveRoot, CopyAttachments = false }
            }, client);
            var retained = DiscordTranscriptCapture.Capture(Message(reactions: new Dictionary<IEmote, ReactionMetadata>
            {
                [new Emote(42, "acknowledged <script>alert('x')</script> 🚀", true)] = Reaction(1, 1, 0, false)
            }));
            var ticket = new Ticket(Guid.NewGuid(), "support", 55, 1234, TicketState.Closed, 1, DateTimeOffset.UnixEpoch);
            ticket = ticket with { ArchivePath = archive.GetArchivePath(ticket) };
            var directory = await archive.ExportAsync(ticket, new([retained], retained.Id), default);
            var html = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.html"), Encoding.UTF8);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json"), Encoding.UTF8));
            var retainedMetadata = json.RootElement.GetProperty("Messages")[0].GetProperty("MetadataJson").GetString();
            Assert.That(retainedMetadata, Is.EqualTo(retained.MetadataJson));
            Assert.That(html, Does.Contain(WebUtility.HtmlEncode(retained.MetadataJson)));
            Assert.That(html, Does.Not.Contain("<script>"));
            using var metadata = JsonDocument.Parse(retainedMetadata!);
            Assert.That(metadata.RootElement.GetProperty("Reactions")[0].GetProperty("Emoji").GetProperty("Name").GetString(),
                Is.EqualTo("acknowledged <script>alert('x')</script> 🚀"));
        }
        finally
        {
            if (Directory.Exists(archiveRoot)) Directory.Delete(archiveRoot, recursive: true);
        }
    }

    [TestCase(MessageType.Reply, MessageFlags.None, MessageReferenceType.Default)]
    [TestCase(MessageType.Default, MessageFlags.IsCrosspost, MessageReferenceType.Default)]
    [TestCase(MessageType.Default, MessageFlags.None, MessageReferenceType.Forward)]
    public void ReplyCrosspostAndForwardReferencesKeepTargetIdsAndReferenceType(
        MessageType messageType, MessageFlags flags, MessageReferenceType referenceType)
    {
        var reference = new MessageReference(100, 200, 300, referenceType: referenceType);
        var retained = DiscordTranscriptCapture.Capture(Message("Evidence", reference: reference, type: messageType, flags: flags));
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var captured = metadata.RootElement.GetProperty("Reference");
        Assert.Multiple(() =>
        {
            Assert.That(captured.GetProperty("MessageId").GetUInt64(), Is.EqualTo(100));
            Assert.That(captured.GetProperty("ChannelId").GetUInt64(), Is.EqualTo(200));
            Assert.That(captured.GetProperty("GuildId").GetUInt64(), Is.EqualTo(300));
            Assert.That(captured.GetProperty("ReferenceType").GetInt32(), Is.EqualTo((int)referenceType));
            Assert.That(captured.EnumerateObject().Select(property => property.Name),
                Is.EquivalentTo(new[] { "MessageId", "ChannelId", "GuildId", "ReferenceType" }),
                "Retain scalar reference data without SDK optional wrappers or related message/client graphs.");
        });
    }

    [TestCase(false, true, true)]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, false, false)]
    public void MessageReferenceMetadataAcceptsMissingIds(bool hasMessageId, bool hasChannelId, bool hasGuildId)
    {
        var reference = new MessageReference(hasMessageId ? 100UL : null, hasChannelId ? 200UL : null, hasGuildId ? 300UL : null);
        var retained = DiscordTranscriptCapture.Capture(Message(reference: reference));
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var captured = metadata.RootElement.GetProperty("Reference");
        Assert.That(captured.GetProperty("MessageId").ValueKind, Is.EqualTo(hasMessageId ? JsonValueKind.Number : JsonValueKind.Null));
        Assert.That(captured.GetProperty("ChannelId").ValueKind, Is.EqualTo(hasChannelId ? JsonValueKind.Number : JsonValueKind.Null));
        Assert.That(captured.GetProperty("GuildId").ValueKind, Is.EqualTo(hasGuildId ? JsonValueKind.Number : JsonValueKind.Null));
        Assert.That(captured.GetProperty("ReferenceType").GetInt32(), Is.EqualTo((int)MessageReferenceType.Default));
    }

    [Test]
    public void MissingReferenceAndMissingReferenceTypeAreHandledWithoutAccessingUnspecifiedValues()
    {
        using var noReference = JsonDocument.Parse(DiscordTranscriptCapture.Capture(Message()).MetadataJson!);
        Assert.That(noReference.RootElement.GetProperty("Reference").ValueKind, Is.EqualTo(JsonValueKind.Null));
        var reference = new MessageReference(100, 200);
        var explicitDefault = DiscordTranscriptCapture.Capture(Message(reference: reference));
        typeof(MessageReference).GetProperty(nameof(MessageReference.ReferenceType))!
            .SetValue(reference, Optional<MessageReferenceType>.Unspecified);
        Assert.That(DiscordTranscriptCapture.Capture(Message(reference: reference)).MetadataJson,
            Is.EqualTo(explicitDefault.MetadataJson), "Discord defines an omitted reference type as Default.");
    }

    [TestCase("message-id")]
    [TestCase("channel-id")]
    [TestCase("guild-id")]
    [TestCase("type")]
    [TestCase("message-id-removed")]
    [TestCase("guild-id-removed")]
    [TestCase("added")]
    [TestCase("removed")]
    public void ReferenceOnlyChangesAlterTranscriptMetadata(string change)
    {
        var reference = change == "added" ? null : new MessageReference(100, 200, 300);
        var message = Message("Reply evidence", reference: reference, type: MessageType.Reply);
        var original = DiscordTranscriptCapture.Capture(message);
        MessageReference? changed = change switch
        {
            "message-id" => new(101, 200, 300),
            "channel-id" => new(100, 201, 300),
            "guild-id" => new(100, 200, 301),
            "type" => new(100, 200, 300, referenceType: MessageReferenceType.Forward),
            "message-id-removed" => new(null, 200, 300),
            "guild-id-removed" => new(100, 200),
            "removed" => null,
            _ => new(100, 200, 300)
        };
        ((TranscriptPayloadProxy)(object)message).Properties["Reference"] = changed;
        var current = DiscordTranscriptCapture.Capture(message);
        Assert.That(current.MetadataJson, Is.Not.EqualTo(original.MetadataJson));
        Assert.That(current.Content, Is.EqualTo(original.Content));
        Assert.That(current.Timestamp, Is.EqualTo(original.Timestamp));
    }

    [Test]
    public void ForwardedMessageKeepsItsSourceAndAvailableSnapshotReferences()
    {
        var forwarded = Message("Forwarded reply", reference: new MessageReference(100, 200, 300), type: MessageType.Reply);
        var message = Message(reference: new MessageReference(901, 902, 903, referenceType: MessageReferenceType.Forward),
            forwarded: [Sdk<MessageSnapshot>(forwarded)]);
        var retained = DiscordTranscriptCapture.Capture(message);
        using var metadata = JsonDocument.Parse(retained.MetadataJson!);
        var source = metadata.RootElement.GetProperty("Reference");
        var nested = metadata.RootElement.GetProperty("ForwardedMessages")[0].GetProperty("Metadata").GetProperty("Reference");
        Assert.That(source.GetProperty("MessageId").GetUInt64(), Is.EqualTo(901));
        Assert.That(source.GetProperty("ReferenceType").GetInt32(), Is.EqualTo((int)MessageReferenceType.Forward));
        Assert.That(nested.GetProperty("MessageId").GetUInt64(), Is.EqualTo(100));
        Assert.That(nested.GetProperty("ReferenceType").GetInt32(), Is.EqualTo((int)MessageReferenceType.Default));
        ((TranscriptPayloadProxy)(object)forwarded).Properties["Reference"] = new MessageReference(101, 200, 300);
        Assert.That(DiscordTranscriptCapture.Capture(message).MetadataJson, Is.Not.EqualTo(retained.MetadataJson));
    }

    [Test]
    public void ReferenceMetadataUsesTheExistingCumulativeTranscriptBudget()
    {
        var reference = new MessageReference(100, 200, 300);
        var retained = DiscordTranscriptCapture.Capture(Message(reference: reference));
        var budget = new TranscriptContentBudget(1024);
        Assert.DoesNotThrow(() => budget.Add(retained));
        Assert.Throws<InvalidDataException>(() => budget.Add(retained with { Id = 11 }));

        var content = new string('x', 300);
        Assert.DoesNotThrow(() => new TranscriptContentBudget(1024).Add(DiscordTranscriptCapture.Capture(Message(content))));
        var largeIds = new MessageReference(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        Assert.Throws<InvalidDataException>(() => new TranscriptContentBudget(1024)
            .Add(DiscordTranscriptCapture.Capture(Message(content, reference: largeIds))));
    }

    [Test]
    public async Task CapturedReferenceMetadataIsPreservedInArchiveJsonAndEncodedInHtml()
    {
        var archiveRoot = Path.Combine(Path.GetTempPath(), "swlor-discord-reference-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient();
            var archive = new FileTranscriptArchive(new BotConfiguration
            {
                Tickets = new TicketOptions { ArchiveDirectory = archiveRoot, CopyAttachments = false }
            }, client);
            var reference = new MessageReference(ulong.MaxValue, 200, 300);
            var retained = DiscordTranscriptCapture.Capture(Message("<script>reply 🚀</script>", reference: reference, type: MessageType.Reply));
            var ticket = new Ticket(Guid.NewGuid(), "support", 55, 1234, TicketState.Closed, 1, DateTimeOffset.UnixEpoch);
            ticket = ticket with { ArchivePath = archive.GetArchivePath(ticket) };
            var directory = await archive.ExportAsync(ticket, new([retained], retained.Id), default);
            var html = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.html"), Encoding.UTF8);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json"), Encoding.UTF8));
            var retainedMetadata = json.RootElement.GetProperty("Messages")[0].GetProperty("MetadataJson").GetString();
            Assert.That(retainedMetadata, Is.EqualTo(retained.MetadataJson));
            Assert.That(html, Does.Contain(WebUtility.HtmlEncode(retained.MetadataJson)));
            Assert.That(html, Does.Contain(WebUtility.HtmlEncode(retained.Content)));
            Assert.That(html, Does.Not.Contain("<script>"));
            using var metadata = JsonDocument.Parse(retainedMetadata!);
            Assert.That(metadata.RootElement.GetProperty("Reference").GetProperty("MessageId").GetUInt64(), Is.EqualTo(ulong.MaxValue));
        }
        finally
        {
            if (Directory.Exists(archiveRoot)) Directory.Delete(archiveRoot, recursive: true);
        }
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
        IReadOnlyCollection<MessageSnapshot>? forwarded = null, IReadOnlyCollection<IAttachment>? attachments = null,
        IReadOnlyDictionary<IEmote, ReactionMetadata>? reactions = null, MessageReference? reference = null,
        MessageType type = MessageType.Default, MessageFlags flags = MessageFlags.None) =>
        Proxy<IUserMessage>(new Dictionary<string, object?>
        {
            ["Id"] = 10UL, ["Author"] = Proxy<IUser>(new Dictionary<string, object?> { ["Id"] = 55UL, ["Username"] = "member" }),
            ["Content"] = content, ["Timestamp"] = DateTimeOffset.UnixEpoch, ["Attachments"] = attachments ?? [],
            ["Embeds"] = Array.Empty<IEmbed>(), ["Components"] = components ?? [], ["Stickers"] = stickers ?? [],
            ["ForwardedMessages"] = forwarded ?? [], ["Poll"] = poll, ["Type"] = type,
            ["Reactions"] = reactions ?? new Dictionary<IEmote, ReactionMetadata>(),
            ["Reference"] = reference, ["Flags"] = (MessageFlags?)flags, ["EditedTimestamp"] = (DateTimeOffset?)null
        });

    private static ReactionMetadata Reaction(int count, int normal, int burst, bool isMe, IReadOnlyCollection<Color>? colors = null)
    {
        // The pinned SDK exposes reaction metadata as a struct with internal setters.
        object metadata = new ReactionMetadata();
        typeof(ReactionMetadata).GetProperty(nameof(ReactionMetadata.ReactionCount))!.SetValue(metadata, count);
        typeof(ReactionMetadata).GetProperty(nameof(ReactionMetadata.NormalCount))!.SetValue(metadata, normal);
        typeof(ReactionMetadata).GetProperty(nameof(ReactionMetadata.BurstCount))!.SetValue(metadata, burst);
        typeof(ReactionMetadata).GetProperty(nameof(ReactionMetadata.IsMe))!.SetValue(metadata, isMe);
        typeof(ReactionMetadata).GetProperty(nameof(ReactionMetadata.BurstColors))!.SetValue(metadata, colors);
        return (ReactionMetadata)metadata;
    }

    private static T Sdk<T>(params object?[] arguments) => (T)Activator.CreateInstance(typeof(T),
        BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null)!;

    private static T Proxy<T>(Dictionary<string, object?> properties) where T : class
    {
        var instance = DispatchProxy.Create<T, TranscriptPayloadProxy>();
        ((TranscriptPayloadProxy)(object)instance).Properties = properties;
        return instance;
    }
}
