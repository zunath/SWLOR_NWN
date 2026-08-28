namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Mutually exclusive families for Mimicry traits that provide alternate versions of the same
    /// loadout role. <see cref="None"/> leaves a trait unrestricted.
    /// </summary>
    public enum MimicryTraitFamily
    {
        None = 0,
        Carapace = 1,
        ForceOffense = 2
    }
}
