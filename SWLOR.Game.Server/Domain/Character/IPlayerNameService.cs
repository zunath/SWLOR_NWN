using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IPlayerNameService
    {
        void ReleaseKnownNamesOnExit();

        void ApplyNameOverridesOnEnter();

        void ClearAfkOnExit();

        bool IsAfk(uint player);

        bool ToggleAfk(uint player);

        void PreventAmbiguousTellTargets();

        string GetDisplayName(uint observer, uint target);

        string GetChatDisplayName(uint observer, uint target);

        /// <summary>Canonical name and current public descriptor for server audit records.</summary>
        string GetAuditName(uint target);

        /// <summary>Server-only identity snapshot for correlating messages with disguise history.</summary>
        string GetAuditIdentity(uint target);

        void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage);

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

        List<string> SearchKnownPlayerIdsByName(uint observer, string searchText, int maxResults);

        string GetColoredDisplayName(uint observer, uint target);

        string GetColoredChatDisplayName(uint observer, uint target);

        string SanitizeKnownName(string name);

        string ValidateKnownName(string name);

        string ValidateKnownNameInput(string name);

        string ValidateKnownNameAssignment(uint observer, uint target, string name);

        void SetKnownName(uint observer, uint target, string name);

        void ForgetKnownName(uint observer, uint target);

        int DeleteKnownNameReferences(string targetIdentityKey);

        void RefreshNameOverridesForObserver(uint observer);

        void RefreshNameOverridesForPlayer(uint player);

        bool ShouldShowDescriptorForNamedPlayers(uint observer);

        bool ShouldShowOwnDescriptor(uint player);

        bool TryGetKnownName(uint observer, uint target, out string knownName);
    }
}
