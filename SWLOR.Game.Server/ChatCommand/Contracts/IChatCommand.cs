using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand.Contracts
{
    public interface IChatCommand
    {
        /// <summary>
        /// The action to perform for the chat command.
        /// </summary>
        /// <param name="user">The user which typed the command.</param>
        /// <param name="args">Arguments passed in by player.</param>
        void DoAction(NWPlayer user, params string[] args);
    }
}
