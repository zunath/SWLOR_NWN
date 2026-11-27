namespace SWLOR.Game.Server.Core
{
    /// <summary>How one step of the reload bootstrap went.</summary>
    /// <param name="Step">The step.</param>
    /// <param name="Milliseconds">How long it ran.</param>
    /// <param name="Error">The exception it threw, or null. The next step runs anyway, as the next handler of a boot script does.</param>
    public sealed record ReloadStepResult(ReloadStep Step, double Milliseconds, Exception Error);
}
