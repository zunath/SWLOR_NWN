using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// What a game build does instead of the boot scripts when it activates after the module has loaded (a hot reload on a
    /// development or test server, MaintainabilityRefactorHotReload.md section 3): the engine runs no boot script for it, so it
    /// reruns the <see cref="BootReload.Rerun"/> handlers of the boot scripts in manifest order, then runs the adoption step of
    /// each handler that has one (<see cref="BootPhaseAttribute.AdoptedBy"/>: a boot-only handler's stands in for it, a rerun
    /// handler's puts back what the rerun rolled anew) in the order of those handlers. Boot-only and native hook handlers never run. When a reload quiesces the running build, the carry steps
    /// (<see cref="BootPhaseAttribute.CarriedBy"/>) leave what the adoption steps cannot read from the world on the module.
    /// Generation 1 never gets here: the engine runs its boot scripts as it always did.
    /// </summary>
    public static class ReloadBootstrap
    {
        /// <summary>
        /// The boot scripts in the order the server reaches them. At boot <c>db_loaded</c> runs inside <c>mod_preload</c>'s
        /// <c>SyncIndexes</c>, and <c>mod_content_chg</c>, <c>mod_cache_bef</c> and <c>mod_cache_aft</c> inside its
        /// <c>OnModulePreload</c>, all boot only; a reload runs the rerun handlers of each script whole, in this order, so a
        /// rerun handler of <c>mod_preload</c> listed after those two runs before the nested scripts' (today <c>db_loaded</c> has
        /// no rerun handler, and the <c>mod_preload</c> ones do not read what the nested scripts fill).
        /// </summary>
        public static IReadOnlyList<string> BootScripts { get; } = new[]
        {
            ScriptName.OnModulePreload,
            ScriptName.OnDatabaseLoaded,
            ScriptName.OnModuleContentChange,
            ScriptName.OnModuleCacheBefore,
            ScriptName.OnModuleCacheAfter,
            ScriptName.OnModuleLoad,
        };

        /// <summary>
        /// The scripts a rerun boot handler runs through the engine while it runs: the skill cache signals that it has loaded
        /// (the crafting cache fills on that event) and the quest cache runs the quests-registered script (the guilds roll their
        /// tasks and the contract board registers its quests). The engine hands them to the activating build
        /// (<c>IHostCallbacks.BindActivationScripts</c>), so their handlers rerun inside the rerun handler that runs them, as at
        /// boot; they are not steps of the plan. <see cref="ReloadDigests.Cache"/> digests their owners too.
        /// </summary>
        public static IReadOnlyList<string> NestedScripts { get; } = new[]
        {
            ScriptName.OnSWLORCacheSkillsLoaded,
            ScriptName.OnQuestsRegistered,
        };

        /// <summary>
        /// The steps of a reload: every rerun handler of <see cref="BootScripts"/> in the registry's (manifest) order, then every
        /// adoption step in the order of the boot-only or rerun handlers they belong to. Handlers without a
        /// <see cref="BootPhaseAttribute"/> (the engine-test assembly's) are left out.
        /// </summary>
        public static IReadOnlyList<ReloadStep> Plan(ScriptRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var reruns = new List<ReloadStep>();
            var adoptions = new List<ReloadStep>();
            foreach (var (script, handler, phase) in BootHandlers(registry))
            {
                var name = ScriptHandlerManifest.HandlerName(handler.Method);
                if (phase.Reload == BootReload.Rerun)
                    reruns.Add(new ReloadStep(ReloadStepKind.Rerun, script, name, handler.Method, handler));
                if (IsAdoptable(phase.Reload) && !string.IsNullOrEmpty(phase.AdoptedBy))
                    adoptions.Add(Bind(ReloadStepKind.Adopt, script, name, handler, phase.AdoptedBy));
            }

            return reruns.Concat(adoptions).ToArray();
        }

        /// <summary>The carry steps (<see cref="BootPhaseAttribute.CarriedBy"/>), in the order of the handlers they belong to.</summary>
        public static IReadOnlyList<ReloadStep> CarryPlan(ScriptRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            return BootHandlers(registry)
                .Where(entry => IsAdoptable(entry.Phase.Reload) && !string.IsNullOrEmpty(entry.Phase.CarriedBy))
                .Select(entry => Bind(ReloadStepKind.Carry, entry.Script, ScriptHandlerManifest.HandlerName(entry.Handler.Method),
                    entry.Handler, entry.Phase.CarriedBy))
                .ToArray();
        }

        /// <summary>
        /// Runs the steps in order, each through <paramref name="runInContext"/> (the server runs each in a script context of the
        /// module, as the engine runs the boot scripts). A step that throws is logged and the next one runs, as the next handler of
        /// a boot script does.
        /// </summary>
        public static IReadOnlyList<ReloadStepResult> Run(IEnumerable<ReloadStep> steps, Action<Action> runInContext)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            if (runInContext == null)
                throw new ArgumentNullException(nameof(runInContext));

            var results = new List<ReloadStepResult>();
            foreach (var step in steps)
            {
                Exception error = null;
                var time = Stopwatch.StartNew();
                try
                {
                    runInContext(step.Run);
                }
                catch (Exception ex)
                {
                    error = ex;
                    Log.Write(LogGroup.Error, $"Reload {step.Kind.ToString().ToLowerInvariant()} step {step.Name} ({step.Script}) threw an exception. " +
                                              $"Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }

                results.Add(new ReloadStepResult(step, time.Elapsed.TotalMilliseconds, error));
            }

            return results;
        }

        /// <summary>
        /// One line for the server log: how many steps of each kind ran, how many threw, the total time and the slowest steps.
        /// </summary>
        public static string Describe(IReadOnlyList<ReloadStepResult> results, int slowest = 8)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            var text = new StringBuilder();
            text.Append(string.Create(CultureInfo.InvariantCulture,
                $"reran={results.Count(r => r.Step.Kind == ReloadStepKind.Rerun)} adopted={results.Count(r => r.Step.Kind == ReloadStepKind.Adopt)} " +
                $"carried={results.Count(r => r.Step.Kind == ReloadStepKind.Carry)} failed={results.Count(r => r.Error != null)} " +
                $"quiesced={results.Count(r => r.Step.Kind == ReloadStepKind.Quiesce)} restored={results.Count(r => r.Step.Kind == ReloadStepKind.Restore)} " +
                $"totalMs={results.Sum(r => r.Milliseconds):F0} slowest="));
            text.Append(string.Join(",", results
                .OrderByDescending(r => r.Milliseconds)
                .Take(slowest)
                .Select(r => string.Create(CultureInfo.InvariantCulture, $"{r.Step.Name}:{r.Milliseconds:F0}"))));
            var failed = results.Where(r => r.Error != null).Select(r => r.Step.Name).ToArray();
            if (failed.Length > 0)
                text.Append(" failedSteps=").Append(string.Join(",", failed));
            return text.ToString();
        }

        /// <summary>The classes whose handlers can have an adoption and a carry step: boot-only and rerun.</summary>
        private static bool IsAdoptable(BootReload reload)
        {
            return reload is BootReload.BootOnly or BootReload.Rerun;
        }

        /// <summary>Every action handler of the boot scripts that carries a <see cref="BootPhaseAttribute"/>, in manifest order.</summary>
        private static IEnumerable<(string Script, Action Handler, BootPhaseAttribute Phase)> BootHandlers(ScriptRegistry registry)
        {
            foreach (var script in BootScripts)
            {
                foreach (var (handler, _) in registry.GetActionScripts(script))
                {
                    var phase = handler.Method.GetCustomAttribute<BootPhaseAttribute>(false);
                    if (phase != null)
                        yield return (script, handler, phase);
                }
            }
        }

        /// <summary>
        /// The adoption or carry method <paramref name="methodName"/> of the class that declares <paramref name="handler"/>: public,
        /// parameterless, returning nothing, static when the handler is, bound to the handler's owner otherwise.
        /// </summary>
        private static ReloadStep Bind(ReloadStepKind kind, string script, string handlerName, Action handler, string methodName)
        {
            var declaring = handler.Method.DeclaringType
                            ?? throw new InvalidOperationException($"{handlerName} has no declaring type.");
            var isStatic = handler.Method.IsStatic;
            var flags = BindingFlags.Public | BindingFlags.DeclaredOnly | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            var method = declaring.GetMethod(methodName, flags, null, Type.EmptyTypes, null);
            if (method == null || method.ReturnType != typeof(void))
            {
                throw new InvalidOperationException(
                    $"{handlerName} names {kind.ToString().ToLowerInvariant()} method {methodName}, but {declaring.FullName} has no public " +
                    $"{(isStatic ? "static" : "instance")} parameterless void method of that name.");
            }

            var run = isStatic
                ? (Action)method.CreateDelegate(typeof(Action))
                : (Action)method.CreateDelegate(typeof(Action), handler.Target);
            return new ReloadStep(kind, script, handlerName, method, run);
        }
    }
}
