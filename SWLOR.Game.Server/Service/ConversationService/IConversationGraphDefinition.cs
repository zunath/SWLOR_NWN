namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>
    /// Supplies a conversation graph to the runtime. JSON-backed definitions can register the
    /// same graph type during the migration phase without changing the NUI window.
    /// </summary>
    public interface IConversationGraphDefinition
    {
        ConversationGraph BuildConversation();
    }
}
