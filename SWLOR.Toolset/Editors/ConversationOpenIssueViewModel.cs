using Dock.Model.Mvvm.Controls;

namespace SWLOR.Toolset.Editors;

/// <summary>
/// A visible, read-only result for a conversation that cannot safely be represented by an editor.
/// Explorer clicks must never disappear into the Output log or a modal that can itself fail.
/// </summary>
public sealed class ConversationOpenIssueViewModel : Document
{
    private bool _closed;

    public ConversationOpenIssueViewModel(
        string identity,
        string resRef,
        string headline,
        string message,
        string filePath,
        IReadOnlyList<string>? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(resRef);

        Id = $"conversation-open-issue:{identity}";
        Title = resRef;
        ResRef = resRef;
        Headline = headline;
        Message = message;
        FilePath = filePath;
        Details = details ?? Array.Empty<string>();
    }

    public event Action<ConversationOpenIssueViewModel>? Closed;

    public string ResRef { get; }
    public string Headline { get; }
    public string Message { get; }
    public string FilePath { get; }
    public IReadOnlyList<string> Details { get; }
    public bool HasDetails => Details.Count > 0;

    public override bool OnClose()
    {
        if (!_closed)
        {
            _closed = true;
            Closed?.Invoke(this);
        }

        return base.OnClose();
    }
}
