using System.Globalization;

namespace SWLOR.DiscordBot.Hosting;

public sealed class ReadinessMarker
{
    public static string MarkerPath => Environment.GetEnvironmentVariable("SWLOR_BOT_READY_FILE") ?? "/data/bot.ready";
    public static bool IsHealthy() => IsHealthy(MarkerPath, DateTimeOffset.UtcNow);
    public static bool IsHealthy(string path, DateTimeOffset now)
    {
        try
        {
            var text = File.ReadAllText(path);
            return DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
                && now - time >= TimeSpan.Zero && now - time <= TimeSpan.FromSeconds(90);
        }
        catch { return false; }
    }
    public void Clear()
    {
        try { if (File.Exists(MarkerPath)) File.Delete(MarkerPath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    public async Task RefreshAsync(bool ready, CancellationToken ct)
    {
        if (!ready) { Clear(); return; }
        var path = Path.GetFullPath(MarkerPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + $".{Environment.ProcessId}.tmp";
        await File.WriteAllTextAsync(temporary, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture), ct);
        File.Move(temporary, path, true);
    }
}
