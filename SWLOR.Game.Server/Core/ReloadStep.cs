using System.Reflection;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// One step of the reload bootstrap: a rerun boot handler, an adoption step or a carry step.
    /// </summary>
    /// <param name="Kind">What the step is.</param>
    /// <param name="Script">The boot script of the handler the step reruns or stands in for.</param>
    /// <param name="Handler">The boot handler's manifest name (<c>Namespace.Type.Method</c>).</param>
    /// <param name="Method">The method the step runs: the handler itself, or its adoption or carry method.</param>
    /// <param name="Run">Runs the step on the handler's owner (the container's singleton) or statically.</param>
    public sealed record ReloadStep(ReloadStepKind Kind, string Script, string Handler, MethodInfo Method, Action Run)
    {
        /// <summary>The method the step runs, as <c>Namespace.Type.Method</c>.</summary>
        public string Name => ScriptHandlerManifest.HandlerName(Method);
    }
}
