using Dock.Model.Mvvm.Controls;
using Nwn.Toolset.Avalonia.Areas.Contents;
using SharedAreaContentsViewModel = Nwn.Toolset.Avalonia.Areas.Contents.AreaContentsViewModel;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Shell.Panels;

/// <summary>Adapts SWLOR area documents to the shared Area Contents tree.</summary>
public sealed class AreaContentsViewModel : Tool
{
    private readonly IEditorPromptService? _prompts;
    private AreaEditorViewModel? _editor;
    private AreaContentsActions? _actions;

    public AreaContentsViewModel(IEditorPromptService? prompts = null)
    {
        _prompts = prompts;
        Id = "AreaContents";
        Title = "Area Contents";
        Contents = new SharedAreaContentsViewModel();
    }

    public SharedAreaContentsViewModel Contents { get; }
    public bool HasArea => _editor is not null;

    public void SetEditor(AreaEditorViewModel? editor)
    {
        if (ReferenceEquals(editor, _editor))
            return;

        if (_editor is not null)
        {
            _editor.ContentsChanged -= Refresh;
            _editor.AreaContentsRevealRequested -= OnReveal;
        }

        _editor = editor;
        _actions = editor is null ? null : new AreaContentsActions(editor, _prompts);
        if (editor is not null)
        {
            editor.ContentsChanged += Refresh;
            editor.AreaContentsRevealRequested += OnReveal;
        }

        OnPropertyChanged(nameof(HasArea));
        Refresh();
        if (editor?.TryTakePendingAreaContentsReveal(out var type, out var index) == true)
            Contents.Reveal(new AreaContentsIdentity(type, index));
    }

    private void OnReveal(ResourceType type, int index)
    {
        if (_editor?.TryTakePendingAreaContentsReveal(out var pendingType, out var pendingIndex) == true)
            Contents.Reveal(new AreaContentsIdentity(pendingType, pendingIndex));
    }

    private void Refresh()
    {
        if (_editor is null)
        {
            Contents.SetContents(null, null);
            return;
        }

        var sections = _editor.Sections
            .Select(section => new AreaContentsSection(
                section.BlueprintType,
                section.Title,
                section.Rows.Select(row => new AreaContentsEntry(
                    section.BlueprintType,
                    row.Index,
                    _editor.ResolveInstanceName(section.BlueprintType, row),
                    row.TemplateResRef,
                    row.Tag,
                    new System.Numerics.Vector3(row.X, row.Y, row.Z)))
                    .ToArray()))
            .ToArray();
        Contents.SetContents(new AreaContentsSnapshot(_editor.AreaResRef, sections), _actions);
    }
}
