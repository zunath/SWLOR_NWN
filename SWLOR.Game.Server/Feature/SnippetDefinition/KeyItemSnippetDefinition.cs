using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public static class KeyItemSnippetDefinition
    {
        /// <summary>
        /// Snippet which checks whether a player has all of the specified key items.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="args">Arguments provided by the conversation builder.</param>
        /// <returns>true if player has all of the specified key items, false otherwise</returns>
        [Snippet("condition-all-key-items")]
        public static bool ConditionAllKeyItems(uint player, string[] args)
        {
            if (args.Length <= 0)
            {
                const string Error = "'condition-has-key-items' requires a keyItemId argument.";
                SendMessageToPC(player, Error);
                Log.Write(LogGroup.Error, Error);
                return false;
            }

            foreach (var arg in args)
            {
                KeyItemType type;

                // Try searching by Id first.
                if (int.TryParse(arg, out var argId))
                {
                    type = KeyItem.GetKeyItemTypeById(argId);
                }
                // Couldn't parse an integer. Look by name.
                else
                {
                    type = KeyItem.GetKeyItemTypeByName(arg);
                }

                // Type is invalid, log an error and end.
                if (type == KeyItemType.Invalid)
                {
                    Log.Write(LogGroup.Error, $"{arg} is not a valid KeyItemType");
                    return false;
                }

                // Player doesn't have the specified key item.
                if (!KeyItem.HasKeyItem(player, type))
                {
                    return false;
                }

            }

            return true;
        }
    }
}
