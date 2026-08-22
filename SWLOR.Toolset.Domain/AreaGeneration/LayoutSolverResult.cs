#nullable disable
using System;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>Outcome of one <see cref="LayoutSolver.Solve"/> call.</summary>
    public sealed class LayoutSolverResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
        public ResolvedLayout Resolved { get; init; }
        public int AttemptSeed { get; init; }
        public string FailureReason { get; init; }
    }
}
