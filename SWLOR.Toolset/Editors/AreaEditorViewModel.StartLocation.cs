using System.Numerics;
using Nwn.Authoring.Areas.Editing;

namespace SWLOR.Toolset.Editors;

public partial class AreaEditorViewModel
{
    private readonly ModuleEntryEditor? _moduleEntry;
    private bool _startLocationEdited;
    private bool HasModuleEntryChanges => _startLocationEdited && _moduleEntry?.IsEntryDirty == true;

    private InstanceMarker? StartLocationMarker()
    {
        if (_moduleEntry?.Document is not { } module ||
            !string.Equals(module.EntryArea, _areResRef, StringComparison.OrdinalIgnoreCase)) return null;
        return new InstanceMarker
        {
            Kind = InstanceMarkerKind.StartLocation, Tag = "Player Start",
            Position = new Vector3(module.EntryX, module.EntryY, module.EntryZ),
            Orientation = new Vector2(module.EntryDirectionX, module.EntryDirectionY)
        };
    }

    private void RotateStartLocation(Vector2 orientation)
    {
        if (_moduleEntry == null) return;
        if (!_documentEdits.ExecuteAreaRelated("Rotate player start", () =>
                _moduleEntry.Document.SetEntryDirection(orientation.X, orientation.Y), _moduleEntry.Session)) return;
        _startLocationEdited = true;
        _moduleEntry.NotifyChanged();
        AfterHistoryChange();
    }

    private void OnModuleEntryChanged()
    {
        if (AreaScene is { } scene)
        {
            var selected = SelectedSceneInstance?.Kind == InstanceMarkerKind.StartLocation;
            AreaScene = scene;
            if (selected) ApplySelection(AreaScene!.Instances.FirstOrDefault(marker => marker.Kind == InstanceMarkerKind.StartLocation));
        }
        AfterHistoryChange();
    }
}
