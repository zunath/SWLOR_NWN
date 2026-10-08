using Nwn.Toolset.Avalonia.Areas.Contents;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Shell.Panels;

/// <summary>Applies shared contents actions through SWLOR's active area editor.</summary>
public sealed class AreaContentsActions : IAreaContentsActions
{
    private readonly AreaEditorViewModel _editor;
    private readonly IEditorPromptService? _prompts;

    public AreaContentsActions(AreaEditorViewModel editor, IEditorPromptService? prompts)
    {
        _editor = editor;
        _prompts = prompts;
    }

    public void Select(AreaContentsIdentity identity) =>
        _editor.RevealInstance(identity.ResourceType, identity.InstanceIndex, frameCamera: false);

    public void Frame(AreaContentsIdentity identity) =>
        _editor.RevealInstance(identity.ResourceType, identity.InstanceIndex, frameCamera: true);

    public void OpenProperties(AreaContentsIdentity identity) =>
        _editor.OpenInstanceProperties(identity.ResourceType, identity.InstanceIndex);

    public async Task<bool> DeleteAsync(
        IReadOnlyList<AreaContentsIdentity> identities,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        if (identities.Count == 0 ||
            identities.Select(identity => identity.ResourceType).Distinct().Count() != 1)
            return false;

        if (identities.Count > 1)
        {
            if (_prompts is null)
                return false;

            var count = identities.Count;
            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete {count} objects from {_editor.AreaResRef}?",
                $"Every object under “{displayName}” is removed from this area. " +
                "The area is not saved by this, so Undo takes it back; saving afterwards does not.",
                $"Delete {count}").ConfigureAwait(true);
            if (!confirmed)
                return false;
        }

        return _editor.DeleteInstances(
            identities[0].ResourceType,
            identities.Select(identity => identity.InstanceIndex).ToArray());
    }
}
