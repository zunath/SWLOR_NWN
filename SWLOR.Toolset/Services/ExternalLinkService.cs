using System.Diagnostics;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>Opens a URL in the user's default browser.</summary>
    public interface IExternalLinkService
    {
        void Open(string url);
    }

    /// <summary>
    /// Hands a URL to the shell so the OS picks the browser.
    /// </summary>
    /// <remarks>
    /// Only http/https are ever launched. <c>UseShellExecute</c> will happily start a local
    /// executable or a <c>file:</c> path, so an unvalidated string reaching here would be a way to
    /// run something from data — the scheme check keeps this to opening web pages, which is all it is
    /// for. Failures are logged rather than thrown: a missing browser association should not take
    /// down the editor.
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
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not open {uri.AbsoluteUri}: {ex.Message}");
            }
        }
    }
}
