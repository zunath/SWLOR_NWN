namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// An nwn_gff JSON document (one module resource file) with enough lexical detail retained
    /// to serialize byte-identically when unmodified.
    /// </summary>
    public sealed class JsonGffDocument
    {
        /// <summary>The root "__data_type" value, including its trailing space (e.g. "ARE ").</summary>
        public string DataType { get; set; }

        public JsonGffStruct Root { get; private set; }

        /// <summary>True when the file uses CRLF line endings (the working-tree norm on Windows).</summary>
        public bool UsesCrLf { get; set; }

        /// <summary>True when the file ends with a newline after the closing brace.</summary>
        public bool HasTrailingNewline { get; set; }

        /// <summary>
        /// True when the trailing newline itself is CRLF. Tracked separately from <see cref="UsesCrLf"/>
        /// because the module corpus mixes the two: the unpack pipeline writes an LF body but
        /// terminates the file with CRLF, so a document's last line ending is not implied by its body.
        /// </summary>
        public bool TrailingNewlineUsesCrLf { get; set; }

        public JsonGffDocument(string dataType, JsonGffStruct root)
        {
            DataType = dataType;
            Root = root;
            UsesCrLf = true;
            HasTrailingNewline = true;
            TrailingNewlineUsesCrLf = true;
        }

        public static JsonGffDocument Load(string path)
        {
            return Parse(File.ReadAllBytes(path));
        }

        public static JsonGffDocument Parse(byte[] content)
        {
            return GffJsonReader.Read(content);
        }

        public byte[] ToBytes()
        {
            return GffJsonWriter.Write(this);
        }

        /// <summary>
        /// Replaces this document's parsed state while preserving the document object itself.
        /// Editor field contexts retain a reference to this object, so an external-change reload
        /// can refresh every field against the new root without rebuilding the whole editor tab.
        /// </summary>
        public void ReplaceWith(JsonGffDocument replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            DataType = replacement.DataType;
            Root.ReplaceParsedWith(replacement.Root);
            UsesCrLf = replacement.UsesCrLf;
            HasTrailingNewline = replacement.HasTrailingNewline;
            TrailingNewlineUsesCrLf = replacement.TrailingNewlineUsesCrLf;
        }
    }
}
