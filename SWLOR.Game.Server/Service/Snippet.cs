using System;
using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Service
{
    public static class Snippet
    {
        private static readonly Dictionary<string, SnippetDetail> _appearsWhenCommands = new Dictionary<string, SnippetDetail>();
        private static readonly Dictionary<string, SnippetDetail> _actionsTakenCommands = new Dictionary<string, SnippetDetail>();

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ISnippetListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (ISnippetListDefinition)Activator.CreateInstance(type);
                var snippets = instance.BuildSnippets();

                foreach (var (key, snippet) in snippets)
                {
                    if (snippet.ConditionAction != null)
                    {
                        _appearsWhenCommands.Add(key, snippet);
                    }

                    if (snippet.ActionsTakenAction != null)
                    {
                        _actionsTakenCommands.Add(key, snippet);
                    }

                }
            }

            Console.WriteLine($"Loaded {_actionsTakenCommands.Count} action snippets.");
            Console.WriteLine($"Loaded {_appearsWhenCommands.Count} condition snippets.");
        }
        /// <summary>
        /// When a conversation node with this script assigned in the "Appears When" event is run,
        /// check for any conversation conditions and process them.
        /// </summary>
        /// <returns></returns>
        [ScriptHandler(ScriptName.OnDialogAppear)]
        [ScriptHandler(ScriptName.OnDialogAppears)]
        [ScriptHandler(ScriptName.OnDialogCondition)]
        [ScriptHandler(ScriptName.OnDialogConditions)]
        public static bool ConversationAppearsWhen()
        {
            var player = GetPCSpeaker();
            return ProcessConditions(player);
        }

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        [ScriptHandler(ScriptName.OnDialogAction)]
        [ScriptHandler(ScriptName.OnDialogActions)]
        public static void ConversationAction()
        {
            var player = GetPCSpeaker();
            ProcessActions(player);
        }

        /// <summary>
        /// Handles processing condition commands.
        /// If any of the conditions fail, false will be returned.
        /// </summary>
        /// <param name="player">The player running the conditions.</param>
        /// <returns>true if all commands passed successfully, false otherwise</returns>
        private static bool ProcessConditions(uint player)
        {
            foreach (var condition in _appearsWhenCommands)
            {
                var notConditionEnabled = false;

                // Check for "not" condition first.
                if (UtilPlugin.GetScriptParamIsSet("!" + condition.Key))
                {
                    notConditionEnabled = true;
                }
                // If we can't find either condition, exit.
                else if (!UtilPlugin.GetScriptParamIsSet(condition.Key)) continue;

                var conditionKey = notConditionEnabled ? "!" + condition.Key : condition.Key;
                var param = GetScriptParam(conditionKey);
                var args = param.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                var snippetName = condition.Key;

                // The first command that fails will result in failure.
                var commandResult = _appearsWhenCommands[snippetName].ConditionAction(player, args.ToArray());
                
                // "Not" conditions check for the opposite condition.
                if (notConditionEnabled && commandResult)
                    return false;

                // Normal conditions
                if (!notConditionEnabled && !commandResult) return false;
            }

            return true;
        }

        /// <summary>
        /// Handles processing action commands.
        /// </summary>
        /// <param name="player">The player to run the commands against</param>
        private static void ProcessActions(uint player)
        {
            foreach (var action in _actionsTakenCommands)
            {
                if (!UtilPlugin.GetScriptParamIsSet(action.Key)) continue;

                var param = GetScriptParam(action.Key);
                var args = param.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                var commandText = action.Key;

                _actionsTakenCommands[commandText].ActionsTakenAction(player, args.ToArray());
            }
        }
    }
}
