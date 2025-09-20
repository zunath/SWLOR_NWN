using Serilog;
using Serilog.Core;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Shared.Core.Log
{
    public class LogService : ILogger
    {
        private static readonly Dictionary<ILogGroup, Logger> _loggers = new();
        private readonly Dictionary<Type, ILogGroup> _logGroups = new()
        {
            { typeof(AttackLogGroup), new AttackLogGroup() },
            { typeof(ChatLogGroup), new ChatLogGroup() },
            { typeof(ConnectionLogGroup), new ConnectionLogGroup() },
            { typeof(CraftingLogGroup), new CraftingLogGroup() },
            { typeof(DeathLogGroup), new DeathLogGroup() },
            { typeof(DMAuthorizationLogGroup), new DMAuthorizationLogGroup() },
            { typeof(DMLogGroup), new DMLogGroup() },
            { typeof(ErrorLogGroup), new ErrorLogGroup() },
            { typeof(IncubationLogGroup), new IncubationLogGroup() },
            { typeof(MigrationLogGroup), new MigrationLogGroup() },
            { typeof(PerkRefundLogGroup), new PerkRefundLogGroup() },
            { typeof(PlayerMarketLogGroup), new PlayerMarketLogGroup() },
            { typeof(PropertyLogGroup), new PropertyLogGroup() },
            { typeof(ServerLogGroup), new ServerLogGroup() },
            { typeof(SpaceLogGroup), new SpaceLogGroup() },
            { typeof(StoreCleanupLogGroup), new StoreCleanupLogGroup() },
        };

        private readonly IAppSettings _appSettings;

        public LogService(IAppSettings appSettings)
        {
            _appSettings = appSettings;
            CreateLoggers();
        }

        private void CreateLoggers()
        {
            foreach (var (_, group) in _logGroups)
            {
                var path = _appSettings.LogDirectory + group.Name + "/" + group.Name + "_.log";
                var logger = new LoggerConfiguration()
                    .WriteTo.File(path, rollingInterval: RollingInterval.Day);

                if (group.AlwaysPrintToConsole)
                {
                    logger.WriteTo.Console();
                }

                _loggers[group] = logger.CreateLogger();
            }
        }

        /// <summary>
        /// Audits are written asynchronously so it's important to flush everything to disk when the server stops.
        /// Ensure this is called one time when the server stops.
        /// </summary>
        public void OnApplicationShutdown()
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Dispose();
            }
        }

        /// <summary>
        /// Writes a log message to the audit log for a given log group.
        /// </summary>
        /// <param name="details">The details about the entry which will be written to disk.</param>
        /// <param name="printToConsole">If true, the details will be printed to the console.</param>
        public void Write<T>(string details, bool printToConsole = false)
            where T: ILogGroup
        {
            var group = _logGroups[typeof(T)];

            // If the log group isn't configured for this environment, skip it.
            if (group.EnvironmentType != ServerEnvironmentType.All &&
                !group.EnvironmentType.HasFlag(_appSettings.ServerEnvironment))
            {
                return;
            }

            if (group.AlwaysPrintToConsole || printToConsole)
            {
                Console.WriteLine(details);
            }

            _loggers[group].Information(details);
        }

        public void WriteError(string details)
        {
            var group = _logGroups[typeof(ErrorLogGroup)];

            if (group.AlwaysPrintToConsole)
            {
                Console.WriteLine(details);
            }

            _loggers[group].Error(details);
        }
    }
}
