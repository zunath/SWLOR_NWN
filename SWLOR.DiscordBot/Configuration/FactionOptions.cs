namespace SWLOR.DiscordBot.Configuration;

public sealed class FactionOptions
{
    public bool Enabled { get; set; }
    public FactionRole[] Roles { get; set; } = [];
    public bool? Exclusive { get; set; }
    public string? Behavior { get; set; }
    public bool DeleteCommand { get; set; }
    public bool DeleteResponse { get; set; }
}
