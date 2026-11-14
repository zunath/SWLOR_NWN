using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The declared order of every script's <see cref="NWNEventHandler"/> methods. <see cref="ScriptRegistry"/> runs
    /// each script's handlers in the order this manifest lists them, so the order no longer depends on the order
    /// reflection returns types and methods in (which follows namespaces and file paths).
    ///
    /// The manifest is the text file <c>Core/ScriptHandlerOrder.txt</c>, embedded in the server assembly under
    /// <see cref="ResourceName"/> so it is always deployed with the code it orders. It is the same text the order
    /// snapshot tests render: a line for each script name in column 0 (an optional <c>  # comment</c> may follow it),
    /// then one indented <c>action</c> or <c>conditional</c> line per handler holding
    /// <c>Namespace.Type.Method</c> (see <see cref="HandlerName"/>), in run order. Blank lines, lines starting with
    /// <c>#</c> and <c>== section ==</c> headings are ignored.
    ///
    /// A handler that is not listed runs after the listed handlers of its script, in discovery order, and a listed
    /// handler that no longer exists is ignored; <see cref="ScriptRegistry"/> logs both at boot.
    /// </summary>
    public sealed class ScriptHandlerManifest
    {
        /// <summary>The name of the embedded resource that holds <c>Core/ScriptHandlerOrder.txt</c>.</summary>
        public const string ResourceName = "ScriptHandlerOrder.txt";

        private const string ActionKind = "action";
        private const string ConditionalKind = "conditional";
        private const string NoHandlersLine = "(no handlers)";

        private static readonly char[] Whitespace = { ' ', '\t' };

        private readonly Dictionary<string, List<string>> _actions;
        private readonly Dictionary<string, List<string>> _conditionals;

        private ScriptHandlerManifest(
            Dictionary<string, List<string>> actions,
            Dictionary<string, List<string>> conditionals)
        {
            _actions = actions;
            _conditionals = conditionals;
        }

        /// <summary>
        /// A manifest that lists nothing, so every handler is unlisted and runs in discovery order. The server never
        /// uses it; it exists so a tool or test can ask for the plain discovery order explicitly.
        /// </summary>
        public static ScriptHandlerManifest Empty =>
            new(new Dictionary<string, List<string>>(StringComparer.Ordinal),
                new Dictionary<string, List<string>>(StringComparer.Ordinal));

        /// <summary>
        /// Every script the manifest lists at least one handler for (scripts with action handlers first, then the
        /// scripts that have only conditional handlers).
        /// </summary>
        public IReadOnlyList<string> ScriptNames =>
            _actions.Keys.Concat(_conditionals.Keys).Distinct(StringComparer.Ordinal).ToArray();

        /// <summary>
        /// Reads the manifest embedded in <paramref name="assembly"/>. Throws when the resource is missing,
        /// cannot be read, is malformed or lists no handler at all: a server must never start with an order it
        /// silently made up.
        /// </summary>
        public static ScriptHandlerManifest LoadEmbedded(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            string text;
            try
            {
                using var stream = assembly.GetManifestResourceStream(ResourceName);
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        $"The script handler order manifest '{ResourceName}' is not embedded in {assemblyName}. " +
                        "It is Core/ScriptHandlerOrder.txt in the SWLOR.Game.Server project (an EmbeddedResource); " +
                        "the server cannot decide the order its script handlers run in without it.");
                }

                using var reader = new StreamReader(stream);
                text = reader.ReadToEnd();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"The script handler order manifest '{ResourceName}' in {assemblyName} could not be read: {ex.Message}", ex);
            }

            return Parse(text, $"{assemblyName}!{ResourceName}");
        }

        /// <summary>
        /// Parses manifest text. Throws <see cref="InvalidDataException"/>, naming <paramref name="source"/> and the
        /// line, for anything that is not a comment, a section heading, a script name or a handler line, and when
        /// the text lists no handler at all.
        /// </summary>
        public static ScriptHandlerManifest Parse(string text, string source)
        {
            var actions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var conditionals = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var scripts = new HashSet<string>(StringComparer.Ordinal);
            string script = null;
            var entries = 0;

            var lines = text.Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].TrimEnd('\r');
                var number = index + 1;
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed[0] == '#' || line.StartsWith("== ", StringComparison.Ordinal))
                    continue;

                if (line[0] != ' ' && line[0] != '\t')
                {
                    var comment = line.IndexOf("  #", StringComparison.Ordinal);
                    var name = (comment >= 0 ? line.Substring(0, comment) : line).Trim();
                    if (name.Length == 0 || name.IndexOfAny(Whitespace) >= 0)
                        throw Malformed(source, number, $"'{trimmed}' is not a script name");
                    if (!scripts.Add(name))
                        throw Malformed(source, number, $"script '{name}' is listed more than once");

                    script = name;
                    continue;
                }

                if (trimmed == NoHandlersLine)
                    continue;

                if (script == null)
                    throw Malformed(source, number, "a handler line comes before any script name");

                var split = trimmed.IndexOfAny(Whitespace);
                var kind = split < 0 ? trimmed : trimmed.Substring(0, split);
                var handler = split < 0 ? string.Empty : trimmed.Substring(split).Trim();
                if (handler.Length == 0 || handler.IndexOfAny(Whitespace) >= 0)
                    throw Malformed(source, number, $"'{trimmed}' is not '<kind> <Namespace.Type.Method>'");

                Dictionary<string, List<string>> target;
                switch (kind)
                {
                    case ActionKind:
                        target = actions;
                        break;
                    case ConditionalKind:
                        target = conditionals;
                        break;
                    default:
                        throw Malformed(source, number, $"'{kind}' is not '{ActionKind}' or '{ConditionalKind}'");
                }

                if (!target.TryGetValue(script, out var list))
                {
                    list = new List<string>();
                    target[script] = list;
                }

                list.Add(handler);
                entries++;
            }

            if (entries == 0)
                throw new InvalidDataException($"The script handler order manifest {source} lists no handlers.");

            return new ScriptHandlerManifest(actions, conditionals);
        }

        /// <summary>
        /// The name a manifest line uses for a handler: <c>Namespace.Type.Method</c>.
        /// </summary>
        public static string HandlerName(MethodInfo method)
        {
            return method.DeclaringType?.FullName + "." + method.Name;
        }

        /// <summary>
        /// The handler names listed for a script and kind, in run order. Empty when the script has none.
        /// </summary>
        public IReadOnlyList<string> GetHandlers(string script, bool conditional)
        {
            var source = conditional ? _conditionals : _actions;
            return source.TryGetValue(script, out var list) ? list : Array.Empty<string>();
        }

        /// <summary>
        /// Decides the run order for one script and kind.
        /// </summary>
        /// <param name="script">The script name.</param>
        /// <param name="conditional">True for the script's bool handlers, false for its void handlers.</param>
        /// <param name="discoveredNames">
        /// <see cref="HandlerName"/> of each handler the registry found for the script and kind, in discovery order.
        /// </param>
        /// <returns>
        /// <c>Order</c>: indexes into <paramref name="discoveredNames"/> in the order the handlers must run (the
        /// listed ones by manifest position, then the unlisted ones in discovery order). <c>Unlisted</c>: the indexes
        /// of the handlers the manifest does not list. <c>Stale</c>: the manifest lines for which no handler was found.
        /// </returns>
        /// <remarks>
        /// A name the manifest lists twice (a method with the attribute twice for the same script, or two overloads)
        /// is matched to discovered handlers one by one, first listing to first discovered.
        /// </remarks>
        public (IReadOnlyList<int> Order, IReadOnlyList<int> Unlisted, IReadOnlyList<string> Stale) Arrange(
            string script,
            bool conditional,
            IReadOnlyList<string> discoveredNames)
        {
            var listed = GetHandlers(script, conditional);
            var positions = new Dictionary<string, Queue<int>>(StringComparer.Ordinal);
            for (var position = 0; position < listed.Count; position++)
            {
                if (!positions.TryGetValue(listed[position], out var queue))
                {
                    queue = new Queue<int>();
                    positions[listed[position]] = queue;
                }

                queue.Enqueue(position);
            }

            var matched = new List<(int Position, int Index)>();
            var matchedPositions = new bool[listed.Count];
            var unlisted = new List<int>();
            for (var index = 0; index < discoveredNames.Count; index++)
            {
                if (positions.TryGetValue(discoveredNames[index], out var queue) && queue.Count > 0)
                {
                    var position = queue.Dequeue();
                    matchedPositions[position] = true;
                    matched.Add((position, index));
                }
                else
                {
                    unlisted.Add(index);
                }
            }

            var order = matched.OrderBy(m => m.Position).Select(m => m.Index).Concat(unlisted).ToArray();
            var stale = Enumerable.Range(0, listed.Count).Where(p => !matchedPositions[p]).Select(p => listed[p]).ToArray();
            return (order, unlisted, stale);
        }

        private static InvalidDataException Malformed(string source, int line, string problem)
        {
            return new InvalidDataException($"The script handler order manifest {source} is malformed at line {line}: {problem}.");
        }
    }
}
