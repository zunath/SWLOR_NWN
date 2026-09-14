using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class Snippet
    {
        private static readonly Dictionary<string, SnippetDetail> _appearsWhenCommands = new Dictionary<string, SnippetDetail>();
        private static readonly Dictionary<string, SnippetDetail> _actionsTakenCommands = new Dictionary<string, SnippetDetail>();
        [ThreadStatic]
        private static uint _executionOwner;

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
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
                        Conversation.Runtime.RegisterCondition(
                            key,
                            (context, arguments) => EvaluateCondition(
                                context.Player,
                                key,
                                arguments,
                                context.Owner));
                    }

                    if (snippet.ActionsTakenAction != null)
                    {
                        _actionsTakenCommands.Add(key, snippet);
                        Conversation.Runtime.RegisterAction(
                            key,
                            (context, arguments) =>
                                ExecuteAction(
                                    context.Player,
                                    key,
                                    arguments,
                                    context.Owner));
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
        [NWNEventHandler(ScriptName.OnDialogAppear)]
        [NWNEventHandler(ScriptName.OnDialogAppears)]
        [NWNEventHandler(ScriptName.OnDialogCondition)]
        [NWNEventHandler(ScriptName.OnDialogConditions)]
        public static bool ConversationAppearsWhen()
        {
            var player = GetPCSpeaker();
            return ProcessConditions(player);
        }

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogAction)]
        [NWNEventHandler(ScriptName.OnDialogActions)]
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
                var commandResult = EvaluateCondition(player, snippetName, args);

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
                ExecuteAction(player, action.Key, args);
            }
        }

        /// <summary>
        /// Evaluates one registered snippet condition without relying on NWScript parameter state.
        /// This is the entry point used by NUI conversations.
        /// </summary>
        public static bool EvaluateCondition(
            uint player,
            string key,
            IReadOnlyList<string> arguments,
            uint owner = OBJECT_INVALID)
        {
            if (!_appearsWhenCommands.TryGetValue(key, out var snippet))
                throw new InvalidOperationException($"Conversation condition snippet '{key}' is not registered.");

            arguments ??= Array.Empty<string>();
            if (!HasUsableArguments(key, snippet, arguments.Count, player))
                return false;

            var previousOwner = _executionOwner;
            _executionOwner = GetIsObjectValid(owner) ? owner : OBJECT_INVALID;
            try
            {
                return snippet.ConditionAction(player, arguments.ToArray());
            }
            finally
            {
                _executionOwner = previousOwner;
            }
        }

        /// <summary>
        /// Executes one registered snippet action without relying on NWScript parameter state.
        /// </summary>
        public static bool ExecuteAction(
            uint player,
            string key,
            IReadOnlyList<string> arguments,
            uint owner = OBJECT_INVALID)
        {
            if (!_actionsTakenCommands.TryGetValue(key, out var snippet))
                throw new InvalidOperationException($"Conversation action snippet '{key}' is not registered.");

            arguments ??= Array.Empty<string>();
            if (!HasUsableArguments(key, snippet, arguments.Count, player))
                return false;

            var previousOwner = _executionOwner;
            _executionOwner = GetIsObjectValid(owner) ? owner : OBJECT_INVALID;
            bool succeeded;
            try
            {
                succeeded = snippet.ActionsTakenAction(player, arguments.ToArray());
            }
            finally
            {
                _executionOwner = previousOwner;
            }

            return succeeded;
        }

        /// <summary>
        /// The object that owns the active conversation. Snippet implementations use this instead
        /// of OBJECT_SELF so they behave identically from native DLG scripts and NUI events.
        /// </summary>
        public static uint GetExecutionOwner()
        {
            return GetIsObjectValid(_executionOwner) ? _executionOwner : OBJECT_SELF;
        }

        /// <summary>
        /// Checks that a snippet was given enough arguments to run, reporting the shortfall to the
        /// player and the log once, in one place, instead of each snippet phrasing it differently.
        /// </summary>
        /// <remarks>
        /// Only a shortfall is refused, never a surplus - see
        /// <see cref="SnippetDetail.HasEnoughArguments"/> for why. A snippet that declares no
        /// arguments is not checked at all: an empty declaration is indistinguishable from one that
        /// has simply not been written yet, and refusing to run it would break working content.
        /// </remarks>
        private static bool HasUsableArguments(string key, SnippetDetail snippet, int argumentCount, uint player)
        {
            if (snippet.Arguments.Count == 0 || snippet.HasEnoughArguments(argumentCount))
                return true;

            var error = $"'{key}' was given {argumentCount} argument(s) but needs at least "
                        + $"{snippet.MinimumArgumentCount}.";

            SendMessageToPC(player, error);
            Log.Write(LogGroup.Error, error);
            return false;
        }
    }
}
