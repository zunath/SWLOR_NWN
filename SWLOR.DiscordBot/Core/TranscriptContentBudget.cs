using System.Text;

namespace SWLOR.DiscordBot.Core;

// Conservative metadata allowances also bound retained message/attachment object counts.
internal sealed class TranscriptContentBudget(long limit)
{
    private long used;
    public void Add(TranscriptMessage message)
    {
        Reserve(512);
        AddText(message.AuthorName);
        AddText(message.Content);
        AddText(message.EmbedsJson);
        AddText(message.MetadataJson);
        foreach (var attachment in message.Attachments)
        {
            Reserve(256);
            AddText(attachment.FileName);
            AddText(attachment.Url);
        }
    }
    private void AddText(string? value)
    {
        if (value is not null) Reserve(Encoding.UTF8.GetByteCount(value));
    }
    private void Reserve(long bytes)
    {
        if (limit <= 0 || bytes > limit - used)
            throw new InvalidDataException("Ticket transcript content exceeds the cumulative memory budget; cleanup is suspended.");
        used += bytes;
    }
}
