namespace SWLOR.DiscordBot.Configuration;

public sealed class TicketOptions
{
    public bool Enabled { get; set; }
    public TicketPanelOptions[] Panels { get; set; } = [];
    public ulong[] SupportRoleIds { get; set; } = [];
    public ulong[] BypassRoleIds { get; set; } = [];
    public bool? BypassMemberLimit { get; set; }
    public bool? BypassPanelLimit { get; set; }
    public bool? BypassGuildLimit { get; set; }
    public int MemberLimit { get; set; } = 1;
    public int GuildLimit { get; set; } = 100;
    public ulong ClosedCategoryId { get; set; }
    public ulong LogChannelId { get; set; }
    public bool CloseConfirmation { get; set; } = true;
    public bool ClosedRequesterCanRead { get; set; } = true;
    public TimeSpan CleanupDelay { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int ArchiveRetentionDays { get; set; } = 90;
    public string ArchiveDirectory { get; set; } = "/data/archives";
    public bool CopyAttachments { get; set; } = true;
    public long MaxAttachmentBytes { get; set; } = 104857600;
    public long MaxTicketAttachmentBytes { get; set; } = 1073741824;
    public long MaxTranscriptContentBytes { get; set; } = 33554432;
}
