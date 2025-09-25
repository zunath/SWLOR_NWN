using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Shared.Domain.Dialog.Contracts;

public interface ISnippetBuilder
{
    /// <summary>
    /// Creates a new snippet with the specified key.
    /// </summary>
    /// <param name="key">The key of the snippet.</param>
    /// <returns>A snippet builder with the configured options.</returns>
    ISnippetBuilder Create(string key);

    /// <summary>
    /// Sets the description of the active snippet.
    /// </summary>
    /// <param name="description">The description to set.</param>
    /// <returns>A snippet builder with the configured options.</returns>
    ISnippetBuilder Description(string description);

    /// <summary>
    /// Sets the action which will run when the Appears When event is called in a conversation.
    /// </summary>
    /// <param name="condition">The action to run.</param>
    /// <returns>A snippet builder with the configured options.</returns>
    ISnippetBuilder AppearsWhenAction(SnippetConditionDelegate condition);

    /// <summary>
    /// Sets the action which will run when the Actions Taken event is called in a conversation.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>A snippet builder with the configured options.</returns>
    ISnippetBuilder ActionsTakenAction(SnippetActionDelegate action);

    /// <summary>
    /// Returns a built dictionary of snippet details.
    /// </summary>
    /// <returns>A dictionary of snippet details.</returns>
    Dictionary<string, SnippetDetail> Build();
}