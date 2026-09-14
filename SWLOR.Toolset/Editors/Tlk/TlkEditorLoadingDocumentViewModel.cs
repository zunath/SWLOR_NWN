using Dock.Model.Mvvm.Controls;

namespace SWLOR.Toolset.Editors.Tlk;

/// <summary>The immediately visible document shown while the TLK and its references are indexed.</summary>
public sealed class TlkEditorLoadingDocumentViewModel : Document
{
    private readonly CancellationTokenSource _cancellation = new();
    private bool _completed;
    private bool _closed;

    public TlkEditorLoadingDocumentViewModel(string jsonPath)
    {
        Id = $"tlk-editor-loading:{jsonPath}";
        Title = "TLK Editor";
    }

    internal CancellationToken CancellationToken => _cancellation.Token;
    public bool CancellationRequested => _cancellation.IsCancellationRequested;

    public event Action<TlkEditorLoadingDocumentViewModel>? Closed;

    internal void Complete() => _completed = true;

    public override bool OnClose()
    {
        if (_closed)
            return base.OnClose();

        _closed = true;
        if (!_completed)
            _cancellation.Cancel();
        Closed?.Invoke(this);
        return base.OnClose();
    }
}
