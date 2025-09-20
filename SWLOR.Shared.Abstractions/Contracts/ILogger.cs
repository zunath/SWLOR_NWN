using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log
{
    /// <summary>
    /// Interface for the LogService to enable dependency injection and testing.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Audits are written asynchronously so it's important to flush everything to disk when the server stops.
        /// Ensure this is called one time when the server stops.
        /// </summary>
        void OnApplicationShutdown();

        /// <summary>
        /// Writes a log message to the audit log for a given log group.
        /// </summary>
        /// <param name="details">The details about the entry which will be written to disk.</param>
        /// <param name="printToConsole">If true, the details will be printed to the console.</param>
        void Write<T>(string details, bool printToConsole = false)
            where T: ILogGroup;
    }
}
