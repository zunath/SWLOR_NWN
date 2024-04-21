namespace SWLOR.Core.Service.SnippetService
{
    public interface ISnippetListDefinition
    {
        public Dictionary<string, SnippetDetail> BuildSnippets();
    }
}
