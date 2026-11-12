namespace SWLOR.Game.Contracts;

/// <summary>
/// What the permanent host hands the active game build about the engine callbacks it delivers, besides the scripts and
/// frames the hosting package forwards itself (<c>IScriptDispatcher</c>, <c>IMainLoopSink</c>). The host implements it
/// once for the process and hands it to every generation through <c>IHostServices</c>; everything runs on the engine
/// thread.
/// </summary>
public interface IHostCallbacks
{
    /// <summary>
    /// The frame number NWNX passed to the main-loop callback the host is delivering. A generation's
    /// <c>IMainLoopSink.Tick</c> runs for this frame.
    /// </summary>
    ulong Frame { get; }

    /// <summary>
    /// Makes <paramref name="handler"/> receive every later NWNX signal (except <c>ON_DESTROY_SERVER</c>, which the hosting
    /// package keeps), until <see cref="UnbindSignals"/>. One handler is bound at a time: the active generation's.
    /// </summary>
    void BindSignals(IHostSignalHandler handler);

    /// <summary>Stops delivering signals to <paramref name="handler"/> when it is the bound one.</summary>
    void UnbindSignals(IHostSignalHandler handler);
}
