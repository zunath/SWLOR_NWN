namespace SWLOR.Game.Server.Service.AIService
{
    public static class AIPhase
    {
        public static AIPhaseCondition HealthAbove(int percent)
        {
            return context => context.SelfHealthPercent > percent;
        }

        public static AIPhaseCondition HealthAtOrBelow(int percent)
        {
            return context => context.SelfHealthPercent <= percent;
        }

        public static AIPhaseCondition ElapsedCombatSecondsAtLeast(float seconds)
        {
            return context => context.ElapsedCombatSeconds >= seconds;
        }

        public static AIPhaseCondition Always()
        {
            return _ => true;
        }
    }
}
