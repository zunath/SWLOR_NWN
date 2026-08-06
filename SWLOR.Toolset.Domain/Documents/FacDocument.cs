using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed view over the repute.fac (faction/reputation table) nwn_gff JSON document:
    /// the faction list ("FactionList": FactionGlobal/FactionName/FactionParentID entries) and
    /// the pairwise reputation list ("RepList": FactionID1/FactionID2/FactionRep entries).
    /// </summary>
    public sealed class FacDocument : GffDocumentBase
    {
        public FacDocument(JsonGffDocument document) : base(document)
        {
        }

        public static FacDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static FacDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public IReadOnlyList<JsonGffStruct> FactionList => Root.GetListOrEmpty("FactionList");

        public IReadOnlyList<JsonGffStruct> RepList => Root.GetListOrEmpty("RepList");
    }
}
