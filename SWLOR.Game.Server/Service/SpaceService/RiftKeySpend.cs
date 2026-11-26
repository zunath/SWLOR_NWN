namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What the key user's saved character file holds for the Rift Key a run spent.</summary>
    public enum RiftKeySpend
    {
        /// <summary>The key is saved as spent, or the run spent none: the run pays.</summary>
        Saved = 0,

        /// <summary>The key goes back to its user: the run pays no spoils.</summary>
        Refunded = 1,

        /// <summary>The key user left before a save held the spend: the spoils wait for their next login.</summary>
        Unconfirmed = 2,
    }
}
