using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IPlayerDescriptorService
    {
        void SetUnknownDisplayName(uint player, string name);

        bool EnsureUnknownDisplayName(uint player);

        string GenerateUnknownDisplayName(Player dbPlayer);

        string GetUnknownDisplayName(uint target);

        /// <summary>
        /// Same as <see cref="GetUnknownDisplayNameByPlayerId"/>, for a player record already loaded.
        /// </summary>
        string GetUnknownDisplayName(Player dbPlayer);

        string GetUnknownDisplayNameByPlayerId(string targetPlayerId);
    }
}
