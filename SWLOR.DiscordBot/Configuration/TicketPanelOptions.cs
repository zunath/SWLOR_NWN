namespace SWLOR.DiscordBot.Configuration;

public sealed class TicketPanelOptions
{
    public string Id { get; set; } = "support";
    public ulong ChannelId { get; set; }
    public ulong[] OpenCategoryIds { get; set; } = [];
    public int OpenLimit { get; set; } = 500;
    public string Label { get; set; } = "Open ticket";
    public string OpeningMessage { get; set; } = "";
    public string PanelMessage { get; set; } = "";
}
