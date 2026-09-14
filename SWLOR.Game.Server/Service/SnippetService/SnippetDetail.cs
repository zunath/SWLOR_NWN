using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SnippetService
{
    public delegate bool SnippetConditionDelegate(uint player, string[] args);

    /// <summary>
    /// Runs a conversation outcome and reports whether it completed successfully.
    /// </summary>
    public delegate bool SnippetActionDelegate(uint player, string[] args);
    public class SnippetDetail
    {
        public string Description { get; set; }

        /// <summary>
        /// A sentence describing what this snippet checks or does, with <c>{argumentName}</c>
        /// placeholders for its arguments - what a conversation editor shows a writer in place of the
        /// snippet key. Empty when the snippet has not declared one.
        /// </summary>
        public string Phrase { get; set; }

        /// <summary>
        /// The <see cref="Phrase"/> equivalent for a condition written with a leading '!'. Only worth
        /// declaring where simple negation reads badly; empty means the reader should fall back to
        /// negating <see cref="Phrase"/> itself.
        /// </summary>
        public string NegatedPhrase { get; set; }

        /// <summary>The arguments this snippet reads, in the order they appear in the value string.</summary>
        public List<SnippetArgument> Arguments { get; }

        /// <summary>
        /// How many trailing arguments may repeat, or 0 when the argument list is fixed. It is 1 for
        /// a snippet taking any number of quest ids, and 2 for the skill snippets, which read their
        /// arguments as skill/rank pairs.
        /// </summary>
        public int RepeatGroupSize { get; set; }

        /// <summary>
        /// The fewest arguments this snippet can run with - every declared argument that is not
        /// optional.
        /// </summary>
        public int MinimumArgumentCount
        {
            get
            {
                var count = 0;
                foreach (var argument in Arguments)
                {
                    if (!argument.IsOptional)
                        count++;
                }

                return count;
            }
        }

        public SnippetConditionDelegate ConditionAction { get; set; }
        public SnippetActionDelegate ActionsTakenAction { get; set; }

        public SnippetDetail()
        {
            Description = string.Empty;
            Phrase = string.Empty;
            NegatedPhrase = string.Empty;
            Arguments = new List<SnippetArgument>();
        }

        /// <summary>
        /// Whether the snippet has enough arguments to do anything at all. This is the runtime
        /// gate, and it deliberately ignores surplus arguments.
        /// </summary>
        /// <remarks>
        /// Every snippet reads its arguments positionally and ignores any it was not expecting, so a
        /// surplus is harmless at runtime - and one conversation in the module relies on that today
        /// (<c>rorrska_buvvien</c> passes a state number to <c>condition-has-quest</c>). Rejecting it
        /// here would turn a working, if loose, guard into a failing one. Surplus is an authoring
        /// mistake worth reporting in an editor, which is what <see cref="IsValidArgumentCount"/> is
        /// for; it is not a reason to refuse to run.
        /// </remarks>
        public bool HasEnoughArguments(int argumentCount)
        {
            return argumentCount >= MinimumArgumentCount;
        }

        /// <summary>
        /// Whether <paramref name="argumentCount"/> is exactly a shape this snippet declares - the
        /// stricter editor-side check. A repeating snippet accepts whole extra groups beyond its
        /// declared list and nothing in between, which is what makes an odd argument count to the
        /// skill snippets visible as the mistake it is.
        /// </summary>
        public bool IsValidArgumentCount(int argumentCount)
        {
            if (argumentCount < MinimumArgumentCount)
                return false;

            if (RepeatGroupSize <= 0)
                return argumentCount <= Arguments.Count;

            var extra = argumentCount - Arguments.Count;
            if (extra <= 0)
                return true;

            return extra % RepeatGroupSize == 0;
        }
    }
}
