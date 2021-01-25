using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class SnippetAttribute : Attribute
    {
        public string Name { get; }

        public SnippetAttribute(string name)
        {
            Name = name;
        }
    }

    public static class Snippet
    {
        private delegate bool AppearsWhenDelegate(uint player, string[] args);
        private delegate void ActionsTakenDelegate(uint player, string[] args);

        private static readonly Dictionary<string, AppearsWhenDelegate> _appearsWhenCommands = new Dictionary<string, AppearsWhenDelegate>();
        private static readonly Dictionary<string, ActionsTakenDelegate> _actionsTakenCommands = new Dictionary<string, ActionsTakenDelegate>();

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void RegisterSnippets()
        {
            var methods = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(SnippetAttribute), false).Length > 0)
                .ToArray();

            foreach (var mi in methods)
            {
                foreach (var attr in mi.GetCustomAttributes(typeof(SnippetAttribute), false))
                {
                    var name = ((SnippetAttribute)attr).Name.ToLower();
                    if (name.StartsWith("action"))
                    {
                        _actionsTakenCommands[name] = (ActionsTakenDelegate)mi.CreateDelegate(typeof(ActionsTakenDelegate));
                    }
                    else if (name.StartsWith("condition"))
                    {
                        _appearsWhenCommands[name] = (AppearsWhenDelegate)mi.CreateDelegate(typeof(AppearsWhenDelegate));
                    }
                }
            }
        }
        /// <summary>
        /// When a conversation node with this script assigned in the "Appears When" event is run,
        /// check for any conversation conditions and process them.
        /// </summary>
        /// <returns></returns>
        [NWNEventHandler("appear")]
        [NWNEventHandler("appears")]
        public static bool ConversationAppearsWhen()
        {
            var player = GetPCSpeaker();
            return ProcessConditions(player);
        }

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        [NWNEventHandler("action")]
        [NWNEventHandler("actions")]
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
                if (Util.GetScriptParamIsSet("!" + condition.Key))
                {
                    notConditionEnabled = true;
                }
                // If we can't find either condition, exit.
                else if (!Util.GetScriptParamIsSet(condition.Key)) continue;

                var conditionKey = notConditionEnabled ? "!" + condition.Key : condition.Key;
                var param = GetScriptParam(conditionKey);
                var args = param.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                var snippetName = condition.Key;

                // The first command that fails will result in failure.
                var commandResult = _appearsWhenCommands[snippetName](player, args.ToArray());
                
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
                if (!Util.GetScriptParamIsSet(action.Key)) continue;

                var param = GetScriptParam(action.Key);
                var args = param.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                var commandText = action.Key;

                _actionsTakenCommands[commandText](player, args.ToArray());
            }
        }
    }
}
