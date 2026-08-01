namespace SWLOR.Game.Server.Service.ConversationService
{
    public interface IConversationRuntime
    {
        bool EvaluateCondition(ConversationContext context, ConversationCondition condition);
        void ExecuteAction(ConversationContext context, ConversationAction action);
        string ResolveText(ConversationContext context, string text);
    }
}
