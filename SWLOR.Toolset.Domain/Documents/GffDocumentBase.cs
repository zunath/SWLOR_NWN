using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Shared base for typed document views over a <see cref="JsonGffDocument"/>. Subclasses
    /// expose named properties that read and write fields on <see cref="Root"/>; anything not
    /// exposed by name is still reachable through <see cref="Fields"/>, so unknown fields flow
    /// through untouched.
    /// </summary>
    public abstract class GffDocumentBase
    {
        protected GffDocumentBase(JsonGffDocument document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>The underlying generic document this view wraps.</summary>
        public JsonGffDocument Document { get; }

        /// <summary>The document's root struct, for typed accessors declared by subclasses.</summary>
        protected JsonGffStruct Root => Document.Root;

        /// <summary>Generic passthrough to the root struct for fields this view does not name.</summary>
        public JsonGffStruct Fields => Document.Root;

        public byte[] ToBytes()
        {
            return Document.ToBytes();
        }
    }
}
