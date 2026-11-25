namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// One scored candidate action: what <see cref="IAIActionService.SelectAction"/> picks and
    /// <see cref="IAIActionService.ExecuteAction"/> runs.
    /// </summary>
    public sealed class AIActionEvaluation
    {
        public AIActionDefinition Action { get; set; }
        public uint Target { get; set; }
        public int Score { get; set; }
        public int Priority { get; set; }
        public int Index { get; set; }
    }
}
