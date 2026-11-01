using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Gff;
using System.Numerics;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Preview.Areas.Clipboard;
using Nwn.Toolset.Avalonia.Areas;

namespace SWLOR.Toolset.Editors;

public partial class AreaEditorViewModel
{
    private bool _preservingMultipleSelection;
    private IReadOnlyList<AreaInstanceClipboardEntry>? _pendingPlacementCopies;
    private AreaTileClipboardEntry? _pendingTileCopy;
    public IReadOnlyList<int> TilePlacementOrientations
    {
        get
        {
            if (_pendingTileCopy is not { } copy)
                return Enumerable.Repeat(_pendingTileOrientation, _pendingTile?.TileIds.Count ?? 0).ToArray();
            var orientations = new int[copy.Columns * copy.Rows];
            foreach (var tile in copy.Tiles)
                orientations[tile.Row * copy.Columns + tile.Column] = tile.Tile.GetIntOrNull("Tile_Orientation") ?? 0;
            return orientations;
        }
    }
    public IReadOnlyList<float?> TilePlacementHeightOffsets
    {
        get
        {
            if (_pendingTileCopy is not { } copy) return Array.Empty<float?>();
            var heights = new float?[copy.Columns * copy.Rows];
            var transition = ResolveTilesetForEditing(copy.Tileset)?.Transition ?? 0;
            foreach (var tile in copy.Tiles)
                heights[tile.Row * copy.Columns + tile.Column] = (tile.Tile.GetIntOrNull("Tile_Height") ?? 0) * transition;
            return heights;
        }
    }
    public IReadOnlyList<InstanceMarker> PlacementGhosts =>
        _pendingPlacementCopies?.Select(copy => copy.Preview).ToArray() ?? Array.Empty<InstanceMarker>();
    public IReadOnlyList<InstanceMarker> SelectedSceneInstances { get; private set; } = Array.Empty<InstanceMarker>();
    public IReadOnlyList<(int Column, int Row)> SelectedTiles =>
        _tileEditor?.SelectedCells ?? Array.Empty<(int, int)>();

    public void SelectSceneSelection(AreaSelectionPick pick)
    {
        var instances = pick.ExtendSelection ? SelectedSceneInstances.ToList() : new List<InstanceMarker>();
        var tiles = pick.ExtendSelection ? SelectedTiles.ToList() : new List<(int Column, int Row)>();
        if (pick.Instances.Count > 0) tiles.Clear();
        if (pick.Tiles.Count > 0) instances.Clear();
        foreach (var instance in pick.Instances)
        {
            if (pick.ExtendSelection && instances.Remove(instance)) continue;
            instances.Add(instance);
        }
        foreach (var cell in pick.Tiles)
        {
            if (pick.ExtendSelection && tiles.Remove(cell)) continue;
            tiles.Add(cell);
        }
        _preservingMultipleSelection = true;
        try { ApplySelection(instances.LastOrDefault()); }
        finally { _preservingMultipleSelection = false; }
        SelectedSceneInstances = instances.AsReadOnly();
        OnPropertyChanged(nameof(SelectedSceneInstances));
        SelectedTile = tiles.Count == 0 ? null : tiles[^1];
        _tileEditor?.SelectCells(tiles);
        OnPropertyChanged(nameof(SelectedTiles));
    }

    private bool CopySceneSelection()
    {
        if (SelectedTiles.Count > 0 && _tileEditor?.CopySelection() is { } tiles)
        {
            _instanceClipboard.SetTiles(tiles);
            SceneStatus = "Copied tiles. Press Ctrl+V to place a copy.";
            return true;
        }
        var selected = SelectedSceneInstances.Count > 0 ? SelectedSceneInstances :
            SelectedSceneInstance is { } instance ? new[] { instance } : Array.Empty<InstanceMarker>();
        var copies = new List<AreaInstanceClipboardEntry>();
        foreach (var marker in selected)
        {
            if (SectionForKind(marker.Kind) is not { } section ||
                section.CopyInstanceForPlacement(IndexWithinKind(marker), marker) is not { } copy)
                return false;
            copies.Add(copy);
        }
        if (copies.Count == 0) return false;
        _instanceClipboard.Set(copies);
        SceneStatus = "Copied objects. Press Ctrl+V to place a copy.";
        return true;
    }

    private bool ArmCopiedTiles(AreaTileClipboardEntry copy)
    {
        if (!string.Equals(TilesetResRef, copy.Tileset, StringComparison.OrdinalIgnoreCase))
        {
            SceneStatus = "Copied tiles require the same tileset.";
            return false;
        }
        var ids = Enumerable.Repeat(-1, copy.Columns * copy.Rows).ToArray();
        foreach (var tile in copy.Tiles)
            ids[tile.Row * copy.Columns + tile.Column] = tile.Tile.GetIntOrNull("Tile_ID") ?? -1;
        if (!ArmTilePlacement(new TilePaletteEntry("Copied tiles", ids, copy.Columns, copy.Rows, string.Empty)))
            return false;
        _pendingTileCopy = copy;
        OnPropertyChanged(nameof(TilePlacementOrientations));
        OnPropertyChanged(nameof(TilePlacementHeightOffsets));
        OnPropertyChanged(nameof(CanRotatePendingTile));
        OnPropertyChanged(nameof(PlacementStatus));
        return true;
    }

    public void CycleTileVariant(int column, int row)
    {
        if (_tileEditor != null) PublishTileClipboardEdit(_tileEditor.CycleVariant(column, row));
    }

    private void PublishTileClipboardEdit(AreaTileEditOutcome outcome)
    {
        if (outcome != AreaTileEditOutcome.Changed) return;
        InvalidateTilePlacementValidity();
        Interlocked.Increment(ref _sceneInputRevision);
        RequestSceneRefresh(immediate: true);
        AfterHistoryChange();
    }

    private void CommitCopiedInstances(IReadOnlyList<AreaInstanceClipboardEntry> copies, Vector3 position)
    {
        var delta = position - copies[0].Preview.Position;
        var pending = new List<AreaInstanceCopy>();
        var reservedDoorways = new HashSet<Vector3>();
        foreach (var copy in copies)
        {
            var instance = InstanceFieldMap.Duplicate(copy.Instance);
            var source = InstanceFieldMap.GetPosition(copy.Type, instance);
            var destination = new Vector3(source.X, source.Y, source.Z) + delta;
            // Doors still belong to an available tile doorway when pasted as part of a group.
            if (copy.Type == ResourceType.Utd)
            {
                if (AreaScene?.NearestEmptyDoorway(destination) is not { } anchor ||
                    Vector3.Distance(anchor.Position, destination) > 0.001f || !reservedDoorways.Add(anchor.Position))
                {
                    SceneStatus = "Copied doors must land in distinct empty doorways.";
                    return;
                }
                InstanceFieldMap.SetOrientation(copy.Type, instance, anchor.Orientation.X, anchor.Orientation.Y);
            }
            InstanceFieldMap.SetPosition(copy.Type, instance, destination.X, destination.Y, destination.Z);
            pending.Add(new AreaInstanceCopy(copy.Type, instance, copy.Comment));
        }
        if (_instanceEditor.AddCopies(pending)) PublishBatchInstanceEdit();
    }

    private void PublishBatchInstanceEdit()
    {
        ApplySelection(null);
        RefreshInstanceSections();
        RefreshGicDirty();
        Interlocked.Increment(ref _sceneInputRevision);
        RequestSceneRefresh(immediate: true);
        AfterHistoryChange();
    }
}
