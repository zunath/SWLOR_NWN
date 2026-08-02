using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// The app-wide NWScript language service: one parsed engine header shared by every open script
    /// tab, plus the module's own script list for include completion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registered as a singleton and built lazily on first use. The header is 13,870 lines and
    /// parsing it is the one costly step, so doing it per tab would be wasteful and doing it at
    /// startup would delay a window that may never open a script.
    /// </para>
    /// <para>
    /// Everything here degrades rather than fails. If the header cannot be found the database is
    /// empty, and the editor still opens, highlights and saves - it simply offers no completion.
    /// That matches how the rest of the toolset treats optional game data.
    /// </para>
    /// </remarks>
    public sealed class ScriptLanguageService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly Lazy<EngineSymbolDatabase> _engine;

        public ScriptLanguageService(WorkspaceContext workspaceContext, OutputLogService log)
        {
            _workspaceContext = workspaceContext;
            _log = log;
            _engine = new Lazy<EngineSymbolDatabase>(BuildDatabase, isThreadSafe: true);
        }

        public EngineSymbolDatabase Engine => _engine.Value;

        /// <summary>A completion engine primed with the module's current script list.</summary>
        public ScriptCompletionEngine CreateCompletionEngine() =>
            new(Engine) { AvailableIncludes = ModuleScriptResRefs() };

        public ScriptSignatureHelpEngine CreateSignatureHelpEngine() => new(Engine);

        public bool IsEngineFunction(string name) => Engine.FindFunction(name) != null;

        public bool IsEngineConstant(string name) => Engine.FindConstant(name) != null;

        /// <summary>
        /// Reads a module script's source, or null when it has none. Go-to-definition follows
        /// includes through this; a missing include resolves to nothing rather than throwing,
        /// because plenty of legacy scripts include headers that only exist in the base game.
        /// </summary>
        public string? ReadScriptSource(string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var path = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(path))
                return null;

            try
            {
                return Domain.Script.ScriptTextDocument.Load(path).Text;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private IReadOnlyList<string> ModuleScriptResRefs()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<string>();

            try
            {
                return workspace.EnumerateResRefs(ResourceType.Nss).OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not list module scripts: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private EngineSymbolDatabase BuildDatabase()
        {
            var root = RepositoryRoot();
            if (root == null)
            {
                _log.AppendLine("NWScript header not found; script completion is unavailable.");
                return EngineSymbolDatabase.Empty;
            }

            var header = Directory
                .EnumerateFiles(Path.Combine(root, "SWLOR.NWN.API", "NWN"), "nwscript*.nss")
                .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (header == null)
            {
                _log.AppendLine("NWScript header not found; script completion is unavailable.");
                return EngineSymbolDatabase.Empty;
            }

            try
            {
                var api = Path.Combine(root, "SWLOR.NWN.API", "NWScript");
                var db = EngineSymbolDatabase.Load(header, Directory.Exists(api) ? api : null);
                _log.AppendLine(
                    $"Loaded {db.Functions.Count} engine functions and {db.Constants.Count} constants from {Path.GetFileName(header)}.");
                return db;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not parse the NWScript header: {ex.Message}");
                return EngineSymbolDatabase.Empty;
            }
        }

        /// <summary>
        /// Walks up from the module directory to the repository root. The header lives in a sibling
        /// project, so it is found relative to the open module rather than to the running exe - the
        /// toolset can be launched from anywhere.
        /// </summary>
        private string? RepositoryRoot()
        {
            var start = _workspaceContext.Workspace?.ModuleRoot ?? AppContext.BaseDirectory;
            var current = new DirectoryInfo(start);

            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "SWLOR.NWN.API", "NWN")))
                    return current.FullName;

                current = current.Parent;
            }

            return null;
        }
    }
}
