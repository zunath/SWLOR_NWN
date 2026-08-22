using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed view over an .itp (palette tree) nwn_gff JSON document: a recursive "MAIN" list of
    /// category/leaf nodes.
    /// </summary>
    public sealed class ItpDocument : GffDocumentBase
    {
        public ItpDocument(JsonGffDocument document) : base(document)
        {
        }

        public static ItpDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static ItpDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        /// <summary>The palette's top-level category nodes ("MAIN").</summary>
        public IReadOnlyList<PaletteNode> Nodes =>
            Root.GetListOrEmpty("MAIN").Select(s => new PaletteNode(s)).ToList();

        /// <summary>Whether any leaf in the recursive palette tree references this blueprint.</summary>
        public bool ContainsResRef(string resRef) =>
            Nodes.Any(node => ContainsResRef(node, resRef));

        private static bool ContainsResRef(PaletteNode node, string resRef) =>
            string.Equals(node.ResRef, resRef, StringComparison.OrdinalIgnoreCase) ||
            node.Children.Any(child => ContainsResRef(child, resRef));
    }

    /// <summary>
    /// One node of a palette tree: either a category (typically NAME or STRREF plus a nested
    /// "LIST" of children) or a leaf blueprint reference (typically RESREF, and for creatures
    /// also NAME/FACTION/CR). Verified against the corpus: CC and DELETE_ME members named in the
    /// original brief were not observed in any .itp file in this repository's Module directory,
    /// so they are exposed defensively (null when absent) rather than assumed present.
    /// </summary>
    public sealed class PaletteNode
    {
        private readonly JsonGffStruct _struct;

        internal PaletteNode(JsonGffStruct target)
        {
            _struct = target;
        }

        /// <summary>The underlying struct, for members this view does not name.</summary>
        public JsonGffStruct Struct => _struct;

        public int? Id => _struct.GetIntOrNull("ID");

        public uint? StrRef => _struct.GetUIntOrNull("STRREF");

        public string? Name => _struct.GetStringOrNull("NAME");

        public string? ResRef => _struct.GetStringOrNull("RESREF");

        public string? Faction => _struct.GetStringOrNull("FACTION");

        public float? ChallengeRating => _struct.GetSingleOrNull("CR");

        /// <summary>Not observed in the corpus; present for compatibility with toolset-authored files.</summary>
        public string? Cc => _struct.GetStringOrNull("CC");

        /// <summary>Not observed in the corpus; present for compatibility with toolset-authored files.</summary>
        public bool? DeleteMe => _struct.GetIntOrNull("DELETE_ME") is { } value ? value != 0 : null;

        /// <summary>This node's children ("LIST"), empty for leaf nodes.</summary>
        public IReadOnlyList<PaletteNode> Children =>
            _struct.GetListOrEmpty("LIST").Select(s => new PaletteNode(s)).ToList();
    }
}
