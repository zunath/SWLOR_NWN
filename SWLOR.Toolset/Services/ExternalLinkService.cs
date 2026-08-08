using System.Diagnostics;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>Opens trusted web links and source files with the user's associated applications.</summary>
    public interface IExternalLinkService
    {
        void Open(string url);

        void OpenFile(string path)
        {
        }
    }

    /// <summary>
    /// Hands a validated URL or C# source path to the shell so the OS picks its application.
    /// </summary>
    /// <remarks>
    /// Web input is restricted to http/https. Source input is normalized, must already exist, and
    /// must be a C# file; arbitrary executables and <c>file:</c> URIs are refused. Failures are
    /// logged rather than thrown so a missing browser or editor association cannot take down the
    /// toolset.
    /// </remarks>
    public sealed class ExternalLinkService : IExternalLinkService
    {
        private readonly OutputLogService _log;

        public ExternalLinkService(OutputLogService log) => _log = log;

        public void Open(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _log.AppendLine($"Refused to open a non-web link: {url}");
                return;
            }

            try
            {
                // UseShellExecute is what hands the absolute URI to the OS so it picks the default
                // browser; without it .NET treats the string as an executable path and fails.
                var started = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });

                // A null process is not an error here — the shell often hands the URL to an already
                // running browser and returns nothing — but if it happens and no window appears, this
                // line is the only trace of it.
                if (started == null)
                    _log.AppendLine($"Handed {uri.AbsoluteUri} to the shell; no new process was started.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not open {uri.AbsoluteUri}: {ex.Message}");
            }
        }

        public void OpenFile(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath) ||
                !string.Equals(Path.GetExtension(fullPath), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                _log.AppendLine("The source definition is not available in this workspace.");
                return;
            }

            try
            {
                var started = Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                if (started == null)
                    _log.AppendLine("Handed the source definition to the shell; no new process was started.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not open the source definition: {ex.Message}");
            }
        }
    }
}
