namespace SWLOR.Game.Server.Service.AIService
{
    public readonly record struct AIPhaseId(string Value)
    {
        public static readonly AIPhaseId Invalid = new(string.Empty);

        public static AIPhaseId Create<TPhase>(AIProfileType profile, TPhase phase)
            where TPhase : struct, Enum
        {
            return new AIPhaseId($"{profile}.{typeof(TPhase).Name}.{phase}");
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
