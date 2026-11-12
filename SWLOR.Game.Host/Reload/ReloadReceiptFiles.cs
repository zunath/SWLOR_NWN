using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.Game.Host.Reload;

/// <summary>
/// Reads and writes receipt files. A receipt is written to a temporary file beside it and moved over it, so a reader
/// polling the file sees the old receipt or the new one, never a partial one.
/// </summary>
public static class ReloadReceiptFiles
{
    /// <summary>The longest detail a receipt keeps; the server log has the rest.</summary>
    public const int MaximumDetailLength = 2048;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter<ReloadReceiptStatus>(allowIntegerValues: false) }
    };

    /// <summary>Writes <paramref name="receipt"/> to <paramref name="path"/>, creating its folder.</summary>
    public static void Write(string path, ReloadReceipt receipt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(receipt);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporary, JsonSerializer.SerializeToUtf8Bytes(receipt, Options));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    /// <summary>
    /// Reads the receipt at <paramref name="path"/>, or null when there is none yet. A file that is not a receipt of this
    /// format throws <see cref="InvalidDataException"/>.
    /// </summary>
    public static ReloadReceipt? Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            return null;

        ReloadReceipt? receipt;
        try
        {
            receipt = JsonSerializer.Deserialize<ReloadReceipt>(File.ReadAllBytes(path), Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{path} is not a reload receipt: {exception.Message}", exception);
        }

        if (receipt is null || receipt.FormatVersion != ReloadReceipt.CurrentFormatVersion || receipt.GenerationId == Guid.Empty ||
            !Enum.IsDefined(receipt.Status) || receipt.Detail is null)
            throw new InvalidDataException($"{path} is not a reload receipt of format {ReloadReceipt.CurrentFormatVersion}.");

        return receipt;
    }

    /// <summary>Shortens <paramref name="detail"/> to what a receipt keeps.</summary>
    public static string LimitDetail(string detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return detail.Length <= MaximumDetailLength ? detail : detail[..MaximumDetailLength] + "...";
    }

    /// <summary>One line for the server log and the developer's console: the id, the status, the numbers, then the detail.</summary>
    public static string Describe(ReloadReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var line = new StringBuilder();
        line.Append(CultureInfo.InvariantCulture, $"generation={ReloadLayout.IdText(receipt.GenerationId)} status={receipt.Status}");
        if (receipt.ActiveGeneration is { } active)
            line.Append(CultureInfo.InvariantCulture, $" active={active}");
        if (receipt.ValidationMilliseconds is { } validation)
            line.Append(CultureInfo.InvariantCulture, $" validateMs={validation:F0}");
        if (receipt.PauseMilliseconds is { } pause)
            line.Append(CultureInfo.InvariantCulture, $" pauseMs={pause:F0}");
        if (receipt.PausedFrame is { } frame)
            line.Append(CultureInfo.InvariantCulture, $" frame={frame}");
        line.Append(": ").Append(receipt.Detail);
        return line.ToString();
    }
}
