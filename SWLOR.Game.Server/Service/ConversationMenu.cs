using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Registry and entry point for code-driven NUI conversations. This is independent of the old
    /// Dialog service and never allocates or starts a generated DLG shell.
    /// </summary>
    public static class ConversationMenu
    {
        private static readonly Dictionary<string, Type> Definitions = new(StringComparer.Ordinal);

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Definitions.Clear();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ConversationMenuDefinition).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               !type.IsInterface);

            foreach (var type in types)
                Definitions.Add(type.Name, type);

            Console.WriteLine($"Loaded {Definitions.Count} code-driven NUI conversations.");
        }

        public static bool Contains(string name) =>
            !string.IsNullOrWhiteSpace(name) && Definitions.ContainsKey(name);

        /// <summary>
        /// Compatibility entry point for module objects whose event script already points at the
        /// SWLOR conversation router. The destination is graph data or a code menu, never a shell.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void StartFromObjectEvent()
        {
            var player = GetCurrentlyRunningEvent() switch
            {
                EventScript.Placeable_OnUsed => GetLastUsedBy(),
                EventScript.Creature_OnDialogue => GetLastSpeaker(),
                EventScript.Door_OnFailToOpen => GetClickingObject(),
                _ => OBJECT_INVALID
            };
            if (!GetIsObjectValid(player) || (!GetIsPC(player) && !GetIsDM(player)))
                return;

            var name = GetLocalString(OBJECT_SELF, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (Conversation.TryGetGraph(name, out _))
                    Conversation.Start(player, OBJECT_SELF, name);
                else if (!TryStart(player, OBJECT_SELF, name))
                    Log.Write(LogGroup.Error, $"Object '{GetTag(OBJECT_SELF)}' references unknown conversation '{name}'.");
                return;
            }

            if (!Conversation.TryStartAssigned(player, OBJECT_SELF))
                AssignCommand(player, () => ActionStartConversation(OBJECT_SELF, string.Empty, true, false));
        }

        [NWNEventHandler(ScriptName.OnDialogStartConversation)]
        public static void StartFromLocalConversation()
        {
            var owner = OBJECT_SELF;
            var player = GetLastUsedBy();
            var name = GetLocalString(owner, "CONVERSATION");
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (Conversation.TryGetGraph(name, out _))
                Conversation.Start(player, owner, name);
            else if (!TryStart(player, owner, name))
                Log.Write(LogGroup.Error, $"Object '{GetTag(owner)}' references unknown conversation '{name}'.");
        }

        public static bool TryStart(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID)
        {
            if (!Contains(name))
                return false;
            Start(player, owner, name, uiTarget);
            return true;
        }

        public static void Start(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player))
            {
                Log.Write(LogGroup.Error, $"Conversation menu '{name}' needs a valid player.");
                return;
            }

            if (!Definitions.TryGetValue(name, out var type))
            {
                Log.Write(LogGroup.Error, $"Conversation menu '{name}' is not registered.");
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
                    Conversation.Runtime);
                definition.Attach(session);
                if (!session.Start())
                    return;

                Conversation.OpenSession(player, owner, session, uiTarget);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Conversation menu '{name}' could not be started. {ex}");
                SendMessageToPC(player, ColorToken.Red("This interaction could not be opened. The error has been logged."));
            }
        }
    }
}
