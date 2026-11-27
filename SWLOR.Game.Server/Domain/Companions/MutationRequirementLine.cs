namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// One requirement of a known mutation recipe, checked against an incubation job. Waitable lines (such as the weekday) can still be met by completing the job later.
    /// </summary>
    public readonly record struct MutationRequirementLine(string Text, bool IsMet, bool IsWaitable);
}
