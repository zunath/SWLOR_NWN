using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service
{
    public enum LogGroup
    {
        Connection,
        Error,
        Chat,
        DM,
        DMAuthorization,
        Death,
        Server,
        PerkRefund,
        PlayerHousing,
        PlayerMarket,
        AI,
        Space
    }

    public static class Log
    {
        private static readonly Dictionary<LogGroup, Logger> _loggers = new Dictionary<LogGroup, Logger>();

        /// <summary>
        /// Audits are written asynchronously so it's important to flush everything to disk when the server stops.
        /// Ensure this is called one time when the server stops.
        /// </summary>
        [NWNEventHandler("app_shutdown")]
        public static void OnApplicationShutdown()
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Dispose();
            }
        }

        /// <summary>
        /// Writes a log message to the audit log for a given log group.
        /// </summary>
        /// <param name="group">The group to audit this log under.</param>
        /// <param name="details">The details about the entry which will be written to disk.</param>
        /// <param name="printToConsole">If true, the details will be printed to the console.</param>
        public static void Write(LogGroup group, string details, bool printToConsole = false)
        {
            if (!_loggers.ContainsKey(group))
            {
                var settings = ApplicationSettings.Get();

                var path = settings.LogDirectory + group + "/" + group + "_.log";
                var logger = new LoggerConfiguration()
                    .WriteTo.Async(a => a.File(path, rollingInterval: RollingInterval.Day));

                // Errors should also be print to the console.
                if (group == LogGroup.Error)
                {
                    logger.WriteTo.Console();
                }

                _loggers[group] = logger.CreateLogger();
            }

            // Errors already print to console by default but if any other log groups have the printToConsole flag,
            // do a console write line.
            if (group != LogGroup.Error && printToConsole)
            {
                Console.WriteLine(details);
            }

            _loggers[group].Information(details);
        }
    }
}
