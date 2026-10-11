using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests.Support;

/// <summary>Answers each text prompt with the next queued value (null once empty) and confirms nothing.</summary>
public sealed class QueuedTextPrompts : IEditorPromptService
{
    public Queue<string?> Answers { get; } = new();

    public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
        Task.FromResult(UnsavedChangesChoice.Cancel);

    public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
        Task.FromResult(ExternalChangeChoice.Cancel);

    public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
        Task.FromResult(false);

    public Task<string?> PromptForTextAsync(string headline, string message, string initialValue, string confirmLabel) =>
        Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);
}
