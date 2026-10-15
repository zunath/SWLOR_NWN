namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>The reservation handed out when no module mutation lock is configured: nothing to release.</summary>
    internal sealed class SwlorExplorerNoReservation : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
