using System.Text.Json.Serialization;

namespace SWLOR.Game.Host.Reload;

/// <summary>
/// The host's answer to one reload attempt, written to the attempt's receipt file (<see cref="ReloadLayout.ReceiptPath"/>)
/// and to the server log: first <see cref="ReloadReceiptStatus.Accepted"/>, then the outcome.
/// </summary>
/// <param name="FormatVersion">The receipt format, <see cref="CurrentFormatVersion"/>.</param>
/// <param name="GenerationId">The candidate the signal named.</param>
/// <param name="Status">Where the attempt stands.</param>
/// <param name="Detail">What happened, in words: the reason for a rejection, the error of a failure.</param>
/// <param name="WrittenUtc">When the host wrote this receipt.</param>
public sealed record ReloadReceipt(
    [property: JsonRequired] int FormatVersion,
    [property: JsonRequired] Guid GenerationId,
    [property: JsonRequired] ReloadReceiptStatus Status,
    [property: JsonRequired] string Detail,
    [property: JsonRequired] DateTimeOffset WrittenUtc)
{
    /// <summary>The receipt format this host writes.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>The host's number of the generation running once the attempt ended, when one runs.</summary>
    public long? ActiveGeneration { get; init; }

    /// <summary>How long the candidate took to load and validate off the engine thread.</summary>
    public double? ValidationMilliseconds { get; init; }

    /// <summary>
    /// How long the engine thread was held inside one frame, from the old build's quiescence to the new build's
    /// activation (or to the failure). No engine frame ran during it.
    /// </summary>
    public double? PauseMilliseconds { get; init; }

    /// <summary>The engine frame the pause held.</summary>
    public ulong? PausedFrame { get; init; }

    /// <summary>True once the attempt has ended: every status but <see cref="ReloadReceiptStatus.Accepted"/>.</summary>
    [JsonIgnore]
    public bool IsFinal => Status != ReloadReceiptStatus.Accepted;
}
