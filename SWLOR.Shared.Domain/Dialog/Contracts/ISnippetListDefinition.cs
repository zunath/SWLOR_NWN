using SWLOR.Shared.Dialog.Model;

namespace SWLOR.Shared.Dialog.Contracts
{
    public interface ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets();
    }
}
