using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public static class AccountSnippetDefinition
    {
        /// <summary>
        /// Snippet which checks whether a player has completed the tutorial on any character.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="args">Arguments provided by the conversation builder.</param>
        /// <returns>true if player has completed the tutorial, false otherwise</returns>
        [Snippet("condition-has-completed-tutorial")]
        public static bool ConditionHasCompletedTutorial(uint player, string[] args)
        {
            var cdKey = GetPCPublicCDKey(player);
            var dbAccount = DB.Get<Account>(cdKey) ?? new Account();

            return dbAccount.HasCompletedTutorial;
        }
    }
}
