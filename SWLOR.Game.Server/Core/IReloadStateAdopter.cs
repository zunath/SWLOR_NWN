namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// A service whose <see cref="ReloadStateKind.Carried"/> fields need their timers back once a hot reload has put them back: the
    /// carried data says what to track, but the old build's timers (<c>Scheduler</c> entries, closures) went with it.
    /// <see cref="ReloadStateCarrier"/> calls <see cref="OnCarriedStateAdopted"/> on the new build's singleton right after it reads
    /// the class's fields. A normal boot never calls it.
    /// </summary>
    public interface IReloadStateAdopter
    {
        /// <summary>Starts again whatever the carried state needs running; on the engine thread, inside the reload pause.</summary>
        void OnCarriedStateAdopted();
    }
}
