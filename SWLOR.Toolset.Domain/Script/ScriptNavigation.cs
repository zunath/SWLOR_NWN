using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>Where a definition lives.</summary>
    /// <param name="ResRef">Script the definition is in, or null when it is in the current file.</param>
    /// <param name="Offset">Offset of the name, when known.</param>
    /// <param name="Name">The symbol.</param>
    /// <param name="IsEngineSymbol">True when it is declared by the engine header, not by module source.</param>
    public sealed record ScriptDefinition(string? ResRef, int Offset, string Name, bool IsEngineSymbol);

    /// <summary>One occurrence of a name in a file.</summary>
    public sealed record ScriptReference(int Offset, int Length, int Line);

    /// <summary>
    /// Go-to-definition, find-references and rename over a single file plus its include set.
    /// </summary>
    /// <remarks>
    /// Occurrences are found from the token stream rather than by text search, so a name inside a
    /// string literal or a comment is never mistaken for a use of the symbol. That distinction is
    /// what makes rename safe enough to offer at all: this module's legacy scripts are full of
    /// local-variable names that also appear as string keys (<c>GetLocalInt(oPC, "nCount")</c>), and
    /// a naive search-and-replace would corrupt them.
    /// </remarks>
    public static class ScriptNavigation
    {
        /// <summary>The identifier under the caret, or null when the caret is not on one.</summary>
        public static string? IdentifierAt(string source, int offset)
        {
            var token = TokenAt(source, offset);
            return token?.Kind == ScriptTokenKind.Identifier ? token.Value.ToText(source) : null;
        }

        /// <summary>The token containing <paramref name="offset"/>, or the one ending there.</summary>
        public static ScriptToken? TokenAt(string source, int offset)
        {
            foreach (var token in ScriptLexer.Tokenize(source))
            {
                if (offset >= token.Start && offset <= token.End && token.Length > 0)
                    return token;
            }

            return null;
        }

        /// <summary>
        /// Resolves the symbol under the caret. Prefers a definition in the current file, then the
        /// include set, then the engine header.
        /// </summary>
        public static ScriptDefinition? FindDefinition(
            string source,
            int offset,
            EngineSymbolDatabase engine,
            Func<string, string?>? readInclude = null)
        {
            var name = IdentifierAt(source, offset);
            if (name == null)
                return null;

            var outline = ScriptOutline.Build(source);

            var local = outline.Functions.FirstOrDefault(f => f.IsDefinition && f.Name == name)
                ?? outline.Functions.FirstOrDefault(f => f.Name == name);
            if (local != null)
                return new ScriptDefinition(null, local.Offset, name, false);

            if (readInclude != null)
            {
                // Direct includes only, in file order: chasing the full transitive set here would
                // mean reading the whole module to answer one F12.
                foreach (var include in outline.Includes)
                {
                    var text = readInclude(include);
                    if (text == null)
                        continue;

                    var fn = ScriptOutline.Build(text).Functions.FirstOrDefault(f => f.Name == name);
                    if (fn != null)
                        return new ScriptDefinition(include, fn.Offset, name, false);
                }
            }

            if (engine.FindFunction(name) != null || engine.FindConstant(name) != null)
                return new ScriptDefinition(null, -1, name, true);

            return null;
        }

        /// <summary>Every occurrence of <paramref name="name"/> as an identifier token.</summary>
        public static IReadOnlyList<ScriptReference> FindReferences(string source, string name)
        {
            var result = new List<ScriptReference>();

            foreach (var token in ScriptLexer.Tokenize(source))
            {
                if (token.Kind != ScriptTokenKind.Identifier)
                    continue;

                if (!token.Text(source).SequenceEqual(name))
                    continue;

                result.Add(new ScriptReference(token.Start, token.Length, ScriptOutline.LineOf(source, token.Start)));
            }

            return result;
        }

        /// <summary>
        /// Renames every identifier occurrence of <paramref name="oldName"/>. Comments and string
        /// literals are left alone, which is the whole point.
        /// </summary>
        public static string Rename(string source, string oldName, string newName)
        {
            var references = FindReferences(source, oldName);
            if (references.Count == 0)
                return source;

            var builder = new System.Text.StringBuilder(source.Length);
            var cursor = 0;

            foreach (var reference in references)
            {
                builder.Append(source, cursor, reference.Offset - cursor);
                builder.Append(newName);
                cursor = reference.Offset + reference.Length;
            }

            builder.Append(source, cursor, source.Length - cursor);
            return builder.ToString();
        }

        /// <summary>True when <paramref name="name"/> is a legal NWScript identifier.</summary>
        public static bool IsValidIdentifier(string name) =>
            name.Length > 0 &&
            (char.IsLetter(name[0]) || name[0] == '_') &&
            name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
