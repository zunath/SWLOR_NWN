namespace SWLOR.Game.Server.Service.PvpCombatService
{
    /// <summary>
    /// One step of a PvP combat ramp: from <see cref="AtSeconds"/> of continuous PvP combat on,
    /// <see cref="Percent"/> applies until the next step is reached.
    /// </summary>
    public readonly struct PvpCombatRampStep
    {
        public float AtSeconds { get; }
        public int Percent { get; }

        public PvpCombatRampStep(float atSeconds, int percent)
        {
            AtSeconds = atSeconds;
            Percent = percent;
        }
    }
}
