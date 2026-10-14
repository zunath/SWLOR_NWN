namespace SWLOR.CombatSim.Rules
{
    /// <summary>
    /// From <see cref="AtSeconds"/> of continuous PvP combat onward, apply <see cref="Percent"/>.
    /// </summary>
    public sealed class RampStep
    {
        public float AtSeconds { get; set; }
        public float Percent { get; set; }

        public RampStep()
        {
        }

        public RampStep(float atSeconds, float percent)
        {
            AtSeconds = atSeconds;
            Percent = percent;
        }
    }
}
