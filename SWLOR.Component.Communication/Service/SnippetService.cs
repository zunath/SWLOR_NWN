using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Dialog;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Communication.Service
{
    public class SnippetService : ISnippetService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IGenericCacheService> _cacheService;
        
        // Cached data
        private IInterfaceCache<string, SnippetDetail> _snippetCache;
        
        // Additional caches for complex data
        private readonly Dictionary<string, SnippetDetail> _appearsWhenCommands = new();
        private readonly Dictionary<string, SnippetDetail> _actionsTakenCommands = new();

        public SnippetService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IGenericCacheService CacheService => _cacheService.Value;

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _snippetCache = CacheService.BuildInterfaceCache<ISnippetListDefinition, string, SnippetDetail>()
                .WithDataExtractor(instance => instance.BuildSnippets())
                .Build();

            // Process snippets for additional caches
            foreach (var (key, snippet) in _snippetCache.AllItems)
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

            Console.WriteLine($"Loaded {_actionsTakenCommands.Count} action snippets.");
            Console.WriteLine($"Loaded {_appearsWhenCommands.Count} condition snippets.");
        }
        /// <summary>
        /// When a conversation node with this script assigned in the "Appears When" event is run,
        /// check for any conversation conditions and process them.
        /// </summary>
        /// <returns></returns>
        [ScriptHandler<OnDialogAppear>]
        [ScriptHandler<OnDialogAppears>]
        [ScriptHandler<OnDialogCondition>]
        [ScriptHandler<OnDialogConditions>]
        public bool ConversationAppearsWhen()
        {
            var player = GetPCSpeaker();
            return ProcessConditions(player);
        }

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        [ScriptHandler<OnDialogAction>]
        [ScriptHandler<OnDialogActions>]
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
        private void ProcessActions(uint player)
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
