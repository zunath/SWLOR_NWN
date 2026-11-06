using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ground telegraphs. The logic lives in <see cref="ITelegraphService"/>; the event handlers stay here and every
    /// member forwards to the container's instance.
    /// </summary>
    public static class Telegraph
    {
        public const int MaxRenderCount = Domain.Combat.TelegraphService.MaxRenderCount;

        private static ITelegraphService Instance => FacadeAccessor.Resolve<ITelegraphService>();

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
        public static string CreateTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            Vector2 size,
            float duration,
            bool isHostile,
            TelegraphType type,
            ApplyTelegraphEffect action,
            bool isPersistentAreaIndicator = false)
            => Instance.CreateTelegraph(creator, position, rotation, size, duration, isHostile, type, action, isPersistentAreaIndicator);

        /// <summary>
        /// Cancels a telegraph effect before it completes.
        /// </summary>
        /// <param name="telegraphId">ID of the telegraph to cancel</param>
        public static void CancelTelegraph(string telegraphId) => Instance.CancelTelegraph(telegraphId);

        /// <summary>
        /// Gets all active telegraphs in a specific area.
        /// </summary>
        /// <param name="area">Area to check</param>
        /// <returns>Dictionary of telegraph IDs to telegraph data</returns>
        public static Dictionary<string, ActiveTelegraph> GetTelegraphsInArea(uint area) => Instance.GetTelegraphsInArea(area);

        /// <summary>
        /// Copies the geometry of existing markers so activation warnings can be compared
        /// with later impacts after their timers expire. Missing marker IDs are ignored.
        /// </summary>
        public static IReadOnlyList<TelegraphGeometry> CaptureGeometry(
            IEnumerable<string> telegraphIds)
            => Instance.CaptureGeometry(telegraphIds);

        /// <summary>
        /// Requests an impact flash unless a captured activation marker already described
        /// the same area. Null or empty snapshots preserve instant and delayed impact flashes.
        /// </summary>
        public static bool ShouldShowImpactFlash(
            TelegraphGeometry impact,
            IReadOnlyList<TelegraphGeometry> activationTelegraphs)
            => Instance.ShouldShowImpactFlash(impact, activationTelegraphs);

        /// <summary>
        /// Keeps a warned impact on its displayed footprint even after its target or caster moves.
        /// Other areas, shapes and sizes can belong to separate payloads and retain their own geometry.
        /// </summary>
        public static TelegraphGeometry ResolveImpactGeometry(
            TelegraphGeometry impact,
            IReadOnlyList<TelegraphGeometry> activationTelegraphs)
            => Instance.ResolveImpactGeometry(impact, activationTelegraphs);

        /// <summary>
        /// Checks if a creature is within a telegraph's area of effect.
        /// </summary>
        /// <param name="creature">Creature to check</param>
        /// <param name="telegraphId">ID of the telegraph</param>
        /// <returns>True if creature is within the telegraph</returns>
        public static bool IsCreatureInTelegraph(
            uint creature,
            string telegraphId)
            => Instance.IsCreatureInTelegraph(creature, telegraphId);

        public static void OnRemoved(uint telegrapher, string telegraphId) => Instance.OnRemoved(telegrapher, telegraphId);

        [NWNEventHandler(ScriptName.TelegraphEffect)]
        public static void OnTelegraphEffect() => Instance.OnTelegraphEffect();

        /// <summary>
        /// Clears all telegraphs from memory. Used for cleanup.
        /// </summary>
        public static void ClearAllTelegraphs() => Instance.ClearAllTelegraphs();

        /// <summary>
        /// Updates shader uniforms for all players to display telegraphs.
        /// </summary>
        public static void UpdateShadersForAllPlayers() => Instance.UpdateShadersForAllPlayers();

        /// <summary>
        /// Refreshes active telegraphs for specific player viewers after relationship state changes.
        /// Party membership affects hostile telegraph colors, so existing packed uniforms must be
        /// rebuilt even when no telegraph was created or removed.
        /// </summary>
        /// <param name="players">Players whose telegraph uniforms should be refreshed.</param>
        public static void UpdateShadersForPlayers(IEnumerable<uint> players) => Instance.UpdateShadersForPlayers(players);

        /// <summary>
        /// Pushes the current telegraph state to a player entering an area. Without this, a player who
        /// zones in (or logs in) while a telegraph is already running sees nothing until some unrelated
        /// telegraph happens to be created or removed in that same area.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter() => Instance.OnAreaEnter();

        /// <summary>
        /// Possession moves a DM's client to another object, so the uniforms recorded as sent to that object are
        /// out of date. Resend every slot once the possession has settled.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void ResendShaderStateOnDMPossess() => Instance.ResendShaderStateOnDMPossess();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetShaderStateOnExit() => Instance.ForgetShaderStateOnExit();

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
        public static string CreateSphereTelegraph(
            uint creator,
            Vector3 position,
            float radius,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action,
            bool isPersistentAreaIndicator = false)
            => Instance.CreateSphereTelegraph(creator, position, radius, duration, isHostile, action, isPersistentAreaIndicator);

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
        public static string CreateConeTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            float length,
            float width,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
            => Instance.CreateConeTelegraph(creator, position, rotation, length, width, duration, isHostile, action);

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
        public static string CreateLineTelegraph(
            uint creator,
            Vector3 position,
            float rotation,
            float length,
            float width,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
            => Instance.CreateLineTelegraph(creator, position, rotation, length, width, duration, isHostile, action);

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
        public static string CreateTelegraphAtCreature(
            uint creator,
            uint target,
            TelegraphType type,
            Vector2 size,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
            => Instance.CreateTelegraphAtCreature(creator, target, type, size, duration, isHostile, action);

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
        public static string CreateTelegraphInFrontOfCreature(
            uint creator,
            uint target,
            float distance,
            TelegraphType type,
            Vector2 size,
            float duration,
            bool isHostile,
            ApplyTelegraphEffect action)
            => Instance.CreateTelegraphInFrontOfCreature(creator, target, distance, type, size, duration, isHostile, action);
    }
}
