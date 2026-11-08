using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SWLOR.Game.Server.Core
{
    public class ScriptRegistry
    {
        private class ActionScript
        {
            public Action Action { get; set; }
            public string Name { get; set; }
            public MethodInfo Method { get; set; }
        }

        private class ConditionalScript
        {
            public ConditionalScriptDelegate Action { get; set; }
            public string Name { get; set; }
            public MethodInfo Method { get; set; }
        }

        public delegate bool ConditionalScriptDelegate();

        private const int MaxCharsInScriptName = 16;

        private readonly Dictionary<string, List<ActionScript>> _scripts;
        private readonly Dictionary<string, List<ConditionalScript>> _conditionalScripts;
        private readonly Func<ScriptHandlerManifest> _loadManifest;

        /// <summary>
        /// Handlers that were registered but that the manifest does not list, as <c>script kind Namespace.Type.Method</c>.
        /// They run after the listed handlers of their script, in discovery order. Set by every load.
        /// </summary>
        public IReadOnlyList<string> UnlistedHandlers { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Manifest lines for which no handler was found, as <c>script kind Namespace.Type.Method</c>. They are ignored.
        /// Set by every load.
        /// </summary>
        public IReadOnlyList<string> StaleManifestEntries { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// A registry whose handler order comes from the manifest embedded in the server assembly
        /// (<see cref="ScriptHandlerManifest.ResourceName"/>).
        /// </summary>
        public ScriptRegistry()
            : this(() => ScriptHandlerManifest.LoadEmbedded(typeof(ScriptRegistry).Assembly))
        {
        }

        /// <param name="loadManifest">
        /// Supplies the manifest each time the registry loads handlers through
        /// <see cref="LoadHandlersFromAssembly"/> or <see cref="LoadHandlersFromAssemblies(IEnumerable{Assembly})"/>.
        /// An exception from it is logged and thrown from the load: the registry never falls back to discovery order.
        /// </param>
        public ScriptRegistry(Func<ScriptHandlerManifest> loadManifest)
        {
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();
            _loadManifest = loadManifest ?? throw new ArgumentNullException(nameof(loadManifest));
        }

        /// <param name="resolveOwner">
        /// The instance a handler declared on an instance method runs on: given the class that declares the method, the
        /// container's singleton of that class, or null when the container does not register it. The server passes the
        /// container's resolver (<c>ContainerHost.ResolveHandlerOwner</c>); without one, a handler on an instance method
        /// stops the load.
        /// </param>
        public void LoadHandlersFromAssembly(Func<Type, object> resolveOwner = null)
        {
            LoadHandlersFromAssemblies(AppDomain.CurrentDomain.GetAssemblies(), resolveOwner);
        }

        /// <summary>
        /// Discovers and registers every <see cref="NWNEventHandler"/> method in the given assemblies, in the
        /// order the assemblies are given, then orders each script's handlers by the shipped manifest.
        /// <see cref="LoadHandlersFromAssembly"/> calls this with every assembly
        /// loaded in the current AppDomain, which is what the server does at boot; tests call it with an explicit
        /// assembly list so they do not depend on what else the test host has loaded.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan, in discovery order.</param>
        /// <param name="resolveOwner">See <see cref="LoadHandlersFromAssembly"/>.</param>
        public void LoadHandlersFromAssemblies(IEnumerable<Assembly> assemblies, Func<Type, object> resolveOwner = null)
        {
            LoadHandlersFromAssemblies(assemblies, ReadManifest(), resolveOwner);
        }

        /// <summary>
        /// <see cref="LoadHandlersFromAssemblies(IEnumerable{Assembly}, Func{Type, object})"/> with the manifest given
        /// instead of the shipped one.
        /// </summary>
        public void LoadHandlersFromAssemblies(IEnumerable<Assembly> assemblies, ScriptHandlerManifest manifest, Func<Type, object> resolveOwner = null)
        {
            LoadHandlers(assemblies.SelectMany(a => a.GetTypes()).SelectMany(t => t.GetMethods()), manifest, resolveOwner);
        }

        /// <summary>
        /// Registers every method in <paramref name="candidateMethods"/> that carries <see cref="NWNEventHandler"/>.
        /// The sequence is the discovery order, which only decides the order of handlers the manifest does not list
        /// and which unlisted or stale entries are reported first; it never changes the order of listed handlers.
        /// </summary>
        /// <remarks>
        /// A static method is bound as it always was. A public instance method is bound to the instance
        /// <paramref name="resolveOwner"/> gives for the class that declares it (the container's singleton), once per
        /// load: a handler of a class reached through a subclass (a test double deriving from a service) is the same
        /// handler and is not registered again. Its name stays <c>DeclaringType.FullName + "." + Name</c>
        /// (<see cref="ScriptHandlerManifest.HandlerName"/>). A handler on a class the container does not register stops
        /// the load with a message: it would have no instance to run on.
        /// </remarks>
        public void LoadHandlers(IEnumerable<MethodInfo> candidateMethods, ScriptHandlerManifest manifest, Func<Type, object> resolveOwner = null)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            _scripts.Clear();
            _conditionalScripts.Clear();
            UnlistedHandlers = Array.Empty<string>();
            StaleManifestEntries = Array.Empty<string>();

            var handlers = candidateMethods
                .Where(m => m.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0)
                .Where(m => m.IsStatic || m.DeclaringType == m.ReflectedType)
                .ToArray();
            var owners = new Dictionary<Type, object>();

            foreach (var mi in handlers)
            {
                var target = mi.IsStatic ? null : OwnerOf(mi, resolveOwner, owners);
                foreach (var attr in mi.GetCustomAttributes(typeof(NWNEventHandler), false))
                {
                    var script = ((NWNEventHandler)attr).Script;
                    if (script.Length > MaxCharsInScriptName || script.Length == 0)
                    {
                        Log.Write(LogGroup.Error, $"Script name '{script}' is invalid on method {mi.Name}.", true);
                        throw new ApplicationException();
                    }

                    if (mi.ReturnType == typeof(bool))
                    {
                        RegisterConditionalScript(script, mi, target);
                    }
                    else if (mi.ReturnType == typeof(void))
                    {
                        RegisterActionScript(script, mi, target);
                    }
                    else
                    {
                        Log.Write(LogGroup.Error, $"Method '{mi.Name}' tied to script '{script}' has an invalid return type. This script was NOT loaded.", true);
                    }
                }
            }

            ApplyManifestOrder(manifest);
        }

        /// <summary>
        /// The instance an instance-method handler runs on: the container's singleton of the declaring class, asked for
        /// once per class and load. Throws (and logs) when there is no resolver or the container does not register the class.
        /// </summary>
        private static object OwnerOf(MethodInfo method, Func<Type, object> resolveOwner, Dictionary<Type, object> owners)
        {
            var type = method.DeclaringType;
            if (owners.TryGetValue(type, out var known))
                return known;

            string problem = null;
            object owner = null;
            if (resolveOwner == null)
            {
                problem = $"{ScriptHandlerManifest.HandlerName(method)} is an event handler on an instance method, but the script registry " +
                          "was loaded without a resolver for the classes that hold handlers. ServerBootstrapper.LoadScripts passes the " +
                          "service container's; a test passes one too (or loads only static handlers).";
            }
            else
            {
                owner = resolveOwner(type);
                if (owner == null)
                {
                    problem = $"{ScriptHandlerManifest.HandlerName(method)} is an event handler on an instance method of {type.FullName}, " +
                              "which the service container does not register, so it has no instance to run on. Register the class in " +
                              "Composition/ServiceRegistrations.cs (its handlers then run on the container's singleton) or make the handler static.";
                }
                else if (owner.GetType() != type)
                {
                    problem = $"{ScriptHandlerManifest.HandlerName(method)}: the resolver answered {type.FullName} with an instance of " +
                              $"{owner.GetType().FullName}. A handler runs on the instance of the class that declares it.";
                }
            }

            if (problem != null)
            {
                Log.Write(LogGroup.Error, "Script handlers were NOT registered: " + problem, true);
                throw new InvalidOperationException(problem);
            }

            owners[type] = owner;
            return owner;
        }

        private ScriptHandlerManifest ReadManifest()
        {
            try
            {
                return _loadManifest() ?? throw new InvalidOperationException("The script handler order manifest loader returned nothing.");
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error,
                    "Script handlers were NOT registered: the script handler order manifest could not be loaded. " + ex.Message,
                    true);
                throw;
            }
        }

        /// <summary>
        /// Reorders each script's handlers (registered so far in discovery order) to the manifest's order, and records
        /// and logs the handlers it does not list and the entries it lists that no handler matches.
        /// </summary>
        private void ApplyManifestOrder(ScriptHandlerManifest manifest)
        {
            var unlisted = new List<(string Description, Assembly Assembly)>();
            var stale = new List<string>();

            foreach (var (script, handlers) in _scripts)
                Arrange(manifest, script, false, handlers, h => h.Method, unlisted, stale);
            foreach (var (script, handlers) in _conditionalScripts)
                Arrange(manifest, script, true, handlers, h => h.Method, unlisted, stale);

            foreach (var script in manifest.ScriptNames)
            {
                if (!_scripts.ContainsKey(script))
                    stale.AddRange(manifest.GetHandlers(script, false).Select(name => $"{script} action {name}"));
                if (!_conditionalScripts.ContainsKey(script))
                    stale.AddRange(manifest.GetHandlers(script, true).Select(name => $"{script} conditional {name}"));
            }

            UnlistedHandlers = unlisted.Select(u => u.Description).ToArray();
            StaleManifestEntries = stale.ToArray();
            LogOrderProblems(unlisted, stale);
        }

        private static void Arrange<T>(
            ScriptHandlerManifest manifest,
            string script,
            bool conditional,
            List<T> handlers,
            Func<T, MethodInfo> methodOf,
            List<(string Description, Assembly Assembly)> unlisted,
            List<string> stale)
        {
            var kind = conditional ? "conditional" : "action";
            var discovered = handlers.ToArray();
            var names = discovered.Select(h => ScriptHandlerManifest.HandlerName(methodOf(h))).ToArray();
            var (order, notListed, staleNames) = manifest.Arrange(script, conditional, names);

            handlers.Clear();
            handlers.AddRange(order.Select(index => discovered[index]));

            foreach (var index in notListed)
                unlisted.Add(($"{script} {kind} {names[index]}", methodOf(discovered[index]).DeclaringType?.Assembly));
            foreach (var name in staleNames)
                stale.Add($"{script} {kind} {name}");
        }

        private static void LogOrderProblems(List<(string Description, Assembly Assembly)> unlisted, List<string> stale)
        {
            // Handlers in other assemblies (the engine test assembly) are never listed in the game's manifest, so
            // they are summarised instead of reported as something to fix.
            var owner = typeof(ScriptRegistry).Assembly;
            var ownUnlisted = unlisted.Where(u => u.Assembly == owner).Select(u => u.Description).ToArray();
            var otherUnlisted = unlisted.Where(u => u.Assembly != owner).ToArray();

            if (ownUnlisted.Length > 0)
            {
                Log.Write(LogGroup.Server, Describe(
                    $"Script handler order: {ownUnlisted.Length} handler(s) are not listed in {ScriptHandlerManifest.ResourceName} " +
                    "(SWLOR.Game.Server/Core/ScriptHandlerOrder.txt) and run after the listed handlers of their script, in discovery order. " +
                    "Add them to the manifest (see Readmes/CoreSystems.md, Script Handler Order):",
                    ownUnlisted), true);
            }

            if (otherUnlisted.Length > 0)
            {
                var assemblies = string.Join(", ", otherUnlisted
                    .Select(u => u.Assembly?.GetName().Name)
                    .Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal));
                Log.Write(LogGroup.Server,
                    $"Script handler order: {otherUnlisted.Length} handler(s) from {assemblies} are not in the manifest and run after the listed handlers of their script.",
                    true);
            }

            if (stale.Count > 0)
            {
                Log.Write(LogGroup.Server, Describe(
                    $"Script handler order: {stale.Count} line(s) of {ScriptHandlerManifest.ResourceName} match no handler and are ignored. " +
                    "Remove them from SWLOR.Game.Server/Core/ScriptHandlerOrder.txt:",
                    stale), true);
            }
        }

        private static string Describe(string heading, IEnumerable<string> lines)
        {
            var text = new StringBuilder(heading);
            foreach (var line in lines)
                text.Append("\n  ").Append(line);
            return text.ToString();
        }

        // target is null for a static handler and the class's instance for an instance handler.
        private void RegisterConditionalScript(string script, MethodInfo methodInfo, object target)
        {
            var del = target == null
                ? (ConditionalScriptDelegate)methodInfo.CreateDelegate(typeof(ConditionalScriptDelegate))
                : (ConditionalScriptDelegate)methodInfo.CreateDelegate(typeof(ConditionalScriptDelegate), target);

            if (!_conditionalScripts.ContainsKey(script))
                _conditionalScripts[script] = new List<ConditionalScript>();

            _conditionalScripts[script].Add(new ConditionalScript
            {
                Action = del,
                Name = del.Method.DeclaringType?.Name + "." + del.Method.Name,
                Method = methodInfo
            });
        }

        private void RegisterActionScript(string script, MethodInfo methodInfo, object target)
        {
            var del = target == null
                ? (Action)methodInfo.CreateDelegate(typeof(Action))
                : (Action)methodInfo.CreateDelegate(typeof(Action), target);

            if (!_scripts.ContainsKey(script))
                _scripts[script] = new List<ActionScript>();

            _scripts[script].Add(new ActionScript
            {
                Action = del,
                Name = del.Method.DeclaringType?.Name + "." + del.Method.Name,
                Method = methodInfo
            });
        }

        public bool HasScript(string scriptName)
        {
            return _scripts.ContainsKey(scriptName) || _conditionalScripts.ContainsKey(scriptName);
        }

        public bool HasConditionalScript(string scriptName)
        {
            return _conditionalScripts.ContainsKey(scriptName);
        }

        /// <summary>
        /// Names of every script that has at least one action (void) handler, in the order the registry stores them.
        /// </summary>
        public IReadOnlyList<string> GetActionScriptNames()
        {
            return _scripts.Keys.ToArray();
        }

        /// <summary>
        /// Names of every script that has at least one conditional (bool) handler, in the order the registry stores them.
        /// </summary>
        public IReadOnlyList<string> GetConditionalScriptNames()
        {
            return _conditionalScripts.Keys.ToArray();
        }

        public IEnumerable<(Action Action, string Name)> GetActionScripts(string scriptName)
        {
            if (_scripts.TryGetValue(scriptName, out var scripts))
            {
                foreach (var script in scripts)
                    yield return (script.Action, script.Name);
            }
        }

        public IEnumerable<(ConditionalScriptDelegate Action, string Name)> GetConditionalScripts(string scriptName)
        {
            if (_conditionalScripts.TryGetValue(scriptName, out var scripts))
            {
                foreach (var script in scripts)
                    yield return (script.Action, script.Name);
            }
        }
    }
}
