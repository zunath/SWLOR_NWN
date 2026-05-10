namespace SWLOR.Game.Server.Service.AIService
{
    public static class AIPhaseResolver
    {
        public static AIPhaseType Resolve(AIContext context)
        {
            if (context.SelfHPPercentage <= 40f)
                return AIPhaseType.Survival;

            if (context.LowestHPAllyPercentage <= 45f)
                return AIPhaseType.Support;

            return AIPhaseType.Damage;
        }
    }
}
