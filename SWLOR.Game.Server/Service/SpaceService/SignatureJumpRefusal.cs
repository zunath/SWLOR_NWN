namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a Jump to Signature cannot start or was broken off.</summary>
    public enum SignatureJumpRefusal
    {
        None = 0,
        NotPiloting = 1,
        NotFound = 2,
        OtherArea = 3,
        NotLocated = 4,
        AlreadySpooling = 5,
        Damaged = 6,
        Interdicted = 7,
        SiteClosed = 8,
    }
}
