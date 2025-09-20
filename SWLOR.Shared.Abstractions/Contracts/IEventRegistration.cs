namespace SWLOR.Shared.Abstractions.Contracts
{
    /// <summary>
    /// Interface for event registration services that handle module, area, player, and NWNX events.
    /// </summary>
    public interface IEventRegistration
    {
        /// <summary>
        /// Fires on the module PreLoad event. This event should be specified in the environment variables.
        /// This will hook all module/global events.
        /// </summary>
        void OnModulePreload();

        /// <summary>
        /// Executes the heartbeat event for all players.
        /// </summary>
        void ExecuteHeartbeatEvent();

        /// <summary>
        /// When a player enters the server, hook their event scripts.
        /// Also add them to a UI processor list.
        /// </summary>
        void EnterServer();

        /// <summary>
        /// A handful of NWNX functions require special calls to load persistence.
        /// When the module loads, run those methods here.
        /// </summary>
        void TriggerNWNXPersistence();
    }
}
