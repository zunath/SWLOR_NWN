using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service
{
    public static class Log
    {
        private static readonly Dictionary<LogGroup, LogGroupAttribute> _logGroups = new();
        private static readonly Dictionary<LogGroup, Logger> _loggers = new();

        /// <summary>
        /// Registers all loggers. This should be called in the EntryPoints class.
        /// </summary>
        public static void Register()
        {
            var logGroupTypes = Enum.GetValues(typeof(LogGroup)).Cast<LogGroup>();
            foreach (var logGroupType in logGroupTypes)
            {
                var detail = logGroupType.GetAttribute<LogGroup, LogGroupAttribute>();
                _logGroups[logGroupType] = detail;
            }

            LoadLoggers();
        }

        /// <summary>
        /// When the module loads, initialize all possible loggers.
        /// </summary>
        private static void LoadLoggers()
        {
            var settings = ApplicationSettings.Get();

            foreach (var (type, detail) in _logGroups)
            {
                var path = settings.LogDirectory + detail.LoggerName + "/" + detail.LoggerName + "_.log";
                var logger = new LoggerConfiguration()
                    .WriteTo.File(path, rollingInterval: RollingInterval.Day);

                // Errors should also be print to the console.
                if (type == LogGroup.Error)
                {
                    logger.WriteTo.Console();
                }

                _loggers[type] = logger.CreateLogger();
            }
        }

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
            var settings = ApplicationSettings.Get();
            var logDetail = _logGroups[group];

            // If the log group isn't configured for this environment, skip it.
            if (logDetail.Environment != ServerEnvironmentType.All &&
                !logDetail.Environment.HasFlag(settings.ServerEnvironment))
            {
                return;
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
