using System;

namespace SWLOR.Game.Server.Service.LogService
{
    public enum LogGroup
    {
        [LogGroup("Attack", ServerEnvironment.Development)]
        Attack,
        [LogGroup("Connection", ServerEnvironment.All)]
        Connection,
        [LogGroup("Erroor", ServerEnvironment.All)]
        Error,
        [LogGroup("Chat", ServerEnvironment.All)]
        Chat,
        [LogGroup("DM", ServerEnvironment.All)]
        DM,
        [LogGroup("DMAuthorization", ServerEnvironment.All)]
        DMAuthorization,
        [LogGroup("Death", ServerEnvironment.All)]
        Death,
        [LogGroup("Server", ServerEnvironment.All)]
        Server,
        [LogGroup("PerkRefund", ServerEnvironment.All)]
        PerkRefund,
        [LogGroup("Property", ServerEnvironment.All)]
        Property,
        [LogGroup("PlayerMarket", ServerEnvironment.All)]
        PlayerMarket,
        [LogGroup("Space", ServerEnvironment.All)]
        Space
    }

    public class LogGroupAttribute : Attribute
    {
        public string LoggerName { get; set; }
        public ServerEnvironment Environment { get; set; }

        public LogGroupAttribute(string loggerName, ServerEnvironment environment)
        {
            LoggerName = loggerName;
            Environment = environment;
        }
    }
}