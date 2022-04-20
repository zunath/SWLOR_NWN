namespace SWLOR.Game.Server.Service.SnippetService
{
    public delegate bool SnippetConditionDelegate(uint player, string[] args);

    public delegate void SnippetActionDelegate(uint player, string[] args);
    public class SnippetDetail
    {
        public string Description { get; set; }
        public SnippetConditionDelegate ConditionAction { get; set; }
        public SnippetActionDelegate ActionsTakenAction { get; set; }

        public SnippetDetail()
        {
            Description = string.Empty;
        }
    }
}
