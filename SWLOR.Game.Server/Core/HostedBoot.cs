using SWLOR.Game.Contracts;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// What the permanent host (<c>SWLOR.Game.Host</c>) hands the game build when it boots it as a generation, for
    /// <see cref="ServerBootstrapper.BootstrapHosted"/>.
    /// </summary>
    public sealed class HostedBoot
    {
        /// <param name="hostClosures">
        /// The host's closure handler (the package's <c>CoreFunctionHandler</c>, as NWN.Core's own interface): it
        /// schedules every closure with NWNX and tracks it for the active generation.
        /// </param>
        /// <param name="redisConnection">The host's Redis connection, which outlives the game build.</param>
        /// <param name="gameDirectory">The directory the game build was loaded from, or null when unknown.</param>
        public HostedBoot(global::NWN.Core.ICoreFunctionHandler hostClosures, IRedisConnectionSource redisConnection, string gameDirectory)
        {
            HostClosures = hostClosures ?? throw new ArgumentNullException(nameof(hostClosures));
            RedisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
            GameDirectory = gameDirectory;
        }

        /// <summary>The host's closure handler.</summary>
        public global::NWN.Core.ICoreFunctionHandler HostClosures { get; }

        /// <summary>The host's Redis connection.</summary>
        public IRedisConnectionSource RedisConnection { get; }

        /// <summary>
        /// The directory the game build was loaded from. The host loads the build from bytes, so the assembly has no
        /// location of its own; the engine-test assembly is looked for here first.
        /// </summary>
        public string GameDirectory { get; }
    }
}
