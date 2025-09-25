using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Shared.Domain.Dialog.Contracts
{
    public interface ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets();
    }
}
