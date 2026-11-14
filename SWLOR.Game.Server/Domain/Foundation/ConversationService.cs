using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Entry point for the NUI-native conversation system: the logic and state that used to be static in
    /// the <c>Conversation</c> facade, moved here unchanged. The event handlers run on the container's instance; static
    /// callers reach it through <c>GameServices.Conversation</c>. This service intentionally has no dependency on
    /// Dialog, PlayerDialog, BeginConversation, or generated DLG shells.
    /// </summary>
    /// <remarks>
    /// The constructor registers the built-in tokens, condition and action of <see cref="Runtime"/>, as the static
    /// constructor did on first use. It only fills in-memory registries with lambdas: it calls no engine function and no
    /// other service. The graphs are loaded by <see cref="CacheData"/>, and the snippets register their operations
    /// in their own cache handler. The reflection search, the embedded-graph read, the calls into <c>Gui</c> (whose
    /// window table is static), the display-name lookup (which reads the database for a real player) and the error log
    /// are <c>protected virtual</c> so a unit test can answer them; the bodies are the original code.
    /// </remarks>
    public class ConversationService : IConversationService
    {
        private readonly IGuiService _gui;
        private readonly IPlayerNameService _playerName;
        private const string NuiConversationHandledLocal = "SWLOR_NUI_CONVO";
        private readonly Dictionary<string, ConversationGraph> _graphs = new();

        public ConversationRuntime Runtime { get; } = new();

        public ConversationService(IGuiService gui, IPlayerNameService playerName)
        {
            _gui = gui;
            _playerName = playerName;
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
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CacheData()
        {
            LoadEmbeddedGraphs();

            foreach (var type in GetGraphDefinitionTypes())
            {
                var definition = (IConversationGraphDefinition)Activator.CreateInstance(type);
                RegisterGraph(definition.BuildConversation());
            }

            Console.WriteLine($"Loaded {_graphs.Count} NUI conversation graphs.");
        }

        /// <summary>
        /// The types of every graph definition in the loaded assemblies. <c>protected virtual</c> so a unit test can
        /// answer with its own definitions; the default body is the original lazy search (each definition is built
        /// when the loop reaches it).
        /// </summary>
        protected virtual IEnumerable<Type> GetGraphDefinitionTypes()
        {
            return GameAssemblies.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IConversationGraphDefinition).IsAssignableFrom(type) &&
                               !type.IsInterface &&
                               !type.IsAbstract);
        }

        /// <summary>
        /// Registers the authored graphs the server assembly embeds. <c>protected virtual</c> so a unit test can load
        /// its own graphs instead of the whole authored corpus; the body is the original read.
        /// </summary>
        protected virtual void LoadEmbeddedGraphs()
        {
            var assembly = typeof(ConversationService).Assembly;
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

        public void RegisterGraph(ConversationGraph graph)
        {
            var errors = ConversationGraphValidator.Validate(graph).ToList();
            if (graph != null)
                errors.AddRange(ValidateRegisteredOperations(graph));
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            if (_graphs.ContainsKey(graph.Id))
                throw new InvalidOperationException($"NUI conversation graph '{graph.Id}' has been registered more than once.");

            _graphs.Add(graph.Id, graph);
        }

        private IEnumerable<string> ValidateRegisteredOperations(ConversationGraph graph)
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

        public bool TryGetGraph(string id, out ConversationGraph graph)
        {
            return _graphs.TryGetValue(id, out graph);
        }

        public bool IsValidParticipant(uint player)
        {
            return GetIsObjectValid(player) &&
                   (GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player));
        }

        public bool TryStartAssigned(uint player, uint owner, uint uiTarget = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(owner))
                return false;

            var conversationId = ObjectPlugin.GetDialogResref(owner);
            if (string.IsNullOrWhiteSpace(conversationId) || !_graphs.ContainsKey(conversationId))
                return false;

            Start(player, owner, conversationId, uiTarget);
            return true;
        }

        [NWNEventHandler(ScriptName.OnCreatureConversationBefore)]
        public void StartAssignedCreatureConversation()
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

        private void MakeCreatureConversationPrivate(uint creature)
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

        private string ResolveModuleText(string variable, string fallback)
        {
            var value = GetLocalString(GetModule(), variable);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        public void Start(uint player, uint owner, string conversationId, uint uiTarget = OBJECT_INVALID)
        {
            if (!_graphs.TryGetValue(conversationId, out var graph))
            {
                ReportStartFailure(player, conversationId, $"Conversation graph '{conversationId}' is not registered.");
                return;
            }

            Start(player, owner, graph, Runtime, uiTarget);
        }

        public void Start(
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

        public void OpenSession(
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

            if (uiTarget == OBJECT_INVALID)
                uiTarget = player;

            End(player, uiTarget);

            var tetherObject = GetIsObjectValid(owner) ? owner : OBJECT_INVALID;
            var payload = new ConversationPayload(player, session);
            TogglePlayerWindow(player, payload, tetherObject, uiTarget);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void EndOnAreaExit()
        {
            End(GetExitingObject());
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void EndOnAreaEnter()
        {
            // Keep this independent of the other windows' geometry saves and close callbacks.
            End(GetEnteringObject());
        }

        public void End(uint player, uint uiTarget = OBJECT_INVALID)
        {
            if (!IsValidParticipant(player))
                return;

            if (uiTarget == OBJECT_INVALID)
                uiTarget = player;

            if (GetIsDMPossessed(player))
                player = GetMaster(player);

            ClosePlayerWindow(player, uiTarget);
        }

        private string ResolveObjectName(uint observer, uint target)
        {
            if (!GetIsObjectValid(target))
                return string.Empty;

            var name = GetIsPC(target)
                ? GetDisplayName(observer, target)
                : GetName(target);

            return UtilPlugin.StripColors(name ?? string.Empty);
        }

        private string SelectGendered(uint player, string maleText, string femaleText)
        {
            return GetGender(player) == Gender.Female ? femaleText : maleText;
        }

        private string ResolveRaceName(uint player)
        {
            var strRef = Get2DAString("racialtypes", "Name", (int)GetRacialType(player));
            return int.TryParse(strRef, out var value)
                ? GetStringByStrRef(value, GetGender(player))
                : string.Empty;
        }

        private void ReportStartFailure(uint player, string conversationId, string detail)
        {
            WriteError($"NUI conversation '{conversationId}' could not be started. {detail}");

            if (GetIsObjectValid(player) && GetIsPC(player))
                SendMessageToPC(player, ColorToken.Red("This conversation could not be opened. The error has been logged."));
        }

        /// <summary>
        /// While a hot reload quiesces this build: the conversation <paramref name="player"/> has open on their own screen, as data the
        /// next build can resume (<see cref="ResumeAfterReload"/>), with its window taken off the screen without ending it. Null when
        /// there is none, or when it cannot be carried: a session built on an unregistered graph or runtime (a menu built in code,
        /// say, whose actions are closures of this build) or holding state other than strings. Such a conversation is left open for
        /// the reload's window step, which closes it as the player would.
        /// </summary>
        public ConversationReloadState CarryForReload(uint player)
        {
            if (FindOpenSession(player) is not ConversationSession session || session.HasEnded || session.CurrentNode == null ||
                session.Context.State.Values.Any(value => value != null && value is not string))
                return null;

            var graphId = GraphIdOf(session);
            if (graphId.Length == 0 || !_graphs.TryGetValue(graphId, out var graph))
                return null;

            var state = new ConversationReloadState
            {
                GraphId = graph.Id,
                Owner = session.Context.Owner,
                ShownOn = player,
                NodeId = session.CurrentNode.Id,
                Text = session.CurrentText.ToList(),
                ChoiceIds = session.VisibleChoices.Select(choice => choice.Id).ToList(),
                Tokens = new Dictionary<string, string>(session.Context.Tokens),
                State = session.Context.State.ToDictionary(pair => pair.Key, pair => (string)pair.Value),
            };
            DetachWindowForReload(player);
            return state;
        }

        /// <summary>
        /// When a hot reload activates this build: opens <paramref name="state"/>'s conversation for <paramref name="player"/> again at
        /// the line the old build left it, on this build's graph of the same id, without running its start, node or choice actions
        /// again. A graph the build no longer has, or a line it no longer has, is reported and not opened.
        /// </summary>
        public void ResumeAfterReload(uint player, ConversationReloadState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!IsValidParticipant(player) || !_graphs.TryGetValue(state.GraphId ?? string.Empty, out var graph))
            {
                WriteError($"NUI conversation '{state.GraphId}' could not be resumed after a reload: the graph or the player is gone.");
                return;
            }

            var context = new ConversationContext(player, state.Owner);
            foreach (var (key, value) in state.Tokens ?? new Dictionary<string, string>())
                context.Tokens[key] = value;
            foreach (var (key, value) in state.State ?? new Dictionary<string, string>())
                context.State[key] = value;

            ConversationSession session;
            try
            {
                session = ConversationSession.Resume(graph, context, Runtime, state.NodeId, state.Text, state.ChoiceIds);
            }
            catch (Exception ex)
            {
                WriteError($"NUI conversation '{state.GraphId}' could not be resumed after a reload. {ex.Message}");
                return;
            }

            var tetherObject = GetIsObjectValid(state.Owner) ? state.Owner : OBJECT_INVALID;
            TogglePlayerWindow(player, new ConversationPayload(player, session), tetherObject,
                state.ShownOn == player || state.ShownOn == OBJECT_INVALID ? OBJECT_INVALID : state.ShownOn);
        }

        /// <summary>The registered graph id a session runs, found by reference among the registered graphs.</summary>
        private string GraphIdOf(ConversationSession session)
        {
            return _graphs.FirstOrDefault(pair => session.RunsOn(pair.Value, Runtime)).Key ?? string.Empty;
        }

        /// <summary>
        /// The session the conversation window open on <paramref name="player"/>'s screen shows, or null. The original window table of
        /// <c>Gui</c>, on its own so a unit test can answer it.
        /// </summary>
        protected virtual IConversationSession FindOpenSession(uint player)
        {
            if (!_gui.IsWindowOpen(player, GuiWindowType.Conversation))
                return null;

            return (_gui.GetPlayerWindow(player, GuiWindowType.Conversation)?.ViewModel as ConversationViewModel)?.GetActiveSession();
        }

        /// <summary>Takes the conversation window off the screen without ending its session, on its own so a unit test can answer it.</summary>
        protected virtual void DetachWindowForReload(uint player)
        {
            _gui.DetachWindowForReload(player, GuiWindowType.Conversation, player);
        }

        /// <summary>
        /// Opens the conversation window. The original call into <c>Gui</c> (which is not converted yet and keeps its
        /// window table in static state), on its own so a unit test can answer it.
        /// </summary>
        protected virtual void TogglePlayerWindow(uint player, GuiPayloadBase payload, uint tetherObject, uint uiTarget)
        {
            _gui.TogglePlayerWindow(player, GuiWindowType.Conversation, payload, tetherObject, uiTarget);
        }

        /// <summary>
        /// Closes the conversation window. The original call into <c>Gui</c>, on its own so a unit test can answer it.
        /// </summary>
        protected virtual void ClosePlayerWindow(uint player, uint uiTarget)
        {
            _gui.ClosePlayerWindow(player, GuiWindowType.Conversation, uiTarget);
        }

        /// <summary>
        /// The name <paramref name="observer"/> sees for the player <paramref name="target"/>. The original call into
        /// <c>PlayerName</c>, which reads the database for a real player, on its own so a unit test can answer it.
        /// </summary>
        protected virtual string GetDisplayName(uint observer, uint target)
        {
            return _playerName.GetDisplayName(observer, target);
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
