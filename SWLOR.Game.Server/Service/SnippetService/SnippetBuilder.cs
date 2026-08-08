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
        /// Sets the sentence a conversation editor shows in place of this snippet's key. Refer to
        /// arguments as <c>{name}</c>, matching the names given to <see cref="Argument"/>.
        /// </summary>
        /// <param name="phrase">The phrase to set, e.g. "the player is on step {state} of {questId}".</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder Phrase(string phrase)
        {
            _activeSnippet.Phrase = phrase;

            return this;
        }

        /// <summary>
        /// Sets the phrase used when a condition is written with a leading '!'. Only needed where
        /// simply negating <see cref="Phrase"/> would read badly.
        /// </summary>
        /// <param name="phrase">The negated phrase to set, e.g. "the player is not doing {questId}".</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder NegatedPhrase(string phrase)
        {
            _activeSnippet.NegatedPhrase = phrase;

            return this;
        }

        /// <summary>
        /// Declares an argument this snippet reads. Declare them in the order they appear in the
        /// conversation's value string.
        /// </summary>
        /// <param name="name">The name a phrase template refers to this argument by.</param>
        /// <param name="type">What kind of value it holds, so an editor can offer the right picker.</param>
        /// <param name="isOptional">True when the snippet still works without it.</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder Argument(string name, SnippetArgumentType type, bool isOptional = false)
        {
            _activeSnippet.Arguments.Add(new SnippetArgument(name, type, isOptional));

            return this;
        }

        /// <summary>
        /// Declares that the last <paramref name="groupSize"/> declared arguments may repeat, so the
        /// snippet accepts any number of further groups of that size.
        /// </summary>
        /// <param name="groupSize">How many trailing arguments form one repeatable group.</param>
        /// <returns>A snippet builder with the configured options.</returns>
        public SnippetBuilder Repeats(int groupSize = 1)
        {
            _activeSnippet.RepeatGroupSize = groupSize;

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
