namespace SWLOR.Game.Server.Service.AIService
{
    public delegate bool AIGuard(AIContext context);
    public delegate int AIScoreCalculation(AIContext context);
    public delegate uint AITargetSelector(AIContext context);
    public delegate bool AIPhaseCondition(AIContext context);
}
