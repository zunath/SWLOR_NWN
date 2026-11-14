using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The per-player steps of a hot reload (MaintainabilityRefactorHotReload.md, P4-15): while the old build quiesces, the
    /// <see cref="PlayerReload.Quiesce"/> handlers of <c>mod_exit</c>, and when the new build activates, the
    /// <see cref="PlayerReload.Restore"/> handlers of <c>mod_enter</c>, each run as its <see cref="PlayerSessionAttribute.Step"/> for
    /// every online player, in manifest order. A normal login or logout never gets here.
    /// </summary>
    public static class PlayerReloadPlan
    {
        /// <summary>The two scripts whose handlers carry a <see cref="PlayerSessionAttribute"/>.</summary>
        public static IReadOnlyList<string> Scripts { get; } = new[]
        {
            ScriptName.OnModuleEnter,
            ScriptName.OnModuleExit,
        };

        /// <summary>The class each script's handlers may have that the reload runs, and the step kind it becomes.</summary>
        public static PlayerReload RunClassOf(string script)
        {
            return script switch
            {
                ScriptName.OnModuleEnter => PlayerReload.Restore,
                ScriptName.OnModuleExit => PlayerReload.Quiesce,
                _ => throw new ArgumentOutOfRangeException(nameof(script), script, "Only mod_enter and mod_exit handlers have a player reload class."),
            };
        }

        /// <summary>
        /// The quiescence steps: every <see cref="PlayerReload.Quiesce"/> handler of <c>mod_exit</c> in manifest order, each running its
        /// step for every player <paramref name="players"/> returns when it runs.
        /// </summary>
        public static IReadOnlyList<ReloadStep> QuiescePlan(ScriptRegistry registry, Func<IReadOnlyList<uint>> players)
        {
            return Plan(registry, ScriptName.OnModuleExit, ReloadStepKind.Quiesce, players);
        }

        /// <summary>
        /// The restore steps: every <see cref="PlayerReload.Restore"/> handler of <c>mod_enter</c> in manifest order, each running its
        /// step for every player <paramref name="players"/> returns when it runs.
        /// </summary>
        public static IReadOnlyList<ReloadStep> RestorePlan(ScriptRegistry registry, Func<IReadOnlyList<uint>> players)
        {
            return Plan(registry, ScriptName.OnModuleEnter, ReloadStepKind.Restore, players);
        }

        /// <summary>Every player online now, DMs included (each step's own guards skip whom the handler skips).</summary>
        public static IReadOnlyList<uint> OnlinePlayers()
        {
            var players = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                players.Add(player);
            return players;
        }

        /// <summary>
        /// The per-player method <paramref name="handler"/>'s <see cref="PlayerSessionAttribute.Step"/> names: public, taking the player
        /// (<c>uint</c>), returning nothing, static when the handler is, bound to the handler's owner otherwise.
        /// </summary>
        public static (MethodInfo Method, Action<uint> Run) BindStep(Action handler, string stepName)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var name = ScriptHandlerManifest.HandlerName(handler.Method);
            var declaring = handler.Method.DeclaringType ?? throw new InvalidOperationException($"{name} has no declaring type.");
            var isStatic = handler.Method.IsStatic;
            var flags = BindingFlags.Public | BindingFlags.DeclaredOnly | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            var method = string.IsNullOrWhiteSpace(stepName)
                ? null
                : declaring.GetMethod(stepName, flags, null, new[] { typeof(uint) }, null);
            if (method == null || method.ReturnType != typeof(void))
            {
                throw new InvalidOperationException(
                    $"{name} names player reload step '{stepName}', but {declaring.FullName} has no public {(isStatic ? "static" : "instance")} " +
                    "void method of that name taking the player (uint).");
            }

            var run = isStatic
                ? (Action<uint>)method.CreateDelegate(typeof(Action<uint>))
                : (Action<uint>)method.CreateDelegate(typeof(Action<uint>), handler.Target);
            return (method, run);
        }

        /// <summary>
        /// Runs <paramref name="run"/> for each player in turn. A player whose step throws is logged and the next player runs, as the
        /// next player's logout would; the step then fails with the first exception, so the reload's log names it.
        /// </summary>
        public static void RunForEach(IReadOnlyList<uint> players, Action<uint> run, string stepName)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));
            if (run == null)
                throw new ArgumentNullException(nameof(run));

            Exception first = null;
            var failed = 0;
            foreach (var player in players)
            {
                try
                {
                    run(player);
                }
                catch (Exception ex)
                {
                    failed++;
                    first ??= ex;
                    Log.Write(LogGroup.Error, $"Player reload step {stepName} threw for {GetName(player)} ({GetObjectUUID(player)}). " +
                                              $"Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }
            }

            if (first != null)
                throw new InvalidOperationException($"{stepName} failed for {failed} of {players.Count} players.", first);
        }

        private static IReadOnlyList<ReloadStep> Plan(ScriptRegistry registry, string script, ReloadStepKind kind, Func<IReadOnlyList<uint>> players)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            var runClass = RunClassOf(script);
            var steps = new List<ReloadStep>();
            foreach (var (handler, _) in registry.GetActionScripts(script))
            {
                var session = handler.Method.GetCustomAttribute<PlayerSessionAttribute>(false);
                if (session == null || session.Reload != runClass)
                    continue;

                var (method, run) = BindStep(handler, session.Step);
                var stepName = ScriptHandlerManifest.HandlerName(method);
                steps.Add(new ReloadStep(kind, script, ScriptHandlerManifest.HandlerName(handler.Method), method,
                    () => RunForEach(players(), run, stepName)));
            }

            return steps;
        }
    }
}
