using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand.Contracts
{
    public interface IChatCommand
    {
        /// <summary>
        /// The action to perform for the chat command.
        /// </summary>
        /// <param name="user">The user which typed the command.</param>
        /// <param name="target">The target of a chat command. If command doesn't require a target, this will be an invalid game object.</param>
        /// <param name="targetLocation">The target location of a chat command. If command doesn't require a target, this will be an invalid location.</param>
        /// <param name="args">Arguments passed in by player.</param>
        void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args);

        /// <summary>
        /// If true, a player must use the Chat Command Targeter feat in order for the command to run.
        /// </summary>
        bool RequiresTarget { get; }

        /// <summary>
        /// If null or empty string, the user can use the chat command.
        /// If any other value, the user will be given the return value as an error message 
        /// </summary>
        /// <param name="user">The user which typed the command</param>
        /// <param name="args">Arguments passed in by player.</param>
        /// 
        /// 
        /// <returns></returns>
        string ValidateArguments(NWPlayer user, params string[] args);
    }
}
