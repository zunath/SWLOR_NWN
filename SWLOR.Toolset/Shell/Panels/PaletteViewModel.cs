using System.ComponentModel;
using Avalonia.Media.Imaging;
using Dock.Model.Mvvm.Controls;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Workspace;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Palette dock tool. The workflow - snapshots, type/source/mode switching, categories, place, edit,
    /// Edit Copy, delete and create - is the shared <see cref="PaletteWorkflowController"/>; this panel
    /// wires SWLOR's data and services into it and hosts it in the dock layout.
    /// </summary>
    /// <remarks>
    /// SWLOR supplies the module's JSON blueprints and the base game's <c>*palstd</c> palettes, the category
    /// sidecar through <see cref="CategoryService"/>, its file writes and editors, its prompts, thumbnails,
    /// tilesets, <see cref="ToolsetSettings"/> keys and the module mutation lock.
    /// </remarks>
    public class PaletteViewModel : Tool
    {
        private readonly PaletteWorkflowController _workflow;

        public PaletteViewModel(
            WorkspaceContext workspaceContext,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            Func<IAreaPlacementTarget?>? placementTarget = null,
            ThumbnailService? thumbnails = null,
            Services.IEditorPromptService? prompts = null,
            TilesetCatalog? tilesets = null,
            Domain.GameData.Tlk.TlkService? tlk = null,
            ToolsetSettings? settings = null,
            Services.ModuleMutationLock? mutationLock = null)
        {
            ArgumentNullException.ThrowIfNull(workspaceContext);
            ArgumentNullException.ThrowIfNull(categories);
            ArgumentNullException.ThrowIfNull(log);

            Id = "Palette";
            Title = "Palette";

            _workflow = new PaletteWorkflowController(
                new PaletteWorkflowHost(
                    new SwlorPaletteContentSource(workspaceContext, categories),
                    new SwlorPaletteCategoryStore(categories))
                {
                    Blueprints = new SwlorPaletteBlueprintOperations(workspaceContext, editorService),
                    Prompts = prompts is null ? null : new SwlorPalettePrompts(prompts),
                    Previews = thumbnails is null ? null : new SwlorPalettePreviewSource(thumbnails),
                    Placement = placementTarget is null ? null : new SwlorPalettePlacementTargets(placementTarget),
                    Tilesets = tilesets is null
                        ? null
                        : new SwlorPaletteTilesetSource(tilesets, tlk is null ? null : tlk.GetString, log),
                    Settings = settings is null ? null : new SwlorPaletteSettings(settings),
                    WriteGate = mutationLock is null ? null : new SwlorPaletteWriteGate(mutationLock),
                    Log = new SwlorPaletteLog(log)
                });
            _workflow.PropertyChanged += OnWorkflowPropertyChanged;
        }

        /// <summary>The shared palette workflow this panel hosts.</summary>
        public PaletteWorkflowController Workflow => _workflow;

        public PalettePresentationState PresentationState => _workflow.PresentationState;

        public ResourceType SelectedType
        {
            get => _workflow.SelectedType;
            set => _workflow.SelectedType = value;
        }

        /// <summary>Whether the tree and grid show the module's own blueprints or the base game's.</summary>
        public PaletteSource Source
        {
            get => _workflow.Source == SharedPaletteSource.Standard ? PaletteSource.Standard : PaletteSource.Custom;
            set => _workflow.Source = value == PaletteSource.Standard
                ? SharedPaletteSource.Standard
                : SharedPaletteSource.Custom;
        }

        public bool IsTileMode
        {
            get => _workflow.IsTileMode;
            set => _workflow.IsTileMode = value;
        }

        public TilePaintMode TilePaintMode
        {
            get => _workflow.TilePaintMode;
            set => _workflow.TilePaintMode = value;
        }

        public double TileSize
        {
            get => _workflow.TileSize;
            set => _workflow.TileSize = value;
        }

        public double CategoryProportion
        {
            get => _workflow.CategoryProportion;
            set => _workflow.CategoryProportion = value;
        }

        public string? StatusMessage
        {
            get => _workflow.StatusMessage;
            set => _workflow.StatusMessage = value;
        }

        public bool NeedsOpenArea => _workflow.NeedsOpenArea;

        public bool IsCustomSource => _workflow.IsCustomSource;

        public bool IsStandardSource => _workflow.IsStandardSource;

        public bool IsBlueprintMode => _workflow.IsBlueprintMode;

        public bool ShowsSourceSwitch => _workflow.ShowsSourceSwitch;

        public bool ShowsTilePaintSwitch => _workflow.ShowsTilePaintSwitch;

        public bool IsAutoTilePaint => _workflow.IsAutoTilePaint;

        public bool IsManualTilePaint => _workflow.IsManualTilePaint;

        public bool CanWrite => _workflow.CanWrite;

        public bool CanEditCopy => _workflow.CanEditCopy;

        public bool CanCreateBlueprint => _workflow.CanCreateBlueprint;

        public bool HasBlueprintActions => _workflow.HasBlueprintActions;

        public string? ReadOnlyNotice => _workflow.ReadOnlyNotice;

        public bool HasReadOnlyNotice => _workflow.HasReadOnlyNotice;

        public string NewBlueprintLabel => _workflow.NewBlueprintLabel;

        /// <summary>Rebuilds the tree and grid for the current type. Safe to call whenever the module changes.</summary>
        public void Refresh() => _workflow.Refresh();

        /// <summary>The area in front changed; only Tiles mode depends on it.</summary>
        public void OnActiveAreaChanged() => _workflow.OnActiveAreaChanged();

        /// <summary>Re-reads the write capabilities, for when the module-wide lock has flipped.</summary>
        public void NotifyWriteAvailabilityChanged() => _workflow.NotifyWriteAvailabilityChanged();

        public void SelectType(PaletteTypeOption type) => _workflow.SelectType(type);

        public void SelectSource(SharedPaletteSource source) => _workflow.SelectSource(source);

        public void SelectMode(PaletteMode mode) => _workflow.SelectMode(mode);

        public void SelectTilePaintMode(PaletteTilePaintMode mode) => _workflow.SelectTilePaintMode(mode);

        public void SelectCategory(PaletteCategoryId? categoryId) => _workflow.SelectCategory(categoryId);

        public void Place(PaletteEntrySnapshot entry) => _workflow.Place(entry);

        public void Edit(PaletteEntrySnapshot entry) => _workflow.Edit(entry);

        public void EditCopy(PaletteEntrySnapshot entry) => _workflow.EditCopy(entry);

        public Task DeleteAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken) =>
            _workflow.DeleteAsync(entry, cancellationToken);

        public ValueTask<Bitmap?> LoadPreviewAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken) =>
            _workflow.LoadPreviewAsync(entry, cancellationToken);

        /// <summary>Re-raises the workflow's changes under the same names, which this panel mirrors.</summary>
        private void OnWorkflowPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
            OnPropertyChanged(e.PropertyName);
    }
}
