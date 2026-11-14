namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What the portrait-tell guard needs to know about a connected client: the player slot the engine addresses it
    /// by and the login object it controls. <see cref="ChatPortraitTellsService"/> reads both from the engine's client
    /// list; a unit test supplies them directly, because that list only exists inside a running server.
    /// </summary>
    /// <param name="PlayerId">The client's player ID (slot).</param>
    /// <param name="LoginObject">The login object the client controls.</param>
    public readonly record struct ChatPortraitTellsClient(uint PlayerId, uint LoginObject);
}
