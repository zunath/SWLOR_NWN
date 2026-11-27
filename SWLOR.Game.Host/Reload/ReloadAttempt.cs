using Nwn.Hosting.Reload;

namespace SWLOR.Game.Host.Reload;

/// <summary>What one reload through the host mode did.</summary>
/// <param name="Report">The package's report of the reload.</param>
/// <param name="Pause">
/// How long the engine thread was held inside one frame, from the old build's quiescence to the new build's activation
/// (or to the failure); null when the reload never got that far (a rejected candidate).
/// </param>
/// <param name="PausedFrame">The engine frame that was held, with <paramref name="Pause"/>.</param>
public sealed record ReloadAttempt(ReloadReport Report, TimeSpan? Pause, ulong? PausedFrame);
