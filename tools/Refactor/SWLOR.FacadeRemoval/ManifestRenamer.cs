namespace SWLOR.FacadeRemoval;

/// <summary>
/// Renames, in place, the lines of the handler order manifest (<c>SWLOR.Game.Server/Core/ScriptHandlerOrder.txt</c>) that
/// name a handler of a removed facade, to the instance method that carries the handler now, as
/// <c>tools/Refactor/HandlerRenames.txt</c> lists it (<c>&lt;master name&gt; &lt;branch name&gt;</c>). A line keeps its position,
/// so the run order does not change. A handler the branch split into several (P4-8, <c>&lt;master name&gt; &lt;branch name&gt;
/// &lt;branch name&gt; ...</c>) becomes one line per branch name, in that order, where its line was. A line that still names a
/// removed facade's handler without a rename is an error.
/// </summary>
internal static class ManifestRenamer
{
    /// <summary>
    /// The renames file: master name to its branch names (one, or several in run order for a split handler). Blank lines
    /// and <c>#</c> comments are skipped.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadRenames(string text, string source)
    {
        var renames = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var number = 0;
        foreach (var raw in text.Split('\n'))
        {
            number++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var comment = line.IndexOf(" #", StringComparison.Ordinal);
            if (comment >= 0)
                line = line[..comment].TrimEnd();

            var names = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length < 2)
                throw new FacadeRemovalException($"{source} line {number} is not '<master handler name> <branch handler name>' (or several branch names for a split handler): '{line}'.");
            var branchNames = names[1..];
            if (branchNames.Distinct(StringComparer.Ordinal).Count() != branchNames.Length)
                throw new FacadeRemovalException($"{source} line {number}: a branch handler is listed twice on the line.");
            if (!renames.TryAdd(names[0], branchNames))
                throw new FacadeRemovalException($"{source} line {number}: the master handler {names[0]} is listed twice.");
        }

        return renames;
    }

    /// <returns>The new manifest text and the number of lines renamed.</returns>
    public static (string Text, int Renamed) Apply(string manifest, IReadOnlyDictionary<string, IReadOnlyList<string>> renames, IReadOnlyList<RemovedFacade> facades)
    {
        var lines = manifest.Split('\n');
        var renamed = 0;
        var missing = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var content = line.TrimEnd('\r');
            var trimmed = content.TrimStart();
            if (content.Length == trimmed.Length || !(trimmed.StartsWith("action ", StringComparison.Ordinal) || trimmed.StartsWith("conditional ", StringComparison.Ordinal)))
                continue;

            var nameStart = content.Length - trimmed.Length + trimmed.IndexOf(' ');
            while (nameStart < content.Length && content[nameStart] == ' ')
                nameStart++;
            var nameEnd = content.IndexOf(' ', nameStart);
            if (nameEnd < 0)
                nameEnd = content.Length;
            var name = content[nameStart..nameEnd];

            if (!facades.Any(facade => name.StartsWith(facade.FullName + ".", StringComparison.Ordinal) && name.IndexOf('.', facade.FullName.Length + 1) < 0))
                continue;

            if (!renames.TryGetValue(name, out var branchNames))
            {
                missing.Add($"line {index + 1}: {name}");
                continue;
            }

            var lineEnd = line.EndsWith('\r') ? "\r" : string.Empty;
            lines[index] = string.Join('\n', branchNames.Select(branchName => content[..nameStart] + branchName + content[nameEnd..] + lineEnd));
            renamed++;
        }

        if (missing.Count > 0)
        {
            throw new FacadeRemovalException(
                "The handler order manifest names handlers of removed facades that tools/Refactor/HandlerRenames.txt does not rename. " +
                "Move each handler to the instance method of the same name and add '<master name> <branch name>':" +
                Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", missing));
        }

        return (string.Join('\n', lines), renamed);
    }
}
