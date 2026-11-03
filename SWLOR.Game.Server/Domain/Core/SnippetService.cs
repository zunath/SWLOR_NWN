using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// The conversation snippets: the logic and state that used to be static in <c>Service.Snippet</c>, moved here
    /// unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    /// <remarks>
    /// The constructor does no work: the snippets are registered by <see cref="CacheData"/>, which runs in the
    /// before-phase of the module cache. The execution owner was a <c>[ThreadStatic]</c> field; it is a
    /// <see cref="ThreadLocal{T}"/> of the instance, so it is still one value per thread. The reflection search and
    /// the error log are <c>protected virtual</c> so a unit test can answer them; the bodies are the original code.
    /// </remarks>
    public class SnippetService : ISnippetService
    {
        private readonly IConversationService _conversation;
        private readonly Dictionary<string, SnippetDetail> _appearsWhenCommands = new Dictionary<string, SnippetDetail>();
        private readonly Dictionary<string, SnippetDetail> _actionsTakenCommands = new Dictionary<string, SnippetDetail>();
        private readonly ThreadLocal<uint> _executionOwner = new ThreadLocal<uint>();

        public SnippetService(IConversationService conversation)
        {
            _conversation = conversation;
        }

        public void CacheData()
        {
            foreach (var type in GetSnippetListDefinitionTypes())
            {
                var instance = (ISnippetListDefinition)Activator.CreateInstance(type);
                var snippets = instance.BuildSnippets();

                foreach (var (key, snippet) in snippets)
                {
                    if (snippet.ConditionAction != null)
                    {
                        _appearsWhenCommands.Add(key, snippet);
                        _conversation.Runtime.RegisterCondition(
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
                        _conversation.Runtime.RegisterAction(
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
        /// The types of every snippet list definition in the loaded assemblies. <c>protected virtual</c> so a unit
        /// test can answer with its own definitions; the default body is the original lazy search.
        /// </summary>
        protected virtual IEnumerable<Type> GetSnippetListDefinitionTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ISnippetListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        public bool ConversationAppearsWhen()
        {
            var player = GetPCSpeaker();
            return ProcessConditions(player);
        }

        public void ConversationAction()
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
        private bool ProcessConditions(uint player)
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
        private void ProcessActions(uint player)
        {
            foreach (var action in _actionsTakenCommands)
            {
                if (!UtilPlugin.GetScriptParamIsSet(action.Key)) continue;

                var param = GetScriptParam(action.Key);
                var args = param.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                ExecuteAction(player, action.Key, args);
            }
        }

        public bool EvaluateCondition(
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

            var previousOwner = _executionOwner.Value;
            _executionOwner.Value = GetIsObjectValid(owner) ? owner : OBJECT_INVALID;
            try
            {
                return snippet.ConditionAction(player, arguments.ToArray());
            }
            finally
            {
                _executionOwner.Value = previousOwner;
            }
        }

        public bool ExecuteAction(
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

            var previousOwner = _executionOwner.Value;
            _executionOwner.Value = GetIsObjectValid(owner) ? owner : OBJECT_INVALID;
            bool succeeded;
            try
            {
                succeeded = snippet.ActionsTakenAction(player, arguments.ToArray());
            }
            finally
            {
                _executionOwner.Value = previousOwner;
            }

            return succeeded;
        }

        public uint GetExecutionOwner()
        {
            return GetIsObjectValid(_executionOwner.Value) ? _executionOwner.Value : OBJECT_SELF;
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
        private bool HasUsableArguments(string key, SnippetDetail snippet, int argumentCount, uint player)
        {
            if (snippet.Arguments.Count == 0 || snippet.HasEnoughArguments(argumentCount))
                return true;

            var error = $"'{key}' was given {argumentCount} argument(s) but needs at least "
                        + $"{snippet.MinimumArgumentCount}.";

            SendMessageToPC(player, error);
            WriteError(error);
            return false;
        }

        /// <summary>
        /// Writes to the error log. <c>protected virtual</c> so a unit test can read what was reported; the body is
        /// the original <c>Log.Write</c> call.
        /// </summary>
        protected virtual void WriteError(string message)
        {
            Log.Write(LogGroup.Error, message);
        }
    }
}
