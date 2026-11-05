using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the public player descriptors. The logic lives in <see cref="IPlayerDescriptorService"/>;
    /// every member forwards to the container's instance.
    /// </summary>
    public static class PlayerDescriptor
    {
        public const string DefaultUnknownDisplayName = Domain.Character.PlayerDescriptorService.DefaultUnknownDisplayName;

        private static IPlayerDescriptorService Instance => FacadeAccessor.Resolve<IPlayerDescriptorService>();

        public static void SetUnknownDisplayName(uint player, string name)
        {
            Instance.SetUnknownDisplayName(player, name);
        }

        public static bool EnsureUnknownDisplayName(uint player)
        {
            return Instance.EnsureUnknownDisplayName(player);
        }

        public static string GenerateUnknownDisplayName(Player dbPlayer)
        {
            return Instance.GenerateUnknownDisplayName(dbPlayer);
        }

        public static string GetUnknownDisplayName(uint target)
        {
            return Instance.GetUnknownDisplayName(target);
        }

        /// <summary>
        /// Same as <see cref="GetUnknownDisplayNameByPlayerId"/>, for a player record already loaded.
        /// </summary>
        public static string GetUnknownDisplayName(Player dbPlayer)
        {
            return Instance.GetUnknownDisplayName(dbPlayer);
        }

        public static string GetUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            return Instance.GetUnknownDisplayNameByPlayerId(targetPlayerId);
        }
    }
}
