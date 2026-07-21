using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Opens blueprint editors as document tabs. One editor per file: requesting an already
    /// open blueprint activates its existing tab. Types without a schema yet log a notice
    /// instead of opening (schemas beyond UTC arrive with the next work package).
    /// </summary>
    public sealed class EditorService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly LookupOptionProvider _lookups;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly OutputLogService _log;
        private readonly ToolsetDockFactory _factory;
        private readonly ModelPreviewViewModel? _modelPreview;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly TileModelCache? _tileModelCache;
        private readonly ResourceIndex? _resourceIndex;
        private readonly Dictionary<string, BlueprintEditorViewModel> _openEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AreaEditorViewModel> _openAreaEditors = new(StringComparer.OrdinalIgnoreCase);

        public EditorService(
            WorkspaceContext workspaceContext,
            LookupOptionProvider lookups,
            OutputLogService log,
            ToolsetDockFactory factory,
            IGameCodeIndex? gameCodeIndex = null,
            ModelPreviewViewModel? modelPreview = null,
            TilesetCatalog? tilesetCatalog = null,
            TileModelCache? tileModelCache = null,
            ResourceIndex? resourceIndex = null)
        {
            _workspaceContext = workspaceContext;
            _lookups = lookups;
            _log = log;
            _factory = factory;
            _gameCodeIndex = gameCodeIndex;
            _modelPreview = modelPreview;
            _tilesetCatalog = tilesetCatalog;
            _tileModelCache = tileModelCache;
            _resourceIndex = resourceIndex;
        }

        public void TryOpenEditor(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            if (type == ResourceType.Area)
            {
                OpenAreaEditor(workspace, resRef);
                return;
            }

            var schema = GetSchema(type);
            if (schema == null)
            {
                _log.AppendLine($"No editor available yet for {type} blueprints.");
                return;
            }

            var filePath = workspace.GetResourcePath(type, resRef);
            if (!File.Exists(filePath))
            {
                _log.AppendLine($"File not found: {filePath}");
                return;
            }

            if (_openEditors.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                PreviewEditorModel(existing);
                return;
            }

            try
            {
                var editor = new BlueprintEditorViewModel(
                    filePath, resRef, type, schema, _lookups, _gameCodeIndex, _log);
                editor.Closed += _ => _openEditors.Remove(filePath);
                editor.DocumentChanged += () => PreviewEditorModel(editor);
                _openEditors[filePath] = editor;
                _factory.OpenDocument(editor);
                PreviewEditorModel(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open editor for {resRef}: {ex.Message}");
            }
        }

        /// <summary>Saves every open editor (blueprint and area) that has unsaved changes.</summary>
        public void SaveAll()
        {
            foreach (var editor in _openEditors.Values.ToList())
                editor.SaveCommand.Execute(null);

            foreach (var editor in _openAreaEditors.Values.ToList())
                editor.SaveCommand.Execute(null);
        }

        /// <summary>Areas open in the composite editor (.are properties + .git instance lists).</summary>
        private void OpenAreaEditor(Domain.Workspace.ModuleWorkspace workspace, string resRef)
        {
            if (_openAreaEditors.TryGetValue(resRef, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            var arePath = workspace.GetResourcePath(ResourceType.Area, resRef);
            var gitPath = Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json");
            if (!File.Exists(arePath) || !File.Exists(gitPath))
            {
                _log.AppendLine($"Area files not found for '{resRef}' (.are/.git pair required).");
                return;
            }

            try
            {
                var editor = new AreaEditorViewModel(
                    resRef, workspace, _lookups, _gameCodeIndex, _log,
                    _tilesetCatalog, _tileModelCache, _resourceIndex);
                editor.Closed += _ => _openAreaEditors.Remove(resRef);
                _openAreaEditors[resRef] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open area editor for {resRef}: {ex.Message}");
            }
        }

        /// <summary>Points the Model Preview panel at an editor's live document (creatures/placeables/doors).</summary>
        private void PreviewEditorModel(BlueprintEditorViewModel editor)
        {
            _modelPreview?.ShowForDocument(editor.BlueprintType, editor.DocumentRoot, editor.Title);
        }

        private static EditorSchema? GetSchema(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utc => UtcSchema.Build(),
                ResourceType.Uti => UtiSchema.Build(),
                ResourceType.Utp => UtpSchema.Build(),
                ResourceType.Utd => UtdSchema.Build(),
                ResourceType.Utw => UtwSchema.Build(),
                ResourceType.Uts => UtsSchema.Build(),
                ResourceType.Utt => UttSchema.Build(),
                ResourceType.Utm => UtmSchema.Build(),
                _ => null
            };
        }
    }
}
