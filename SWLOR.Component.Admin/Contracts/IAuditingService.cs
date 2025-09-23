namespace SWLOR.Component.Admin.Contracts
{
    public interface IAuditingService
    {
        /// <summary>
        /// Writes an audit log when a player connects to the server.
        /// </summary>
        void AuditClientConnection();

        /// <summary>
        /// Writes an audit log when a player disconnects from the server.
        /// </summary>
        void AuditClientDisconnection();

        /// <summary>
        /// Writes an audit log when a player sends a chat message.
        /// </summary>
        void AuditChatMessages();
    }
}
