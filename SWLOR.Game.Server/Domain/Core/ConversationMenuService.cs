using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Registry and entry point for code-driven NUI conversations: the logic and state that used to be static in
    /// <c>Service.ConversationMenu</c>, moved here unchanged. The facade still carries the event handlers and forwards
    /// every call to the container's instance. This is independent of the old Dialog service and never allocates or
    /// starts a generated DLG shell.
    /// </summary>
    /// <remarks>
    /// The constructor does no work: the menus are registered by <see cref="CacheData"/>. The reflection search and
    /// the error log are <c>protected virtual</c> so a unit test can answer them; the bodies are the original code.
    /// </remarks>
    public class ConversationMenuService : IConversationMenuService
    {
        private readonly IConversationService _conversation;
        private readonly Dictionary<string, Type> _definitions = new(StringComparer.Ordinal);

        public ConversationMenuService(IConversationService conversation)
        {
            _conversation = conversation;
        }

        public void CacheData()
        {
            _definitions.Clear();

            foreach (var type in GetMenuDefinitionTypes())
                _definitions.Add(type.Name, type);

            Console.WriteLine($"Loaded {_definitions.Count} code-driven NUI conversations.");
        }

        /// <summary>
        /// The types of every code-driven menu definition in the loaded assemblies. <c>protected virtual</c> so a unit
        /// test can answer with its own definitions; the default body is the original lazy search.
        /// </summary>
        protected virtual IEnumerable<Type> GetMenuDefinitionTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ConversationMenuDefinition).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               !type.IsInterface);
        }

        public bool Contains(string name) =>
            !string.IsNullOrWhiteSpace(name) && _definitions.ContainsKey(name);

        public void StartFromObjectEvent()
        {
            var owner = OBJECT_SELF;
            var eventScript = GetCurrentlyRunningEvent();
            var player = eventScript switch
            {
                EventScript.Placeable_OnUsed => GetLastUsedBy(),
                EventScript.Creature_OnDialogue => GetLastSpeaker(),
                EventScript.Door_OnFailToOpen => GetClickingObject(),
                _ => OBJECT_INVALID
            };
            if (!_conversation.IsValidParticipant(player))
                return;

            // ModuleConversationRouter replaces a migrated creature's native OnConversation script
            // with this direct route. Preserve nw_c2_default4's creature state gate before opening
            // the NUI graph: petrified/dead creatures refuse, while charmed creatures retain the
            // native exception to the commandability check.
            if (eventScript == EventScript.Creature_OnDialogue &&
                !CanStartCreatureConversation(owner))
                return;

            var name = GetLocalString(owner, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (_conversation.TryGetGraph(name, out _))
                    _conversation.Start(player, owner, name);
                else if (!TryStart(player, owner, name))
                    WriteError($"Object '{GetTag(owner)}' references unknown conversation '{name}'.");
                return;
            }

            if (!_conversation.TryStartAssigned(player, owner))
                AssignCommand(player, () => ActionStartConversation(owner, string.Empty, true, false));
        }

        private bool CanStartCreatureConversation(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return false;

            var isCharmed = false;
            for (var effect = GetFirstEffect(creature);
                 GetIsEffectValid(effect);
                 effect = GetNextEffect(creature))
            {
                var effectType = GetEffectType(effect);
                if (effectType == EffectTypeScript.Petrify)
                    return false;
                if (effectType == EffectTypeScript.Charmed)
                    isCharmed = true;
            }

            if (GetIsDead(creature))
                return false;

            return GetCommandable(creature) || isCharmed;
        }

        public void StartFromLocalConversation()
        {
            var owner = OBJECT_SELF;
            var player = GetLastUsedBy();
            var name = GetLocalString(owner, "CONVERSATION");
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (_conversation.TryGetGraph(name, out _))
                _conversation.Start(player, owner, name);
            else if (!TryStart(player, owner, name))
                WriteError($"Object '{GetTag(owner)}' references unknown conversation '{name}'.");
        }

        public bool TryStart(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID)
        {
            if (!Contains(name))
                return false;
            Start(player, owner, name, uiTarget);
            return true;
        }

        public void Start(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID)
        {
            if (!_conversation.IsValidParticipant(player))
            {
                WriteError($"Conversation menu '{name}' needs a valid player.");
                return;
            }

            if (!_definitions.TryGetValue(name, out var type))
            {
                WriteError($"Conversation menu '{name}' is not registered.");
                SendMessageToPC(player, ColorToken.Red("This interaction could not be opened. The error has been logged."));
                return;
            }

            try
            {
                var definition = (ConversationMenuDefinition)Activator.CreateInstance(type);
                definition.Bind(player, owner);
                var menu = definition.Build();
                var session = new ConversationMenuSession(
                    menu,
                    new ConversationContext(player, owner),
                    _conversation.Runtime);
                definition.Attach(session);
                if (!session.Start())
                    return;

                _conversation.OpenSession(player, owner, session, uiTarget);
            }
            catch (Exception ex)
            {
                WriteError($"Conversation menu '{name}' could not be started. {ex}");
                SendMessageToPC(player, ColorToken.Red("This interaction could not be opened. The error has been logged."));
            }
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
