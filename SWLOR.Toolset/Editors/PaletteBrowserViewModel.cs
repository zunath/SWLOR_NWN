using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// One node of a browsed palette tree, wrapping the raw struct directly (rather than the
    /// read-only SWLOR.Toolset.Domain.Documents.PaletteNode view) so the light category
    /// rename/delete editing below can reach the node's parent list field and index.
    /// </summary>
    public sealed class PaletteNodeViewModel
    {
        public JsonGffStruct Struct { get; }
        public JsonGffField ParentListField { get; }
        public int IndexInParent { get; }
        public string? ResRef { get; }
        public bool IsLeaf => ResRef != null;
        public string DisplayName { get; }
        public ObservableCollection<PaletteNodeViewModel> Children { get; } = new();

        public PaletteNodeViewModel(JsonGffStruct target, JsonGffField parentListField, int indexInParent)
        {
            Struct = target;
            ParentListField = parentListField;
            IndexInParent = indexInParent;
            ResRef = target.GetOrNull("RESREF")?.GetString();

            var name = target.GetOrNull("NAME")?.GetString();
            var strRef = target.GetOrNull("STRREF")?.GetUnsignedInteger();
            DisplayName = !string.IsNullOrEmpty(name)
                ? name
                : strRef.HasValue
                    ? $"(strref {strRef.Value})"
                    : ResRef ?? "(unnamed)";

            var childList = target.GetOrNull("LIST");
            if (childList?.Elements != null)
            {
                for (var i = 0; i < childList.Elements.Count; i++)
                    Children.Add(new PaletteNodeViewModel(childList.Elements[i], childList, i));
            }
        }
    }

    /// <summary>
    /// Browses one palette (.itp) tree for a blueprint type and lets the caller pick a leaf's
    /// resref to place (<c>onResRefChosen</c>), or cancel (<c>onCancelled</c>) - both close the
    /// host's popup. Also covers the WP3.3 "light" palette editing scope: renaming a category
    /// node's display name and deleting an empty category, saved back to the .itp file through
    /// its own tiny DocumentSession. Creating or moving palette entries is out of scope.
    /// </summary>
    public partial class PaletteBrowserViewModel : ObservableObject
    {
        private readonly string _itpPath;
        private readonly Action<string> _onResRefChosen;
        private readonly Action _onCancelled;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private DocumentSession? _session;

        public string Title { get; }
        public ObservableCollection<PaletteNodeViewModel> Nodes { get; } = new();

        [ObservableProperty]
        private PaletteNodeViewModel? _selectedNode;

        [ObservableProperty]
        private string _renameText = string.Empty;

        [ObservableProperty]
        private string? _statusMessage;

        public bool CanUndo => _session?.UndoStack.CanUndo ?? false;
        public bool CanRedo => _session?.UndoStack.CanRedo ?? false;
        public bool IsDirty => _session?.UndoStack.IsDirty ?? false;

        public PaletteBrowserViewModel(
            string title,
            string itpPath,
            Action<string> onResRefChosen,
            Action onCancelled,
            OutputLogService log,
            IEditorPromptService prompts)
        {
            Title = title;
            _itpPath = itpPath;
            _onResRefChosen = onResRefChosen;
            _onCancelled = onCancelled;
            _log = log;
            _prompts = prompts;

            _session = DocumentSession.Open(_itpPath);
            RebuildTree();
        }

        private void RebuildTree()
        {
            Nodes.Clear();
            if (_session == null)
                return;

            var mainField = _session.Document.Root.GetOrNull("MAIN");
            if (mainField?.Elements == null)
                return;

            for (var i = 0; i < mainField.Elements.Count; i++)
                Nodes.Add(new PaletteNodeViewModel(mainField.Elements[i], mainField, i));
        }

        partial void OnSelectedNodeChanged(PaletteNodeViewModel? value)
        {
            StatusMessage = null;
            RenameText = value is { IsLeaf: false } ? value.DisplayName : string.Empty;
        }

        [RelayCommand]
        private async Task Choose()
        {
            if (SelectedNode is { IsLeaf: true, ResRef: { } resRef })
            {
                if (await TryCloseAsync().ConfigureAwait(true))
                    _onResRefChosen(resRef);
            }
            else
            {
                StatusMessage = "Select a blueprint (leaf) node first.";
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            if (await TryCloseAsync().ConfigureAwait(true))
                _onCancelled();
        }

        [RelayCommand]
        private void RenameCategory()
        {
            if (_session == null || SelectedNode is not { IsLeaf: false } node)
            {
                StatusMessage = "Select a category node first.";
                return;
            }

            var newName = RenameText.Trim();
            if (newName.Length == 0)
            {
                StatusMessage = "Enter a category name first.";
                return;
            }

            _session.Execute($"Rename category to '{newName}'", () =>
            {
                if (node.Struct.TryGet("NAME", out var existing))
                    existing.SetString(newName);
                else
                    node.Struct.Add("NAME",
                        JsonGffField.CreateScalar(GffFieldType.CExoString, JsonStringCodec.Encode(newName)));
            });

            RebuildTree();
            NotifyHistoryChanged();
            StatusMessage = $"Renamed to '{newName}' (not yet saved).";
        }

        [RelayCommand]
        private void DeleteCategory()
        {
            if (_session == null || SelectedNode is not { IsLeaf: false } node)
            {
                StatusMessage = "Select a category node first.";
                return;
            }

            if (node.Children.Count > 0)
            {
                StatusMessage = "Only an empty category can be deleted.";
                return;
            }

            _session.Execute(
                $"Delete category '{node.DisplayName}'",
                () => node.ParentListField.RemoveElementAt(node.IndexInParent));

            SelectedNode = null;
            RebuildTree();
            NotifyHistoryChanged();
            StatusMessage = "Category deleted (not yet saved).";
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            _session?.UndoStack.Undo();
            RebuildTree();
            NotifyHistoryChanged();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            _session?.UndoStack.Redo();
            RebuildTree();
            NotifyHistoryChanged();
        }

        [RelayCommand]
        private async Task SavePalette()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        /// <summary>Saves this palette, returning false when a prompt is cancelled or the write fails.</summary>
        public async Task<bool> TrySaveAsync()
        {
            if (_session == null)
                return true;

            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts
                        .ConfirmExternalChangeAsync(_session.FilePath)
                        .ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                    {
                        StatusMessage = "Save cancelled.";
                        return false;
                    }

                    if (choice == ExternalChangeChoice.Reload)
                    {
                        _session.ReloadFromDisk();
                        SelectedNode = null;
                        RebuildTree();
                        NotifyHistoryChanged();
                        StatusMessage = $"Reloaded {_session.FilePath}.";
                        return true;
                    }
                }

                Services.SaveService.WriteAtomic(_session.FilePath, _session.Document.ToBytes());
                _session.UndoStack.MarkSaved();
                _session.RecordCurrentFileState();
                StatusMessage = $"Saved {_session.FilePath}.";
                NotifyHistoryChanged();
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to save palette '{_itpPath}': {ex.Message}");
                StatusMessage = $"Save failed: {ex.Message}";
                return false;
            }
        }

        private async Task<bool> TryCloseAsync()
        {
            if (IsDirty)
            {
                var choice = await _prompts.ConfirmCloseAsync(Title).ConfigureAwait(true);
                if (choice == UnsavedChangesChoice.Cancel)
                    return false;
                if (choice == UnsavedChangesChoice.Save &&
                    !await TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            CloseSession();
            return true;
        }

        /// <summary>Closes the nested session after its owning area has approved shutdown.</summary>
        internal void DiscardAndClose() => CloseSession();

        private void NotifyHistoryChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(IsDirty));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        private void CloseSession()
        {
            _session?.Dispose();
            _session = null;
            NotifyHistoryChanged();
        }
    }
}
