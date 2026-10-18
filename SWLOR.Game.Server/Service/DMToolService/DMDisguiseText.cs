using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Describes the disguise a character is wearing for the DM windows. Players never see these values.
    /// </summary>
    public static class DMDisguiseText
    {
        public const string NotDisguised = "None";

        /// <summary>
        /// Returns the disguise's private name and when it was put on, or "None" when no disguise is worn.
        /// </summary>
        /// <param name="disguise">The active disguise, or null.</param>
        public static string Describe(PlayerDisguise disguise)
        {
            if (disguise == null)
                return NotDisguised;

            // Clearing disguise cooldowns also clears the activation time.
            return disguise.DateLastActivated.HasValue
                ? $"{disguise.PrivateName} (since {disguise.DateLastActivated.Value:yyyy-MM-dd HH:mm} UTC)"
                : disguise.PrivateName;
        }
    }
}
