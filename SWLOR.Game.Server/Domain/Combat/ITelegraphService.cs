using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Ground telegraphs: shapes shown to players while an ability winds up, the creatures inside them when they end,
    /// and the shader uniforms that draw them. The static <c>Telegraph</c> facade forwards here.
    /// </summary>
    public interface ITelegraphService
    {
        /// <summary>
        /// Creates a new telegraph effect at the specified location.
        /// </summary>
        /// <param name="creator">The creature creating the telegraph</param>
        /// <param name="position">Position of the telegraph</param>
        /// <param name="rotation">Rotation of the telegraph in radians</param>
        /// <param name="size">Size of the telegraph (X = length/radius, Y = width for cone/line)</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="type">Shape type of the telegraph</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Unique ID for the telegraph</returns>
        string CreateTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            Vector2 size,
            float duration,
            bool isHostile,
            TelegraphType type,
            ApplyTelegraphEffect action,
            bool isPersistentAreaIndicator = false,
            bool isImpactFlash = false);

        /// <summary>
        /// Cancels a telegraph effect before it completes.
        /// </summary>
        /// <param name="telegraphId">ID of the telegraph to cancel</param>
        void CancelTelegraph(string telegraphId);

        /// <summary>
        /// Gets all active telegraphs in a specific area.
        /// </summary>
        /// <param name="area">Area to check</param>
        /// <returns>Dictionary of telegraph IDs to telegraph data</returns>
        Dictionary<string, ActiveTelegraph> GetTelegraphsInArea(uint area);

        /// <summary>
        /// Copies the geometry of existing markers so activation warnings can be compared
        /// with later impacts after their timers expire. Missing marker IDs are ignored.
        /// </summary>
        IReadOnlyList<TelegraphGeometry> CaptureGeometry(IEnumerable<string> telegraphIds);

        /// <summary>
        /// Requests an impact flash unless a captured activation marker already described
        /// the same area. Null or empty snapshots preserve instant and delayed impact flashes.
        /// </summary>
        bool ShouldShowImpactFlash(TelegraphGeometry impact, IReadOnlyList<TelegraphGeometry> activationTelegraphs);

        /// <summary>
        /// Keeps a warned impact on its displayed footprint even after its target or caster moves.
        /// Other areas, shapes and sizes can belong to separate payloads and retain their own geometry.
        /// </summary>
        TelegraphGeometry ResolveImpactGeometry(TelegraphGeometry impact, IReadOnlyList<TelegraphGeometry> activationTelegraphs);

        /// <summary>
        /// Checks if a creature is within a telegraph's area of effect.
        /// </summary>
        /// <param name="creature">Creature to check</param>
        /// <param name="telegraphId">ID of the telegraph</param>
        /// <returns>True if creature is within the telegraph</returns>
        bool IsCreatureInTelegraph(uint creature, string telegraphId);

        void OnRemoved(uint telegrapher, string telegraphId);

        void OnTelegraphEffect();

        /// <summary>
        /// Clears all telegraphs from memory. Used for cleanup.
        /// </summary>
        void ClearAllTelegraphs();

        /// <summary>
        /// Updates shader uniforms for all players to display telegraphs.
        /// </summary>
        void UpdateShadersForAllPlayers();

        /// <summary>
        /// Refreshes active telegraphs for specific player viewers after relationship state changes.
        /// Party membership affects hostile telegraph colors, so existing packed uniforms must be
        /// rebuilt even when no telegraph was created or removed.
        /// </summary>
        /// <param name="players">Players whose telegraph uniforms should be refreshed.</param>
        void UpdateShadersForPlayers(IEnumerable<uint> players);

        /// <summary>
        /// Pushes the current telegraph state to a player entering an area. Without this, a player who
        /// zones in (or logs in) while a telegraph is already running sees nothing until some unrelated
        /// telegraph happens to be created or removed in that same area.
        /// </summary>
        void OnAreaEnter();

        /// <summary>
        /// Possession moves a DM's client to another object, so the uniforms recorded as sent to that object are
        /// out of date. Resend every slot once the possession has settled.
        /// </summary>
        void ResendShaderStateOnDMPossess();

        void ForgetShaderStateOnExit();

        /// <summary>
        /// Creates a simple sphere telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Center position</param>
        /// <param name="radius">Radius of the sphere</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        string CreateSphereTelegraph(
            uint creator,
            Vector3 position,
            float radius,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action,
            bool isPersistentAreaIndicator = false);

        /// <summary>
        /// Creates a cone telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Base position of the cone</param>
        /// <param name="rotation">Direction the cone faces (in radians)</param>
        /// <param name="length">Length of the cone</param>
        /// <param name="width">Width of the cone at the end</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        string CreateConeTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            float length,
            float width,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action);

        /// <summary>
        /// Creates a line telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Start position of the line</param>
        /// <param name="rotation">Direction the line faces (in radians)</param>
        /// <param name="length">Length of the line</param>
        /// <param name="width">Width of the line</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        string CreateLineTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            float length,
            float width,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action);

        /// <summary>
        /// Creates a telegraph at a creature's position.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="target">Target creature to center the telegraph on</param>
        /// <param name="type">Type of telegraph</param>
        /// <param name="size">Size of the telegraph</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        string CreateTelegraphAtCreature(
            uint creator,
            uint target,
            TelegraphType type,
            Vector2 size,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action);

        /// <summary>
        /// Creates a telegraph in front of a creature.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="target">Target creature to position the telegraph in front of</param>
        /// <param name="distance">Distance in front of the target</param>
        /// <param name="type">Type of telegraph</param>
        /// <param name="size">Size of the telegraph</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        string CreateTelegraphInFrontOfCreature(
            uint creator,
            uint target,
            float distance,
            TelegraphType type,
            Vector2 size,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action);
    }
}
