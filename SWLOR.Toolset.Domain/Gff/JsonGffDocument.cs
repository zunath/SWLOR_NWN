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

        public JsonGffStruct Root { get; }

        /// <summary>True when the file uses CRLF line endings (the working-tree norm on Windows).</summary>
        public bool UsesCrLf { get; set; }

        /// <summary>True when the file ends with a newline after the closing brace.</summary>
        public bool HasTrailingNewline { get; set; }

        public JsonGffDocument(string dataType, JsonGffStruct root)
        {
            DataType = dataType;
            Root = root;
            UsesCrLf = true;
            HasTrailingNewline = true;
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
    }
}
