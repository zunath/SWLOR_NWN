using SWLOR.Shared.Dialog.Model;

namespace SWLOR.Shared.Dialog.Contracts
{
    public interface IDialogService
    {
        /// <summary>
        /// When the module is loaded, the assembly will be searched for conversations.
        /// These will be added to the cache for use at a later time.
        /// </summary>
        void RegisterConversations();

        void InitializeDialogs();

        /// <summary>
        /// Retrieves a conversation from the cache.
        /// </summary>
        /// <param name="key">The name of the conversation.</param>
        /// <returns>A conversation instance</returns>
        IConversation GetConversation(string key);

        /// <summary>
        /// Handles when a dialog is started.
        /// </summary>
        void Start();

        void NodeAction0();
        void NodeAction1();
        void NodeAction2();
        void NodeAction3();
        void NodeAction4();
        void NodeAction5();
        void NodeAction6();
        void NodeAction7();
        void NodeAction8();
        void NodeAction9();
        void NodeAction10();
        void NodeAction11();
        bool NodeAppears0();
        bool NodeAppears1();
        bool NodeAppears2();
        bool NodeAppears3();
        bool NodeAppears4();
        bool NodeAppears5();
        bool NodeAppears6();
        bool NodeAppears7();
        bool NodeAppears8();
        bool NodeAppears9();
        bool NodeAppears10();
        bool NodeAppears11();
        bool HeaderAppearsWhen();
        bool NextAppearsWhen();
        void NextAction();
        bool PreviousAppearsWhen();
        void PreviousAction();
        bool BackAppearsWhen();
        void BackAction();

        /// <summary>
        /// Fires when the "End Dialog" node is clicked.
        /// </summary>
        void End();

        /// <summary>
        /// When an object executes this script, the custom dialog specified on their local variables
        /// will be started.
        /// </summary>
        void StartConversationEvent();

        /// <summary>
        /// Begins a new conversation for a player.
        /// </summary>
        /// <param name="player">The player to start the conversation for.</param>
        /// <param name="talkTo">The creature to speak to.</param>
        /// <param name="class">The name of the conversation class.</param>
        void StartConversation(uint player, uint talkTo, string @class);

        /// <summary>
        /// Checks whether a player has an active dialog in cache.
        /// </summary>
        /// <param name="playerId">The player's unique Id.</param>
        /// <returns>true if the player has the dialog, false otherwise</returns>
        bool HasPlayerDialog(string playerId);

        /// <summary>
        /// Loads a specific player's cached dialog information.
        /// </summary>
        /// <param name="playerId">The player's unique Id</param>
        /// <returns>The cached player dialog object.</returns>
        PlayerDialog LoadPlayerDialog(string playerId);

        /// <summary>
        /// Removes a player's dialog from the cache.
        /// </summary>
        /// <param name="playerId">The player's unique Id.</param>
        void RemovePlayerDialog(string playerId);

        /// <summary>
        /// Loads a conversation for a player.
        /// </summary>
        /// <param name="player">The player to load the conversation for.</param>
        /// <param name="talkTo">The creature to talk to.</param>
        /// <param name="class">The conversation name to start.</param>
        /// <param name="dialogNumber">The dialog number the player's conversation is tied to.</param>
        void LoadConversation(uint player, uint talkTo, string @class, int dialogNumber);

        /// <summary>
        /// Ends a conversation and cleans up related cache data.
        /// </summary>
        /// <param name="player">The player to end the conversation for.</param>
        void EndConversation(uint player);
    }
}