using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Logging
{
    /// <summary>
    /// Audits should be used to track important information which will keep for an extended period of time.
    /// For example, an audit should be used to track player connections.
    /// </summary>
    public static class Audit
    {
        private static readonly Dictionary<AuditGroup, Logger> _loggers = new Dictionary<AuditGroup, Logger>();

        /// <summary>
        /// Sets up the Audit process and ensures it starts/stops gracefully.
        /// </summary>
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnServerStopped>(x => FlushAllLogs());
        }

        /// <summary>
        /// Audits are written asynchronously so it's important to flush everything to disk when the server stops.
        /// Ensure this is called one time when the server stops.
        /// </summary>
        private static void FlushAllLogs()
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Dispose();
            }
        }

        /// <summary>
        /// Writes an audit message to the audit log for a given audit group.
        /// </summary>
        /// <param name="group">The group to audit this log under.</param>
        /// <param name="details">The details about the entry which will be written to disk.</param>
        public static void Write(AuditGroup group, string details)
        {
            if (!_loggers.ContainsKey(group))
            {
                var settings = ApplicationSettings.Get();

                var path = settings.LogDirectory + group + "/" + group + "_.log";
                var logger = new LoggerConfiguration()
                    .WriteTo.Async(a => a.File(path, rollingInterval: RollingInterval.Day));

                // Errors should also be print to the console.
                if (group == AuditGroup.Error)
                {
                    logger.WriteTo.Console();
                }

                _loggers[group] = logger.CreateLogger();
            }

            _loggers[group].Information(details);
        }
    }
}
