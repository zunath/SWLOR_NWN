using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.LogService
{
    public enum LogGroup
    {
        [LogGroup("Attack", ServerEnvironmentType.Development | ServerEnvironmentType.Test)]
        Attack,
        [LogGroup("Connection", ServerEnvironmentType.All)]
        Connection,
        [LogGroup("Error", ServerEnvironmentType.All)]
        Error,
        [LogGroup("Chat", ServerEnvironmentType.All)]
        Chat,
        [LogGroup("DM", ServerEnvironmentType.All)]
        DM,
        [LogGroup("DMAuthorization", ServerEnvironmentType.All)]
        DMAuthorization,
        [LogGroup("Death", ServerEnvironmentType.All)]
        Death,
        [LogGroup("Server", ServerEnvironmentType.All)]
        Server,
        [LogGroup("PerkRefund", ServerEnvironmentType.All)]
        PerkRefund,
        [LogGroup("Property", ServerEnvironmentType.All)]
        Property,
        [LogGroup("PlayerMarket", ServerEnvironmentType.All)]
        PlayerMarket,
        [LogGroup("Space", ServerEnvironmentType.All)]
        Space,
        [LogGroup("StoreCleanup", ServerEnvironmentType.All)]
        StoreCleanup,
        [LogGroup("Migration", ServerEnvironmentType.All)]
        Migration,
        [LogGroup("Crafting", ServerEnvironmentType.All)]
        Crafting,
        [LogGroup("Incubation", ServerEnvironmentType.All)]
        Incubation
    }

    public class LogGroupAttribute : Attribute
    {
        public string LoggerName { get; set; }
        public ServerEnvironmentType Environment { get; set; }

        public LogGroupAttribute(string loggerName, ServerEnvironmentType environment)
        {
            LoggerName = loggerName;
            Environment = environment;
        }
    }
}