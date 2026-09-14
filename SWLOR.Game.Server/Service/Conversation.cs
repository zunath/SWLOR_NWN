using System.Collections.Generic;
using System.IO;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Entry point for the NUI-native conversation system. This service intentionally has no
    /// dependency on Dialog, PlayerDialog, BeginConversation, or generated DLG shells.
    /// </summary>
    public static class Conversation
    {
        private const string NuiConversationHandledLocal = "SWLOR_NUI_CONVO";
        private static readonly Dictionary<string, ConversationGraph> Graphs = new();
        public static ConversationRuntime Runtime { get; } = new();

        static Conversation()
        {
            Runtime.RegisterToken("player.name", context => ResolveObjectName(context.Player, context.Player));
            Runtime.RegisterToken("owner.name", context => ResolveObjectName(context.Player, context.Owner));
            Runtime.RegisterToken("player.gender.boy-girl", context => SelectGendered(context.Player, "boy", "girl"));
            Runtime.RegisterToken("player.gender.lad-lass", context => SelectGendered(context.Player, "lad", "lass"));
            Runtime.RegisterToken("player.gender.sir-madam", context => SelectGendered(context.Player, "sir", "madam"));
            Runtime.RegisterToken("player.gender.man-woman", context => SelectGendered(context.Player, "man", "woman"));
            Runtime.RegisterToken("player.race", context => ResolveRaceName(context.Player));
            Runtime.RegisterToken("skyrace.record-holder", _ => ResolveModuleText("SWLOR_SKYRACE_RECORD_HOLDER", "Nobody"));
            Runtime.RegisterToken("skyrace.record-time", _ => ResolveModuleText("SWLOR_SKYRACE_RECORD_TIME", "No time recorded"));
            Runtime.RegisterToken("skyrace.entry-fee", _ => "50");
            Runtime.RegisterToken("skyrace.prize", _ => "250");
            Runtime.RegisterCondition("system.always-false", (_, _) => false);
            Runtime.RegisterAction("system.execute-owner-script", (context, arguments) =>
            {
                if (arguments.Count == 0 || string.IsNullOrWhiteSpace(arguments[0]))
                    throw new InvalidOperationException("The owner-script action requires a script resref.");
                if (!GetIsObjectValid(context.Owner))
                    return false;

                ExecuteScript(arguments[0], context.Owner);
                return true;
            });
        }

        // Snippet handlers are registered during OnModuleCacheBefore. Graphs must load in the
        // after phase so operation validation never depends on reflection handler ordering within
        // the before phase (Skill.CacheData raises OnSwlorSkillCache re-entrantly while it runs).
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheData()
        {
            LoadEmbeddedGraphs();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IConversationGraphDefinition).IsAssignableFrom(type) &&
                               !type.IsInterface &&
                               !type.IsAbstract);

            foreach (var type in types)
            {
                var definition = (IConversationGraphDefinition)Activator.CreateInstance(type);
                RegisterGraph(definition.BuildConversation());
            }

            Console.WriteLine($"Loaded {Graphs.Count} NUI conversation graphs.");
        }

        private static void LoadEmbeddedGraphs()
        {
            var assembly = typeof(Conversation).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.Contains(".ConversationData.", StringComparison.Ordinal) &&
                               name.EndsWith(".conversation.json", StringComparison.OrdinalIgnoreCase));

            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream ??
                    throw new InvalidOperationException($"Embedded conversation resource '{resourceName}' could not be opened."));
                var graph = JsonConvert.DeserializeObject<ConversationGraph>(reader.ReadToEnd()) ??
                            throw new InvalidOperationException($"Embedded conversation resource '{resourceName}' is empty.");
                RegisterGraph(graph);
            }
        }

        public static void RegisterGraph(ConversationGraph graph)
        {
            var errors = ConversationGraphValidator.Validate(graph).ToList();
            if (graph != null)
                errors.AddRange(ValidateRegisteredOperations(graph));
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            if (Graphs.ContainsKey(graph.Id))
                throw new InvalidOperationException($"NUI conversation graph '{graph.Id}' has been registered more than once.");

            Graphs.Add(graph.Id, graph);
        }

        private static IEnumerable<string> ValidateRegisteredOperations(ConversationGraph graph)
        {
            var conditions = (graph.EntryPoints ?? new List<ConversationLink>())
                .SelectMany(link => link.Conditions ?? new List<ConversationCondition>())
                .Concat((graph.Nodes ?? new Dictionary<string, ConversationNode>()).Values
                    .Where(node => node != null)
                    .SelectMany(node => node.Choices ?? new List<ConversationChoiceLink>())
                    .SelectMany(link => link.Conditions ?? new List<ConversationCondition>()))
                .Concat((graph.Choices ?? new Dictionary<string, ConversationChoice>()).Values
                    .Where(choice => choice != null)
                    .SelectMany(choice => choice.Next ?? new List<ConversationLink>())
                    .SelectMany(link => link.Conditions ?? new List<ConversationCondition>()));

            foreach (var key in conditions
                         .Where(condition => condition != null)
                         .Select(condition => condition.Key)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!Runtime.HasCondition(key))
                    yield return $"Conversation '{graph.Id}' uses unregistered condition '{key}'.";
            }

            var actions = (graph.OnStartActions ?? new List<ConversationAction>())
                .Concat(graph.OnEndActions ?? new List<ConversationAction>())
                .Concat(graph.OnAbortActions ?? new List<ConversationAction>())
                .Concat((graph.Nodes ?? new Dictionary<string, ConversationNode>()).Values
                    .Where(node => node != null)
                    .SelectMany(node => node.OnEnterActions ?? new List<ConversationAction>()))
                .Concat((graph.Choices ?? new Dictionary<string, ConversationChoice>()).Values
                    .Where(choice => choice != null)
                    .SelectMany(choice => choice.Actions ?? new List<ConversationAction>()));

            foreach (var key in actions
                         .Where(action => action != null)
                         .Select(action => action.Key)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!Runtime.HasAction(key))
                    yield return $"Conversation '{graph.Id}' uses unregistered action '{key}'.";
            }
        }

        public static bool TryGetGraph(string id, out ConversationGraph graph)
        {
            return Graphs.TryGetValue(id, out graph);
        }

        internal static bool IsValidParticipant(uint player)
        {
            return GetIsObjectValid(player) &&
                   (GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player));
        }

        /// <summary>
        /// Starts the NUI graph assigned to an object's native Dialog ResRef, when that resource
        /// has been migrated. Returning false leaves the caller free to use an explicit legacy
        /// path for resources listed as migration exceptions.
        /// </summary>
        public static bool TryStartAssigned(uint player, uint owner, uint uiTarget = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(owner))
                return false;

            var conversationId = ObjectPlugin.GetDialogResref(owner);
            if (string.IsNullOrWhiteSpace(conversationId) || !Graphs.ContainsKey(conversationId))
                return false;

            Start(player, owner, conversationId, uiTarget);
            return true;
        }

        /// <summary>
        /// Intercepts native creature dialogue only for migrated resources. A missing graph is an
        /// intentional legacy exception and continues through NWN's normal conversation event.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureConversationBefore)]
        public static void StartAssignedCreatureConversation()
        {
            var player = GetLastSpeaker();
            if (!IsValidParticipant(player))
                return;

            MakeCreatureConversationPrivate(OBJECT_SELF);
            if (!TryStartAssigned(player, OBJECT_SELF))
                return;

            // This handler is invoked by nw_c2_default4 through ExecuteScript, not by a
            // cancellable NWNX event. Signal the caller explicitly so it does not continue into
            // BeginConversation after the NUI window has opened.
            SetLocalInt(OBJECT_SELF, NuiConversationHandledLocal, 1);
        }

        private static void MakeCreatureConversationPrivate(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetObjectType(creature) != ObjectType.Creature ||
                GetIsPC(creature) ||
                GetIsDM(creature) ||
                GetIsDMPossessed(creature))
            {
                return;
            }

            ObjectPlugin.SetConversationPrivate(creature, true);
        }

        private static string ResolveModuleText(string variable, string fallback)
        {
            var value = GetLocalString(GetModule(), variable);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        public static void Start(uint player, uint owner, string conversationId, uint uiTarget = OBJECT_INVALID)
        {
            if (!Graphs.TryGetValue(conversationId, out var graph))
            {
                ReportStartFailure(player, conversationId, $"Conversation graph '{conversationId}' is not registered.");
                return;
            }

            Start(player, owner, graph, Runtime, uiTarget);
        }

        public static void Start(
            uint player,
            uint owner,
            ConversationGraph graph,
            IConversationRuntime runtime = null,
            uint uiTarget = OBJECT_INVALID)
        {
            var conversationId = graph?.Id ?? "<null>";
            if (!IsValidParticipant(player))
            {
                ReportStartFailure(player, conversationId, "The conversation target is not a valid player.");
                return;
            }

            try
            {
                var context = new ConversationContext(player, owner);
                var session = new ConversationSession(graph, context, runtime ?? Runtime);
                if (!session.Start())
                {
                    ReportStartFailure(player, conversationId, "No opening line passed its conditions.");
                    return;
                }

                OpenSession(player, owner, session, uiTarget);
            }
            catch (Exception ex)
            {
                ReportStartFailure(player, conversationId, ex.Message);
            }
        }

        /// <summary>Displays any NUI-native conversation session in the shared conversation window.</summary>
        public static void OpenSession(
            uint player,
            uint owner,
            IConversationSession session,
            uint uiTarget = OBJECT_INVALID)
        {
            if (GetIsDMPossessed(player))
            {
                if (uiTarget == OBJECT_INVALID)
                    uiTarget = player;

                player = GetMaster(player);
            }

            if (Gui.IsWindowOpen(player, GuiWindowType.Conversation))
                Gui.TogglePlayerWindow(player, GuiWindowType.Conversation);

            if (uiTarget == OBJECT_INVALID)
                uiTarget = player;

            var tetherObject = GetIsObjectValid(owner) ? owner : OBJECT_INVALID;
            var payload = new ConversationPayload(player, session);
            Gui.TogglePlayerWindow(player, GuiWindowType.Conversation, payload, tetherObject, uiTarget);
        }

        public static void End(uint player)
        {
            if (GetIsObjectValid(player) && Gui.IsWindowOpen(player, GuiWindowType.Conversation))
                Gui.TogglePlayerWindow(player, GuiWindowType.Conversation);
        }

        private static string ResolveObjectName(uint observer, uint target)
        {
            if (!GetIsObjectValid(target))
                return string.Empty;

            var name = GetIsPC(target)
                ? PlayerName.GetDisplayName(observer, target)
                : GetName(target);

            return UtilPlugin.StripColors(name ?? string.Empty);
        }

        private static string SelectGendered(uint player, string maleText, string femaleText)
        {
            return GetGender(player) == Gender.Female ? femaleText : maleText;
        }

        private static string ResolveRaceName(uint player)
        {
            var strRef = Get2DAString("racialtypes", "Name", (int)GetRacialType(player));
            return int.TryParse(strRef, out var value)
                ? GetStringByStrRef(value, GetGender(player))
                : string.Empty;
        }

        private static void ReportStartFailure(uint player, string conversationId, string detail)
        {
            Log.Write(LogGroup.Error, $"NUI conversation '{conversationId}' could not be started. {detail}");

            if (GetIsObjectValid(player) && GetIsPC(player))
                SendMessageToPC(player, ColorToken.Red("This conversation could not be opened. The error has been logged."));
        }
    }
}
