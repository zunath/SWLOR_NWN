using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Workspace
{
    public sealed record ModuleCustomContentSnapshot(
        NwnIniProfile Profile,
        IReadOnlyList<string> AvailableHaks,
        IReadOnlyList<string> AvailableTlks,
        IReadOnlyList<string> AvailableMovies);

    public sealed record ModuleCustomContentReloadResult(
        int AssignedHakCount,
        int LoadedHakCount,
        IReadOnlyList<string> MissingHaks,
        string? CustomTlk,
        bool ResourceIndexAvailable,
        bool RetainedHakLayers = false);

    /// <summary>
    /// Resolves the open module's authoritative HAK/TLK assignments through nwn.ini and hot-swaps
    /// the shared game-data services. The repository HAK-builder folders remain startup fallback
    /// only; once this service can resolve the user's aliases, packed archives drive the toolset.
    /// </summary>
    public sealed class ModuleCustomContentService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly ResourceIndex? _resourceIndex;
        private readonly TlkService? _tlkService;
        private readonly string? _iniPathOverride;
        private readonly string? _nwnInstallPath;
        private readonly SemaphoreSlim _reloadGate = new(1, 1);

        public event Action<ModuleCustomContentReloadResult>? Reloaded;

        public ResourceIndex? ResourceIndex => _resourceIndex;

        public ModuleCustomContentService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ResourceIndex? resourceIndex = null,
            TlkService? tlkService = null,
            string? iniPathOverride = null,
            ToolsetSettings? settings = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _resourceIndex = resourceIndex;
            _tlkService = tlkService;
            _iniPathOverride = iniPathOverride;
            _nwnInstallPath = NwnInstallLocator.Locate(settings?.NwnInstallOverride);
            _workspaceContext.WorkspaceOpened += ReloadSavedModuleInBackground;
        }

        public ModuleCustomContentSnapshot Discover()
        {
            var profile = NwnIniProfile.Load(_iniPathOverride);
            return new ModuleCustomContentSnapshot(
                profile,
                profile.EnumerateHakNames(),
                profile.EnumerateTlkNames(),
                profile.EnumerateMovieNames(_nwnInstallPath));
        }

        public async Task<ModuleCustomContentReloadResult> ReloadAsync(
            IReadOnlyList<string> hakNames,
            string? customTlk,
            CancellationToken cancellationToken = default,
            bool rescanUnchangedHaks = true,
            bool retainCurrentHaksWhenMissing = false)
        {
            ArgumentNullException.ThrowIfNull(hakNames);
            await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Reloading a known saved list needs path resolution only. Discover() also enumerates every
                // available HAK, TLK, and movie for the module-properties pickers, which is unrelated work
                // on the startup path.
                var profile = NwnIniProfile.Load(_iniPathOverride);
                var hakResolution = profile.ResolveHakLayers(hakNames);
                var layers = hakResolution.Layers;
                var missing = hakResolution.MissingHakNames;

                var normalizedTlk = string.IsNullOrWhiteSpace(customTlk) ? null : customTlk.Trim();
                var tlkPath = normalizedTlk == null ? null : profile.FindTlkPath(normalizedTlk);
                if (normalizedTlk != null && tlkPath == null)
                    throw new FileNotFoundException(
                        $"The custom TLK '{normalizedTlk}.tlk' was not found in the nwn.ini TLK directory.");

                var retainHakLayers =
                    retainCurrentHaksWhenMissing && missing.Count > 0;
                if (_resourceIndex != null && !retainHakLayers)
                    await _resourceIndex.ReloadHakLayersAsync(
                            layers,
                            cancellationToken,
                            rescanUnchangedHaks)
                        .ConfigureAwait(false);

                _tlkService?.ReloadCustomTlk(tlkPath);

                var result = new ModuleCustomContentReloadResult(
                    hakNames.Count,
                    layers.Count,
                    missing,
                    normalizedTlk,
                    _resourceIndex != null,
                    retainHakLayers);
                Reloaded?.Invoke(result);
                return result;
            }
            finally
            {
                _reloadGate.Release();
            }
        }

        private void ReloadSavedModuleInBackground()
        {
            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null)
                return;

            var ifoPath = Path.Combine(moduleRoot, "ifo", "module.ifo.json");
            if (!File.Exists(ifoPath))
                return;

            try
            {
                var ifo = IfoDocument.Load(ifoPath);
                _ = ReloadSavedAsync(ifo.HakNames, ifo.CustomTlk);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read module custom content: {ex.Message}");
            }
        }

        private async Task ReloadSavedAsync(IReadOnlyList<string> hakNames, string? customTlk)
        {
            try
            {
                // App startup may already have indexed the saved module.ifo stack. Confirm that
                // identical list without paying for another archive scan; explicit reloads keep
                // the public default above and rescan even when the paths have not changed.
                var result = await ReloadAsync(
                        hakNames,
                        customTlk,
                        rescanUnchangedHaks: false,
                        retainCurrentHaksWhenMissing: true)
                    .ConfigureAwait(false);
                _log.AppendLine(result.RetainedHakLayers
                    ? "Module custom content retained the repository HAK fallback because the saved packed stack is incomplete."
                    : $"Module custom content loaded: {result.LoadedHakCount}/{result.AssignedHakCount} HAKs" +
                      (result.CustomTlk == null ? "." : $", {result.CustomTlk}.tlk."));
                if (result.MissingHaks.Count > 0)
                    _log.AppendLine("Missing assigned HAKs: " + string.Join(", ", result.MissingHaks));
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Module custom-content reload failed: {ex.GetBaseException().Message}");
            }
        }
    }
}
