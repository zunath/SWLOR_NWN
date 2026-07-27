using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Views;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Backs every script slot in the blueprint and area editors: existence checks, the picker, and
    /// opening the script in its own tab.
    /// </summary>
    /// <remarks>
    /// The usage counts shown in the picker come from <see cref="ScriptUsageIndex"/>, built lazily in
    /// the background and invalidated as resources change. That index is what makes "used by 41"
    /// possible, and with 2,250 module
    /// resources naming scripts it is the difference between editing a legacy script confidently and
    /// guessing.
    /// </remarks>
    public sealed class ScriptSlotHost : IScriptSlotHost
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly Func<EditorService> _editorService;
        private readonly OutputLogService _log;
        private readonly string _ownerDescription;
        private readonly Func<Task<ScriptUsageIndex?>> _usageIndex;
        private readonly Func<Task<string?>> _newScriptFactory;

        public ScriptSlotHost(
            WorkspaceContext workspaceContext,
            Func<EditorService> editorService,
            OutputLogService log,
            string ownerDescription,
            Func<Task<ScriptUsageIndex?>> usageIndex,
            Func<Task<string?>> newScriptFactory)
        {
            _workspaceContext = workspaceContext;
            _editorService = editorService;
            _log = log;
            _ownerDescription = ownerDescription;
            _usageIndex = usageIndex;
            _newScriptFactory = newScriptFactory;
        }

        public bool ScriptExists(string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || string.IsNullOrWhiteSpace(resRef))
                return true;

            if (File.Exists(workspace.GetResourcePath(ResourceType.Nss, resRef)))
                return true;

            // A committed .ncs with no source still runs in game - 154 of the module's compiled
            // scripts have no .nss - so the slot is valid even though it cannot be opened.
            return File.Exists(Path.Combine(workspace.ModuleRoot, "ncs", resRef + ".ncs"));
        }

        public void OpenScript(string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            if (!File.Exists(workspace.GetResourcePath(ResourceType.Nss, resRef)))
            {
                _log.AppendLine($"'{resRef}' has no source in this module; only its compiled .ncs exists.");
                return;
            }

            _editorService().TryOpenEditor(ResourceType.Nss, resRef);
        }

        /// <summary>Resrefs with a compiled .ncs and no .nss beside it.</summary>
        private static IEnumerable<string> EnumerateCompiledOnly(
            string moduleRoot, IReadOnlySet<string> withSource)
        {
            var directory = Path.Combine(moduleRoot, "ncs");
            if (!Directory.Exists(directory))
                yield break;

            foreach (var path in Directory.EnumerateFiles(directory, "*.ncs"))
            {
                var resRef = Path.GetFileNameWithoutExtension(path);
                if (resRef.Length > 0 && !withSource.Contains(resRef))
                    yield return resRef;
            }
        }

        public async Task<string?> PickScriptAsync(string current)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var owner = (Avalonia.Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null)
                return null;

            var usage = await _usageIndex().ConfigureAwait(true);
            var counts = usage?.UsageCounts();

            var rows = new List<ScriptPickerRow>();
            var withSource = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Nss))
            {
                withSource.Add(resRef);
                var path = workspace.GetResourcePath(ResourceType.Nss, resRef);
                var isInclude = false;

                try
                {
                    isInclude = !ScriptStalenessScanner.IsEntryPoint(ScriptTextDocument.Load(path).Text);
                }
                catch (IOException)
                {
                    // Unreadable: still listed, just unlabelled.
                }

                rows.Add(new ScriptPickerRow(
                    resRef, resRef, isInclude,
                    counts != null && counts.TryGetValue(resRef, out var n) ? n : 0));
            }

            // The compiled-only scripts. ScriptExists already treats an .ncs without source as a
            // real executable script - the module has many - but this list did not, so a slot
            // naming one was reported as pointing at a script that does not exist, and no other
            // slot could be pointed at it through the browse UI at all. Listed and marked, not
            // hidden: the builder can select it, and only opening it is unavailable.
            foreach (var resRef in EnumerateCompiledOnly(workspace.ModuleRoot, withSource))
            {
                rows.Add(new ScriptPickerRow(
                    resRef, resRef, isInclude: false,
                    counts != null && counts.TryGetValue(resRef, out var n) ? n : 0,
                    hasSource: false));
            }

            var dialog = new ScriptPickerDialog();
            dialog.NewScriptFactory = _newScriptFactory;
            dialog.Configure("Script slot", _ownerDescription, current, rows);
            return await dialog.ShowDialog<string?>(owner).ConfigureAwait(true);
        }
    }
}
