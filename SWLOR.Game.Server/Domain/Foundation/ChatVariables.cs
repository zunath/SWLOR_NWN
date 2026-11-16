namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>Local variable names the chat code and its lower-domain callers share.</summary>
    public static class ChatVariables
    {
        /// <summary>Set on an area whose Comms scope is the event area.</summary>
        public const string EventCommsAreaVariable = "COMMS_EVENT_AREA";

        /// <summary>Set on an object whose chat is left out of the chat audit.</summary>
        public const string SuppressChatAuditVariable = "SUPPRESS_CHAT_AUDIT";
    }
}
