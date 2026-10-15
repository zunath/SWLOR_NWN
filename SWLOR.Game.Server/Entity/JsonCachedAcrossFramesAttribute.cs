namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Keeps the entity's stored JSON in memory across server frames, so the first read of a record in
    /// a frame skips the Redis round trip. Every frame still deserializes a fresh instance. Use only for
    /// entity types that nothing outside the game server writes while it runs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonCachedAcrossFramesAttribute : Attribute
    {
    }
}
