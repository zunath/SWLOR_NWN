namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Ends a crew member's station fire and restores whatever context was active before it.</summary>
    public sealed class SpaceCrewFireScope : IDisposable
    {
        private readonly uint _ship;
        private readonly SpaceCrewFireContext _previous;
        private readonly bool _droidFire;
        private bool _disposed;

        internal SpaceCrewFireScope(uint ship, SpaceCrewFireContext previous, bool droidFire = false)
        {
            _ship = ship;
            _previous = previous;
            _droidFire = droidFire;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            SpaceCrew.RestoreFireContext(_ship, _previous, _droidFire);
        }
    }
}
