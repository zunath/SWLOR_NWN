using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IPlayerNameService : IDisplayNameLookup
    {
        void ReleaseKnownNamesOnExit();

        /// <summary>Canonical name and current public descriptor for server audit records.</summary>
        string GetAuditName(uint target);

        /// <summary>Server-only identity snapshot for correlating messages with disguise history.</summary>
        string GetAuditIdentity(uint target);

        string GetDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName);

        /// <summary>
        /// Retrieves the display name of an offline/persisted player record for plain-text UI surfaces
        /// such as NUI windows, which render chat color tokens literally. Unlike
        /// <see cref="GetDisplayNameByPlayerId"/>, targets the observer has named show only the assigned
        /// name (no bracketed descriptor), and no output contains color tokens. Staff observers still see
        /// the canonical name with the descriptor in brackets.
        /// </summary>
        /// <param name="observer">The player viewing the name.</param>
        /// <param name="targetPlayerId">The player Id of the record being displayed.</param>
        /// <param name="fallbackName">The persisted canonical name to fall back on.</param>
        /// <returns>The observer-appropriate display name, free of color tokens.</returns>
        string GetPlainDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName);

        /// <summary>
        /// Returns the observer's known name when present, otherwise the fallback name.
        /// Use for operational permission management surfaces that target persisted character records.
        /// </summary>
        string GetKnownNameOrFallbackByPlayerId(uint observer, string targetPlayerId, string fallbackName);

        string GetColoredDisplayName(uint observer, uint target);

        string SanitizeKnownName(string name);

        bool ShouldShowDescriptorForNamedPlayers(uint observer);

        bool ShouldShowOwnDescriptor(uint player);

        bool TryGetKnownName(uint observer, uint target, out string knownName);

        /// <summary>The observer's stored known names, cached; null when none are stored and <paramref name="createIfMissing"/> is false.</summary>
        PlayerKnownName GetKnownNames(uint observer, bool createIfMissing);

        /// <summary>Drops the cached known names of an observer, after another service rewrote the stored record.</summary>
        void ForgetKnownNames(string observerId);

        /// <summary>Drops the cached descriptor setting of an observer, so the next read comes from the player record.</summary>
        void ForgetDescriptorSetting(string observerId);

        /// <summary>The name <paramref name="observer"/> sees for <paramref name="target"/>; <paramref name="isUnknown"/> is true when it is the public descriptor.</summary>
        string ResolveDisplayName(uint observer, uint target, out bool isUnknown);

        /// <summary>The primary name followed by the gray bracketed descriptor.</summary>
        string BuildDisplayNameWithDescriptor(string primaryName, string descriptor);

        /// <summary>The true name of a target with the gray bracketed descriptor, as staff see it.</summary>
        string BuildStaffDisplayName(uint target);

        /// <summary>The active disguise of a player, or null.</summary>
        PlayerDisguise GetActiveDisguise(uint player);

        /// <summary>The descriptor others see for the player.</summary>
        string GetDisplayDescriptor(uint player);

        /// <summary>Whether the account name is scrambled for the player.</summary>
        bool ShouldScrambleAccountName(uint player);

        /// <summary>Generates a missing base descriptor; true when one was generated.</summary>
        bool EnsureUnknownDisplayName(uint player);

        /// <summary>The identity key of the player's active identity.</summary>
        string GetIdentityKey(uint player);

        /// <summary>Whether an identity key names a disguise.</summary>
        bool IsDisguiseIdentityKey(string identityKey);
    }
}
