using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SnippetService
{
    public class SnippetBuilder
    {
        private readonly Dictionary<string, SnippetDetail> _snippets = new Dictionary<string, SnippetDetail>();
        private SnippetDetail _activeSnippet;

        /// <summary>
        /// Creates a new snippet with the specified key.
        /// </summary>
        /// <param name="key">The key of the snippet.</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder Create(string key)
        {
            _activeSnippet = new SnippetDetail();
            _snippets[key] = _activeSnippet;

            return this;
        }

        /// <summary>
        /// Sets the description of the active snippet.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder Description(string description)
        {
            _activeSnippet.Description = description;

            return this;
        }

        /// <summary>
        /// Sets the action which will run when the Appears When event is called in a conversation.
        /// </summary>
        /// <param name="condition">The action to run.</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder AppearsWhenAction(SnippetConditionDelegate condition)
        {
            _activeSnippet.ConditionAction = condition;

            return this;
        }

        /// <summary>
        /// Sets the action which will run when the Actions Taken event is called in a conversation.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder ActionsTakenAction(SnippetActionDelegate action)
        {
            _activeSnippet.ActionsTakenAction = action;

            return this;
        }

        /// <summary>
        /// Returns a built dictionary of snippet details.
        /// </summary>
        /// <returns>A dictionary of snippet details.</returns>
        public Dictionary<string, SnippetDetail> Build()
        {
            return _snippets;
        }
    }
}
