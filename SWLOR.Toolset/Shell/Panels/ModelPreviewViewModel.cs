using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using Radoub.Formats.Mdl;
using Radoub.UI.Services;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Model Preview panel (WP4.1 spike proof): renders the selected creature's model via
    /// Radoub's ModelPreviewGLControl, resolving appearance → model resref through
    /// appearance.2da and loading bytes through the layered ResourceIndex. Simple (MODELTYPE
    /// S/L) models render now; segmented part-based models arrive with WP4.3.
    /// </summary>
    public partial class ModelPreviewViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly AppearanceService? _appearances;
        private readonly ResourceIndex? _resourceIndex;
        private readonly OutputLogService _log;
        private readonly MdlReader _mdlReader = new();

        /// <summary>Shared texture resolver for the GL control; null when no ResourceIndex.</summary>
        public TextureService? TextureService { get; }

        [ObservableProperty]
        private MdlModel? _currentModel;

        [ObservableProperty]
        private string _statusText = "Select a creature to preview its model.";

        public ModelPreviewViewModel(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            AppearanceService? appearances = null,
            ResourceIndex? resourceIndex = null,
            TwoDaService? twoDaService = null,
            Domain.GameData.Tlk.TlkService? tlkService = null)
        {
            _workspaceContext = workspaceContext;
            _log = log;
            _appearances = appearances;
            _resourceIndex = resourceIndex;
            Id = "ModelPreview";
            Title = "Model Preview";

            if (resourceIndex != null)
                TextureService = new TextureService(new SwlorGameDataService(resourceIndex, twoDaService, tlkService));
        }

        /// <summary>Called by the explorer on selection; only creature selections preview.</summary>
        public void ShowFor(ResourceType type, string resRef)
        {
            if (type != ResourceType.Utc)
                return;

            if (_appearances == null || _resourceIndex == null)
            {
                StatusText = "Model preview unavailable (game data services not loaded).";
                return;
            }

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            try
            {
                var utc = (UtcDocument)workspace.LoadBlueprint(ResourceType.Utc, resRef);
                var appearanceId = (int)(utc.Document.Root.GetOrNull("Appearance_Type")?.GetInteger() ?? -1);
                var row = _appearances.GetAll().FirstOrDefault(r => r.Id == appearanceId);
                if (row == null)
                {
                    SetModel(null, $"Unknown appearance id {appearanceId}.");
                    return;
                }

                if (string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase))
                {
                    SetModel(null, $"{row.Label}: segmented (parts) model — preview arrives with WP4.3.");
                    return;
                }

                var modelResRef = row.Race;
                if (string.IsNullOrWhiteSpace(modelResRef))
                {
                    SetModel(null, $"{row.Label}: no model resref in appearance.2da.");
                    return;
                }

                var identity = ResourceIdentity.FromFileName(modelResRef + ".mdl");
                if (!_resourceIndex.TryLookup(identity, out var handle))
                {
                    SetModel(null, $"Model '{modelResRef}.mdl' not found in haks or base game.");
                    return;
                }

                SetModel(_mdlReader.Parse(handle.GetBytes()), $"{row.Label} ({modelResRef}.mdl)");
            }
            catch (Exception ex)
            {
                SetModel(null, $"Preview failed: {ex.Message}");
                _log.AppendLine($"Model preview failed for {resRef}: {ex.Message}");
            }
        }

        private void SetModel(MdlModel? model, string status)
        {
            CurrentModel = model;
            StatusText = status;
        }
    }
}
