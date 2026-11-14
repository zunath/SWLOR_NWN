using System.Text.Json;
using System.Text.RegularExpressions;

namespace SWLOR.TypeMoves;

/// <summary>
/// A data file of the tool (<c>tools/Refactor/TypeMoves.json</c>, <c>tools/Refactor/DomainOrderTypeMoves.json</c>): each type
/// with the namespace it leaves and the namespace it joins (and its file when another type's file declares it), the paths
/// whose text the tool never rewrites, and what happens to a using of a namespace a type leaves.
/// </summary>
/// <remarks>
/// <c>from</c> at the top is the namespace of every entry that names none of its own; an entry's own <c>from</c> wins. A
/// target is any namespace of the server outside the source (<c>Feature.*</c> to <c>Domain.*</c>, <c>Domain.Foundation</c> to
/// <c>Core</c>). <c>removeUnusedSourceUsings</c> (default true) drops a using of a source namespace from a file once nothing
/// binds through it; false keeps it (the smallest edit) unless the namespace is left without a type.
/// </remarks>
internal sealed class TypeMoveData
{
    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$");
    private static readonly Regex DottedName = new(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$");

    private TypeMoveData(string? from, IReadOnlyList<TypeMove> moves, IReadOnlyList<string> textExcludes, bool removeUnusedSourceUsings)
    {
        From = from;
        Moves = moves;
        TextExcludes = textExcludes;
        RemoveUnusedSourceUsings = removeUnusedSourceUsings;
        ByName = moves.ToDictionary(move => move.Name, StringComparer.Ordinal);
        ByOldFullName = moves.ToDictionary(move => move.OldFullName, StringComparer.Ordinal);
        Sources = moves.Select(move => move.From).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The namespace of every entry without its own <c>from</c> (<c>SWLOR.Game.Server.Service</c>), or null.</summary>
    public string? From { get; }

    public IReadOnlyList<TypeMove> Moves { get; }

    public IReadOnlyDictionary<string, TypeMove> ByName { get; }

    /// <summary>Each move by the type's full name before the move.</summary>
    public IReadOnlyDictionary<string, TypeMove> ByOldFullName { get; }

    /// <summary>The namespaces the types leave.</summary>
    public IReadOnlySet<string> Sources { get; }

    /// <summary>True: a using of a source namespace goes where nothing binds through it any more.</summary>
    public bool RemoveUnusedSourceUsings { get; }

    /// <summary>Patterns on repository paths (<c>*</c> within a segment, <c>**</c> across segments).</summary>
    public IReadOnlyList<string> TextExcludes { get; }

    /// <summary>The files to move (by their path before the move), each with the moves it holds (a file can declare more than one type).</summary>
    public IEnumerable<IGrouping<string, TypeMove>> Files => Moves.GroupBy(move => move.SourcePath, StringComparer.Ordinal);

    public static TypeMoveData Load(string path)
    {
        if (!File.Exists(path))
            throw new TypeMoveException($"The data file {path} does not exist.");

        return Parse(File.ReadAllText(path), path);
    }

    public static TypeMoveData Parse(string json, string source)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException exception)
        {
            throw new TypeMoveException($"{source} is not valid JSON: {exception.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            var defaultFrom = root.TryGetProperty("from", out _) ? Namespace(root, source) : null;
            if (!root.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
                throw new TypeMoveException($"{source} has no \"types\" list.");
            if (defaultFrom == null && types.GetArrayLength() == 0)
                throw new TypeMoveException($"{source}: \"from\" is missing or empty.");

            var moves = new List<TypeMove>();
            foreach (var entry in types.EnumerateArray())
            {
                var name = Text(entry, "type", source);
                var from = entry.TryGetProperty("from", out _) ? Namespace(entry, source) : defaultFrom ?? Text(entry, "from", source);
                var to = Text(entry, "to", source);
                var file = entry.TryGetProperty("file", out var fileElement) ? fileElement.GetString() ?? string.Empty : name + ".cs";
                if (!Identifier.IsMatch(name))
                    throw new TypeMoveException($"{source}: '{name}' is not a type name.");
                if (!DottedName.IsMatch(to) || !to.StartsWith(TypeMove.ServerRoot + ".", StringComparison.Ordinal) || to == from || to.StartsWith(from + ".", StringComparison.Ordinal))
                    throw new TypeMoveException($"{source}: {name} must move to a namespace below {TypeMove.ServerRoot} outside {from}, not '{to}'.");
                if (file.Length == 0 || file.Contains('/') || file.Contains('\\') || !file.EndsWith(".cs", StringComparison.Ordinal))
                    throw new TypeMoveException($"{source}: the file of {name} must be a plain .cs file name, not '{file}'.");

                moves.Add(new TypeMove(name, from, to, file));
            }

            var twice = moves.GroupBy(move => move.Name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            if (twice.Count > 0)
                throw new TypeMoveException($"{source} lists {string.Join(", ", twice)} more than once.");

            foreach (var file in moves.GroupBy(move => move.SourcePath, StringComparer.Ordinal))
            {
                if (file.Select(move => move.To).Distinct(StringComparer.Ordinal).Count() > 1)
                    throw new TypeMoveException($"{source}: the types of {file.First().File} ({string.Join(", ", file.Select(move => move.Name))}) move to different namespaces; a file moves whole.");
            }

            var collisions = moves.GroupBy(move => move.TargetPath, StringComparer.Ordinal)
                .Where(target => target.Select(move => move.SourcePath).Distinct(StringComparer.Ordinal).Count() > 1)
                .Select(target => target.Key)
                .ToList();
            if (collisions.Count > 0)
                throw new TypeMoveException($"{source}: two files would move to {string.Join(", ", collisions)}.");

            var excludes = new List<string>();
            if (root.TryGetProperty("textExcludes", out var list) && list.ValueKind == JsonValueKind.Array)
                excludes.AddRange(list.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(item => item.Length > 0));

            var removeUnused = true;
            if (root.TryGetProperty("removeUnusedSourceUsings", out var flag))
            {
                if (flag.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    throw new TypeMoveException($"{source}: \"removeUnusedSourceUsings\" must be true or false.");
                removeUnused = flag.GetBoolean();
            }

            return new TypeMoveData(defaultFrom, moves, excludes, removeUnused);
        }
    }

    /// <summary>
    /// The same data with only the given moves: the ones a tree holds. A listed type whose file a tree has at neither path
    /// (master without it) is no move there, even when a type of that name is declared in another file of the old namespace.
    /// </summary>
    public TypeMoveData Only(IEnumerable<TypeMove> moves)
    {
        var kept = moves.ToHashSet();
        return new TypeMoveData(From, Moves.Where(kept.Contains).ToList(), TextExcludes, RemoveUnusedSourceUsings);
    }

    /// <summary>True when the repository path (forward slashes) matches one of <see cref="TextExcludes"/>.</summary>
    public bool IsExcluded(string relativePath) => TextExcludes.Any(pattern => PathPattern.Matches(pattern, relativePath));

    /// <summary>A <c>from</c> that names a namespace below the server's root.</summary>
    private static string Namespace(JsonElement element, string source)
    {
        var from = Text(element, "from", source);
        if (!DottedName.IsMatch(from) || !from.StartsWith(TypeMove.ServerRoot + ".", StringComparison.Ordinal))
            throw new TypeMoveException($"{source}: \"from\" must be a namespace below {TypeMove.ServerRoot}, not '{from}'.");

        return from;
    }

    private static string Text(JsonElement element, string property, string source)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new TypeMoveException($"{source}: \"{property}\" is missing or empty.");

        return value.GetString()!;
    }
}
