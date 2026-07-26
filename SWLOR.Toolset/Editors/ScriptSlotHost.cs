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
            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Nss))
            {
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

                counts?.TryGetValue(resRef, out var count);
                rows.Add(new ScriptPickerRow(
                    resRef, resRef, isInclude,
                    counts != null && counts.TryGetValue(resRef, out var n) ? n : 0));
            }

            var dialog = new ScriptPickerDialog();
            dialog.NewScriptFactory = _newScriptFactory;
            dialog.Configure("Script slot", _ownerDescription, current, rows);
            return await dialog.ShowDialog<string?>(owner).ConfigureAwait(true);
        }
    }
}
