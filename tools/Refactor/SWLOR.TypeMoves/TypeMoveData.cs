using System.Text.Json;
using System.Text.RegularExpressions;

namespace SWLOR.TypeMoves;

/// <summary>
/// <c>tools/Refactor/TypeMoves.json</c>: the namespace the types leave, each type with the namespace it joins (and its file
/// when another type's file declares it), and the repository paths whose text the tool never rewrites.
/// </summary>
internal sealed class TypeMoveData
{
    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$");
    private static readonly Regex DottedName = new(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$");

    private TypeMoveData(string from, IReadOnlyList<TypeMove> moves, IReadOnlyList<string> textExcludes)
    {
        From = from;
        Moves = moves;
        TextExcludes = textExcludes;
        ByName = moves.ToDictionary(move => move.Name, StringComparer.Ordinal);
    }

    /// <summary>The namespace the types leave (<c>SWLOR.Game.Server.Service</c>).</summary>
    public string From { get; }

    public IReadOnlyList<TypeMove> Moves { get; }

    public IReadOnlyDictionary<string, TypeMove> ByName { get; }

    /// <summary>Patterns on repository paths (<c>*</c> within a segment, <c>**</c> across segments).</summary>
    public IReadOnlyList<string> TextExcludes { get; }

    /// <summary>The files to move, each with the moves it holds (a file can declare more than one type).</summary>
    public IEnumerable<IGrouping<string, TypeMove>> Files => Moves.GroupBy(move => move.File, StringComparer.Ordinal);

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
            var from = Text(root, "from", source);
            if (!DottedName.IsMatch(from) || !from.StartsWith(TypeMove.ServerRoot + ".", StringComparison.Ordinal))
                throw new TypeMoveException($"{source}: \"from\" must be a namespace below {TypeMove.ServerRoot}, not '{from}'.");

            var parent = TypeMove.Parent(from);
            var moves = new List<TypeMove>();
            if (!root.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
                throw new TypeMoveException($"{source} has no \"types\" list.");

            foreach (var entry in types.EnumerateArray())
            {
                var name = Text(entry, "type", source);
                var to = Text(entry, "to", source);
                var file = entry.TryGetProperty("file", out var fileElement) ? fileElement.GetString() ?? string.Empty : name + ".cs";
                if (!Identifier.IsMatch(name))
                    throw new TypeMoveException($"{source}: '{name}' is not a type name.");
                if (!DottedName.IsMatch(to) || !to.StartsWith(parent + ".", StringComparison.Ordinal) || to == from || to.StartsWith(from + ".", StringComparison.Ordinal))
                    throw new TypeMoveException($"{source}: {name} must move to a namespace below {parent} outside {from}, not '{to}'.");
                if (file.Length == 0 || file.Contains('/') || file.Contains('\\') || !file.EndsWith(".cs", StringComparison.Ordinal))
                    throw new TypeMoveException($"{source}: the file of {name} must be a plain .cs file name, not '{file}'.");

                moves.Add(new TypeMove(name, from, to, file));
            }

            var twice = moves.GroupBy(move => move.Name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            if (twice.Count > 0)
                throw new TypeMoveException($"{source} lists {string.Join(", ", twice)} more than once.");

            foreach (var file in moves.GroupBy(move => move.File, StringComparer.Ordinal))
            {
                if (file.Select(move => move.To).Distinct(StringComparer.Ordinal).Count() > 1)
                    throw new TypeMoveException($"{source}: the types of {file.Key} ({string.Join(", ", file.Select(move => move.Name))}) move to different namespaces; a file moves whole.");
            }

            var excludes = new List<string>();
            if (root.TryGetProperty("textExcludes", out var list) && list.ValueKind == JsonValueKind.Array)
                excludes.AddRange(list.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(item => item.Length > 0));

            return new TypeMoveData(from, moves, excludes);
        }
    }

    /// <summary>True when the repository path (forward slashes) matches one of <see cref="TextExcludes"/>.</summary>
    public bool IsExcluded(string relativePath) => TextExcludes.Any(pattern => PathPattern.Matches(pattern, relativePath));

    private static string Text(JsonElement element, string property, string source)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new TypeMoveException($"{source}: an entry has no \"{property}\".");

        return value.GetString()!;
    }
}
