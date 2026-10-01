using System.Text;
using Newtonsoft.Json;
using Nwn.Authoring.Documents;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Toolset.Editors;

internal sealed class ConversationGraphCodec : IDocumentCodec<ConversationGraph>
{
    public ConversationGraph Decode(ReadOnlyMemory<byte> bytes) =>
        JsonConvert.DeserializeObject<ConversationGraph>(Encoding.UTF8.GetString(bytes.Span))
        ?? throw new InvalidDataException("The conversation graph snapshot was empty.");

    public byte[] Encode(ConversationGraph document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(document, Formatting.Indented));
    }
}
