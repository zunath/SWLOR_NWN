namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a Rift Key cannot open a pocket now.</summary>
    public enum RiftRunRefusal
    {
        None = 0,
        NotPiloting = 1,
        InCombat = 2,
        AlreadyInRun = 3,
        TooManyRuns = 4,
        UnknownKey = 5,
        Cloaked = 6,
    }
}
