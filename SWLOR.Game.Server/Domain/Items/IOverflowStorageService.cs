namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// Items a character upgrade could not keep in a character's inventory. They wait in the database until the
    /// player claims them with /overflow. The static facade <c>Service.OverflowStorage</c> forwards to the container's
    /// instance of this interface.
    /// </summary>
    public interface IOverflowStorageService
    {
        /// <summary>The number of overflow items waiting for a player.</summary>
        /// <param name="playerId">The player's Id.</param>
        long GetItemCount(string playerId);

        /// <summary>Moves overflow items into the player's inventory until it is full.</summary>
        void Claim(uint player);

        /// <summary>Tells the player at login how many items are waiting in overflow storage.</summary>
        void NotifyOnLogin(uint player);
    }
}
