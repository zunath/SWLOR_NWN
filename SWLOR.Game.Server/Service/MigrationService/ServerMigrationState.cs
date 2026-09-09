namespace SWLOR.Game.Server.Service.MigrationService
{
    internal sealed class ServerMigrationState
    {
        private readonly int _initialVersion;
        private int _completedVersion;

        public bool Failed { get; private set; }
        public int CompletedVersion => Failed ? _initialVersion : _completedVersion;

        public ServerMigrationState(int initialVersion)
        {
            _initialVersion = initialVersion;
            _completedVersion = initialVersion;
        }

        public void MarkFailed() => Failed = true;

        public void Run(IServerMigration migration, Action<IServerMigration> execute)
        {
            if (Failed)
                throw new InvalidOperationException("Server migrations have already failed.");

            try
            {
                execute(migration);
                _completedVersion = Math.Max(_completedVersion, migration.Version);
            }
            catch
            {
                Failed = true;
                throw;
            }
        }
    }
}
