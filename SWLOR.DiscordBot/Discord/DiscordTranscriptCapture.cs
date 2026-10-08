using System.Text.Json;
using Discord;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Discord;

internal static class DiscordTranscriptCapture
{
    // Poll, PollAnswer, PollResults and MessageSnapshot in the pinned SDK use public fields.
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        IncludeFields = true,
        Converters = { new DiscordPayloadConverter<IMessageComponent>(), new DiscordPayloadConverter<IEmote>() }
    };
    private const int MaximumForwardDepth = 4;

    public static TranscriptMessage Capture(IMessage message) => new(message.Id, message.Author.Id,
        message.Author.Username, message.Content, message.Timestamp,
        CaptureAttachments(message, 0).DistinctBy(attachment => attachment.Id).ToArray(),
        JsonSerializer.Serialize(message.Embeds), JsonSerializer.Serialize(Metadata(message, 0), PayloadOptions));

    private static object Metadata(IMessage message, int depth)
    {
        CheckDepth(depth);
        var userMessage = message as IUserMessage;
        return new
        {
            message.Type,
            message.Flags,
            message.EditedTimestamp,
            Reference = message.Reference is { } reference ? new
            {
                MessageId = reference.MessageId.IsSpecified ? reference.MessageId.Value : (ulong?)null,
                ChannelId = reference.ChannelId != 0 ? reference.ChannelId : (ulong?)null,
                GuildId = reference.GuildId.IsSpecified ? reference.GuildId.Value : (ulong?)null,
                ReferenceType = reference.ReferenceType.GetValueOrDefault(MessageReferenceType.Default)
            } : null,
            message.Components,
            message.Stickers,
            Reactions = message.Reactions.Select(reaction => new
            {
                Emoji = new
                {
                    Id = (reaction.Key as Emote)?.Id,
                    reaction.Key.Name,
                    Animated = (reaction.Key as Emote)?.Animated
                },
                reaction.Value.ReactionCount,
                reaction.Value.IsMe,
                reaction.Value.NormalCount,
                reaction.Value.BurstCount,
                BurstColors = reaction.Value.BurstColors?.Select(color => color.RawValue).Order().ToArray() ?? []
            }).OrderBy(reaction => reaction.Emoji.Id)
                .ThenBy(reaction => reaction.Emoji.Name, StringComparer.Ordinal)
                .ThenBy(reaction => reaction.Emoji.Animated).ToArray(),
            Poll = userMessage?.Poll,
            ForwardedMessages = userMessage?.ForwardedMessages.Select(snapshot => new
            {
                snapshot.Message.Content,
                snapshot.Message.Timestamp,
                Attachments = snapshot.Message.Attachments.Select(attachment => new
                {
                    attachment.Id,
                    attachment.Filename,
                    attachment.Size,
                    Resource = Resource(attachment.Url)
                }).ToArray(),
                snapshot.Message.Embeds,
                Metadata = Metadata(snapshot.Message, depth + 1)
            }).ToArray()
        };
    }

    private static IEnumerable<TranscriptAttachment> CaptureAttachments(IMessage message, int depth)
    {
        CheckDepth(depth);
        foreach (var attachment in message.Attachments)
            yield return new(attachment.Id, attachment.Filename, attachment.Url, attachment.Size);
        if (message is IUserMessage userMessage)
            foreach (var snapshot in userMessage.ForwardedMessages)
                foreach (var attachment in CaptureAttachments(snapshot.Message, depth + 1))
                    yield return attachment;
    }

    private static string Resource(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        ? uri.GetLeftPart(UriPartial.Path) : url;

    private static void CheckDepth(int depth)
    {
        if (depth > MaximumForwardDepth)
            throw new InvalidDataException("Ticket transcript forwarded content exceeds the supported nesting depth; cleanup is suspended.");
    }
}
