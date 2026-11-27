using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// An <see cref="IHookControl"/> that records which hooks the game enables instead of asking NWNX, so a test process can
/// run the game's boot handlers; installed hooks are process state and outlive every generation, as on the server.
/// </summary>
internal sealed class RecordingHookControl : IHookControl
{
    private readonly HashSet<NativeHookId> _enabled = [];

    /// <summary>The hooks enabled so far.</summary>
    public IReadOnlyCollection<NativeHookId> Enabled => _enabled;

    public void Enable(NativeHookId hook) => _enabled.Add(hook);

    public bool IsEnabled(NativeHookId hook) => _enabled.Contains(hook);
}
