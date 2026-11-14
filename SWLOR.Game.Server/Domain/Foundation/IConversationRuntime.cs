namespace SWLOR.Game.Server.Domain.Foundation
{
    public interface IConversationRuntime
    {
        bool EvaluateCondition(ConversationContext context, ConversationCondition condition);
        bool ExecuteAction(ConversationContext context, ConversationAction action);
        string ResolveText(ConversationContext context, string text);
    }
}
