namespace SWLOR.Game.Contracts;

/// <summary>
/// Receives the NWNX signals (<c>ON_MODULE_LOAD_FINISH</c> and the others) the host forwards to the active game build,
/// on the engine thread. Bound through <see cref="IHostCallbacks.BindSignals"/>.
/// </summary>
public interface IHostSignalHandler
{
    /// <summary>Handles one signal. Never throws: a failure is the game's to log.</summary>
    void OnSignal(string signal);
}
