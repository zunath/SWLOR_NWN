namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Ends a module's trait context (<see cref="ShipModuleTraitContext.Begin"/>).</summary>
    public sealed class ShipModuleTraitScope : IDisposable
    {
        private bool _disposed;

        internal ShipModuleTraitScope()
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ShipModuleTraitContext.End();
        }
    }
}
