namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// One step of a PvP combat ramp: from <see cref="AtSeconds"/> of continuous PvP combat on,
    /// <see cref="Percent"/> applies until the next step is reached.
    /// </summary>
    public readonly record struct PvpCombatRampStep(float AtSeconds, int Percent);
}
