using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using Radoub.Formats.Mdl;
using Radoub.UI.Services;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Model Preview panel: renders the appearance-driven model for the selected/edited creature,
    /// placeable, or door via Radoub's ModelPreviewGLControl. Model resolution runs through the
    /// headless <see cref="BlueprintModelResolver"/>; simple models parse directly, segmented
    /// (MODELTYPE=P) creatures compose from body-part MDLs via Radoub's <see cref="MdlPartComposer"/>.
    /// Driven two ways: explorer selection previews the on-disk blueprint; an open utc/utp/utd editor
    /// previews its live in-memory document and refreshes as the appearance changes (WP4.3).
    /// </summary>
    public partial class ModelPreviewViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly AppearanceService? _appearances;
        private readonly PlaceableAppearanceService? _placeables;
        private readonly DoorTypeService? _doors;
        private readonly ResourceIndex? _resourceIndex;
        private readonly OutputLogService _log;
        private readonly MdlReader _mdlReader = new();
        private readonly MdlPartComposer? _partComposer;

        /// <summary>Shared texture resolver for the GL control; null when no ResourceIndex.</summary>
        public TextureService? TextureService { get; }

        [ObservableProperty]
        private MdlModel? _currentModel;

        [ObservableProperty]
        private string _statusText = "Select a creature, placeable, or door to preview its model.";

        public ModelPreviewViewModel(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            AppearanceService? appearances = null,
            ResourceIndex? resourceIndex = null,
            TwoDaService? twoDaService = null,
            Domain.GameData.Tlk.TlkService? tlkService = null,
            PlaceableAppearanceService? placeables = null,
            DoorTypeService? doors = null)
        {
            _workspaceContext = workspaceContext;
            _log = log;
            _appearances = appearances;
            _placeables = placeables;
            _doors = doors;
            _resourceIndex = resourceIndex;
            Id = "ModelPreview";
            Title = "Model Preview";

            if (resourceIndex != null)
            {
                var gameData = new SwlorGameDataService(resourceIndex, twoDaService, tlkService);
                TextureService = new TextureService(gameData);
                _partComposer = new MdlPartComposer(gameData, LoadComposerModel);
            }
        }

        /// <summary>
        /// Previews an on-disk blueprint chosen in the explorer. Loads the blueprint from disk (so it
        /// reflects the last saved state); an open editor's unsaved edits arrive via
        /// <see cref="ShowForDocument"/> instead.
        /// </summary>
        public void ShowFor(ResourceType type, string resRef)
        {
            if (!IsPreviewable(type))
                return;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            try
            {
                var root = workspace.LoadBlueprint(type, resRef).Document.Root;
                ShowForDocument(type, root, resRef);
            }
            catch (Exception ex)
            {
                SetModel(null, $"Preview failed: {ex.Message}");
                _log.AppendLine($"Model preview failed for {resRef}: {ex.Message}");
            }
        }

        /// <summary>
        /// Previews a live in-memory blueprint document (an open editor's current, possibly unsaved,
        /// state). Called on editor open and after each appearance-affecting edit.
        /// </summary>
        public void ShowForDocument(ResourceType type, JsonGffStruct root, string resRef)
        {
            if (!IsPreviewable(type))
                return;

            if (_resourceIndex == null)
            {
                SetModel(null, "Model preview unavailable (game data services not loaded).");
                return;
            }

            try
            {
                var reference = BlueprintModelResolver.Resolve(
                    type, root, _appearances, _placeables, _doors,
                    LoadItemBlueprintRoot, PartModelExists);
                RenderReference(reference);
            }
            catch (Exception ex)
            {
                SetModel(null, $"Preview failed: {ex.Message}");
                _log.AppendLine($"Model preview failed for {resRef}: {ex.Message}");
            }
        }

        private void RenderReference(BlueprintModelReference reference)
        {
            switch (reference.Kind)
            {
                case BlueprintModelKind.Simple:
                    var model = LoadModel(reference.ModelResRef!, withSupermodelAnims: false);
                    SetModel(
                        model,
                        model == null
                            ? $"Model '{reference.ModelResRef}.mdl' not found in haks or base game."
                            : reference.Status);
                    break;

                case BlueprintModelKind.Segmented:
                    if (_partComposer == null)
                    {
                        SetModel(null, "Segmented preview unavailable (composer not initialized).");
                        return;
                    }

                    var parts = reference.Parts
                        .Select(p => (p.PartType, p.ModelResRef))
                        .ToList();
                    _partOriginalBitmaps.Clear();
                    var composed = _partComposer.Compose(reference.SkeletonResRef!, parts, adjustSeams: true);
                    if (composed != null)
                        RestorePartBitmaps(composed);
                    SetModel(
                        composed,
                        composed == null
                            ? $"{reference.Status}: no body-part models resolved."
                            : reference.Status);
                    break;

                default:
                    SetModel(null, reference.Status);
                    break;
            }
        }

        /// <summary>(part resref, mesh name) → the mesh's authored Bitmap, recorded per Compose run.</summary>
        private readonly Dictionary<(string PartResRef, string MeshName), string> _partOriginalBitmaps =
            new(TupleBitmapKeyComparer.Instance);

        /// <summary>
        /// Model loader for MdlPartComposer. The composer passes withSupermodelAnims=true for the
        /// skeleton and false for body parts. Part models are flattened (node transforms baked
        /// into vertices) because the composer attaches part meshes assuming geometry at the part
        /// origin — several SWLOR hak parts violate that with in-file node offsets. The skeleton
        /// must NEVER be flattened: its node transforms are the bone positions. Each part mesh's
        /// authored Bitmap is recorded so <see cref="RestorePartBitmaps"/> can undo the composer's
        /// resref-derived texture override where the authored texture actually exists.
        /// </summary>
        private MdlModel? LoadComposerModel(string resRef, bool withSupermodelAnims)
        {
            var model = LoadModel(resRef, withSupermodelAnims);
            if (model != null && !withSupermodelAnims)
            {
                MdlGeometryFlattener.FlattenNodeTransforms(model);
                foreach (var mesh in model.GetMeshNodes())
                {
                    if (!string.IsNullOrWhiteSpace(mesh.Bitmap))
                        _partOriginalBitmaps[(resRef, mesh.Name)] = mesh.Bitmap;
                }
            }

            return model;
        }

        /// <summary>
        /// The composer overwrites every attached part mesh's Bitmap with the part resref (its
        /// workaround for stale bitmap fields in BioWare's reused part files). That is correct for
        /// BioWare parts, whose textures are named like the part — but many SWLOR custom parts
        /// reference their real texture by a different name (e.g. pmh0_bicepl249's meshes use
        /// 'N_RepSold01'), so the override points at a texture that doesn't exist and the part
        /// renders white. Restore the authored Bitmap wherever it resolves to a real texture;
        /// keep the composer's override otherwise (the BioWare stale-bitmap case).
        /// </summary>
        private void RestorePartBitmaps(MdlModel composed)
        {
            foreach (var mesh in composed.GetMeshNodes())
            {
                if (string.IsNullOrWhiteSpace(mesh.Bitmap))
                    continue;

                if (_partOriginalBitmaps.TryGetValue((mesh.Bitmap, mesh.Name), out var original) &&
                    !string.Equals(original, mesh.Bitmap, StringComparison.OrdinalIgnoreCase) &&
                    TextureExists(original))
                {
                    mesh.Bitmap = original;
                }
            }
        }

        private bool TextureExists(string name)
        {
            if (_resourceIndex == null)
                return false;

            foreach (var extension in new[] { ".plt", ".tga", ".dds" })
            {
                if (_resourceIndex.TryLookup(ResourceIdentity.FromFileName(name + extension), out _))
                    return true;
            }

            return false;
        }

        /// <summary>Loads an item blueprint's root for armor-part resolution; null when unavailable.</summary>
        private JsonGffStruct? LoadItemBlueprintRoot(string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            try
            {
                return workspace.LoadBlueprint(ResourceType.Uti, resRef).Document.Root;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool PartModelExists(string resRef)
        {
            return _resourceIndex != null &&
                   _resourceIndex.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out _);
        }

        private sealed class TupleBitmapKeyComparer : IEqualityComparer<(string PartResRef, string MeshName)>
        {
            public static readonly TupleBitmapKeyComparer Instance = new();

            public bool Equals((string PartResRef, string MeshName) x, (string PartResRef, string MeshName) y) =>
                string.Equals(x.PartResRef, y.PartResRef, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.MeshName, y.MeshName, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string PartResRef, string MeshName) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PartResRef),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.MeshName));
        }

        /// <summary>Loads and parses an MDL by resref through the layered index; null when missing/unparseable.</summary>
        private MdlModel? LoadModel(string resRef, bool withSupermodelAnims)
        {
            if (_resourceIndex == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            var identity = ResourceIdentity.FromFileName(resRef + ".mdl");
            if (!_resourceIndex.TryLookup(identity, out var handle))
                return null;

            try
            {
                return _mdlReader.Parse(handle.GetBytes());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsPreviewable(ResourceType type) =>
            type is ResourceType.Utc or ResourceType.Utp or ResourceType.Utd;

        private void SetModel(MdlModel? model, string status)
        {
            CurrentModel = model;
            StatusText = status;
        }
    }
}
