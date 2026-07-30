using System.Text;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>Whether a snippet guards a route or changes the world.</summary>
    public enum SnippetKind
    {
        Condition,
        Action
    }

    /// <summary>
    /// One snippet as the editor needs it: its key, what it does in a sentence, and what its
    /// arguments mean. Read from the game code rather than restated here, so a snippet added to
    /// <c>SnippetBuilder</c> appears in the editor without anything being kept in step by hand.
    /// </summary>
    public sealed class SnippetDescriptor
    {
        public required string Key { get; init; }

        public required SnippetKind Kind { get; init; }

        /// <summary>The builder's own <c>Description</c> - the tooltip.</summary>
        public required string Description { get; init; }

        /// <summary>The sentence shown in place of the key, with <c>{name}</c> placeholders.</summary>
        public required string Phrase { get; init; }

        /// <summary>The phrase for a negated condition, or empty when none was declared.</summary>
        public required string NegatedPhrase { get; init; }

        public required IReadOnlyList<SnippetArgument> Arguments { get; init; }

        /// <summary>How many trailing arguments repeat, or 0 when the list is fixed.</summary>
        public required int RepeatGroupSize { get; init; }

        public required int MinimumArgumentCount { get; init; }

        /// <summary>
        /// Whether the snippet has enough arguments to run. Surplus arguments are ignored at
        /// runtime, so this is the line between "will fail" and "is merely untidy".
        /// </summary>
        public bool HasEnoughArguments(int count) => count >= MinimumArgumentCount;

        /// <summary>
        /// Whether this many arguments is exactly a shape the snippet declares. Stricter than
        /// <see cref="HasEnoughArguments"/>: a surplus argument, or an odd count where the snippet
        /// reads pairs, is an authoring mistake worth reporting even though the game tolerates it.
        /// </summary>
        public bool IsValidArgumentCount(int count)
        {
            if (count < MinimumArgumentCount)
                return false;

            if (RepeatGroupSize <= 0)
                return count <= Arguments.Count;

            var extra = count - Arguments.Count;
            return extra <= 0 || extra % RepeatGroupSize == 0;
        }

        /// <summary>
        /// The argument at a position, following the repeat group past the end of the declared list
        /// so the fourth quest id in <c>condition-completed-quest</c> still reads as a quest id.
        /// </summary>
        public SnippetArgument? ArgumentAt(int index)
        {
            if (index < 0 || Arguments.Count == 0)
                return null;

            if (index < Arguments.Count)
                return Arguments[index];

            if (RepeatGroupSize <= 0)
                return null;

            var offsetIntoGroup = (index - Arguments.Count) % RepeatGroupSize;
            return Arguments[Arguments.Count - RepeatGroupSize + offsetIntoGroup];
        }

        /// <summary>
        /// The sentence for this snippet with the given arguments substituted in — what a writer
        /// reads instead of <c>condition-on-quest-state field_tinctures 2</c>.
        /// </summary>
        /// <param name="arguments">The argument values, in order.</param>
        /// <param name="negated">True to use the negated phrasing.</param>
        /// <param name="display">
        /// Optional lookup turning a raw value into something readable — a quest id into its name,
        /// a key item number into its label. Returning null falls back to the raw value.
        /// </param>
        public string ToSentence(
            IReadOnlyList<string> arguments,
            bool negated = false,
            Func<SnippetArgument, string, string?>? display = null)
        {
            var template = negated && !string.IsNullOrEmpty(NegatedPhrase) ? NegatedPhrase : Phrase;
            if (string.IsNullOrEmpty(template))
                return negated ? $"not {Key}" : Key;

            string result;
            if (RepeatGroupSize > 1 && arguments.Count > Arguments.Count)
            {
                // A multi-argument repeat is a tuple, not an independent list per placeholder.
                // Render one complete clause for every tuple so Force/10 and Devices/5 cannot turn
                // into the ambiguous "Force and Devices at rank 10 and 5".
                result = RenderRepeatedClauses(template, arguments, negated, display);
            }
            else
            {
                // A repeating snippet has more values than placeholders, so each placeholder
                // collects every value of its argument: three quest ids read as "a, b and c"
                // rather than repeating the whole sentence three times.
                var valuesByName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                for (var i = 0; i < arguments.Count; i++)
                {
                    var argument = ArgumentAt(i);
                    if (argument == null)
                        continue;

                    if (!valuesByName.TryGetValue(argument.Name, out var values))
                        valuesByName[argument.Name] = values = new List<string>();

                    values.Add(display?.Invoke(argument, arguments[i]) ?? arguments[i]);
                }

                result = Substitute(template, valuesByName);
            }

            return negated && string.IsNullOrEmpty(NegatedPhrase) ? $"not: {result}" : result;
        }

        private string RenderRepeatedClauses(
            string template,
            IReadOnlyList<string> values,
            bool negated,
            Func<SnippetArgument, string, string?>? display)
        {
            var fixedArgumentCount = Arguments.Count - RepeatGroupSize;
            var repeatedValueCount = Math.Max(0, values.Count - fixedArgumentCount);
            var groupCount = Math.Max(1, (repeatedValueCount + RepeatGroupSize - 1) / RepeatGroupSize);
            var clauses = new List<string>(groupCount);

            for (var group = 0; group < groupCount; group++)
            {
                var valuesByName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                for (var i = 0; i < fixedArgumentCount && i < values.Count; i++)
                    AddDisplayedValue(valuesByName, Arguments[i], values[i], display);

                var groupStart = fixedArgumentCount + group * RepeatGroupSize;
                for (var offset = 0; offset < RepeatGroupSize; offset++)
                {
                    var valueIndex = groupStart + offset;
                    if (valueIndex >= values.Count)
                        break;

                    AddDisplayedValue(
                        valuesByName,
                        Arguments[fixedArgumentCount + offset],
                        values[valueIndex],
                        display);
                }

                clauses.Add(Substitute(template, valuesByName));
            }

            // The two current tuple snippets are the dual any/all skill conditions. Their negated
            // forms reverse the connective by De Morgan's law; future tuple actions default to
            // sequencing their clauses with "and".
            var anyCondition = Kind == SnippetKind.Condition &&
                               Key.Contains("-any-", StringComparison.Ordinal);
            var joinWithOr = anyCondition ^ negated;
            return string.Join(joinWithOr ? " or " : " and ", clauses);
        }

        private static void AddDisplayedValue(
            IDictionary<string, List<string>> valuesByName,
            SnippetArgument argument,
            string value,
            Func<SnippetArgument, string, string?>? display)
        {
            valuesByName[argument.Name] = new List<string>
            {
                display?.Invoke(argument, value) ?? value
            };
        }

        private static string Substitute(string template, IReadOnlyDictionary<string, List<string>> valuesByName)
        {
            var builder = new StringBuilder(template.Length);
            var index = 0;
            while (index < template.Length)
            {
                var open = template.IndexOf('{', index);
                if (open < 0)
                {
                    builder.Append(template, index, template.Length - index);
                    break;
                }

                var close = template.IndexOf('}', open);
                if (close < 0)
                {
                    builder.Append(template, index, template.Length - index);
                    break;
                }

                builder.Append(template, index, open - index);
                var name = template[(open + 1)..close];
                builder.Append(valuesByName.TryGetValue(name, out var values)
                    ? JoinReadably(values)
                    : $"⟨{name}⟩");

                index = close + 1;
            }

            return builder.ToString();
        }

        private static string JoinReadably(IReadOnlyList<string> values)
        {
            return values.Count switch
            {
                0 => string.Empty,
                1 => values[0],
                2 => $"{values[0]} and {values[1]}",
                _ => $"{string.Join(", ", values.Take(values.Count - 1))} and {values[^1]}"
            };
        }
    }

    /// <summary>
    /// Every conversation snippet the game registers, read once by reflecting over
    /// <c>SWLOR.Game.Server</c>'s <c>ISnippetListDefinition</c> implementations — the same
    /// mechanism the server itself uses at module load.
    /// </summary>
    /// <remarks>
    /// Building the catalog runs each definition's <c>BuildSnippets()</c>, which only calls builder
    /// methods; the delegates it stores are never invoked, so nothing here touches the NWN engine
    /// and this is safe in a headless process.
    /// </remarks>
    public sealed class SnippetCatalog
    {
        private readonly Dictionary<string, SnippetDescriptor> _byKey;

        private SnippetCatalog(Dictionary<string, SnippetDescriptor> byKey)
        {
            _byKey = byKey;
        }

        /// <summary>Every snippet, ordered by key.</summary>
        public IReadOnlyList<SnippetDescriptor> All =>
            _byKey.Values.OrderBy(snippet => snippet.Key, StringComparer.Ordinal).ToList();

        public IReadOnlyList<SnippetDescriptor> Conditions =>
            All.Where(snippet => snippet.Kind == SnippetKind.Condition).ToList();

        public IReadOnlyList<SnippetDescriptor> Actions =>
            All.Where(snippet => snippet.Kind == SnippetKind.Action).ToList();

        /// <summary>
        /// Looks a snippet up by the key stored in a conversation, tolerating the leading '!' the
        /// negated form carries.
        /// </summary>
        public SnippetDescriptor? Find(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var bare = key.StartsWith('!') ? key[1..] : key;
            return _byKey.TryGetValue(bare, out var snippet) ? snippet : null;
        }

        public bool IsKnown(string? key) => Find(key) != null;

        public static SnippetCatalog Build()
        {
            var byKey = new Dictionary<string, SnippetDescriptor>(StringComparer.Ordinal);

            var definitionTypes = typeof(ISnippetListDefinition).Assembly
                .GetTypes()
                .Where(type => typeof(ISnippetListDefinition).IsAssignableFrom(type)
                               && !type.IsInterface
                               && !type.IsAbstract);

            foreach (var type in definitionTypes)
            {
                if (Activator.CreateInstance(type) is not ISnippetListDefinition definition)
                    continue;

                foreach (var (key, detail) in definition.BuildSnippets())
                {
                    byKey[key] = new SnippetDescriptor
                    {
                        Key = key,
                        Kind = detail.ConditionAction != null ? SnippetKind.Condition : SnippetKind.Action,
                        Description = detail.Description,
                        Phrase = detail.Phrase,
                        NegatedPhrase = detail.NegatedPhrase,
                        Arguments = detail.Arguments.ToList(),
                        RepeatGroupSize = detail.RepeatGroupSize,
                        MinimumArgumentCount = detail.MinimumArgumentCount
                    };
                }
            }

            return new SnippetCatalog(byKey);
        }
    }
}
