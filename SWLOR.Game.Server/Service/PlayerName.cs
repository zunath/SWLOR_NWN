using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the player names, known names and AFK flag. The logic and state live in
    /// <see cref="IPlayerNameService"/>; the event handlers stay here and every member forwards to the container's
    /// instance.
    /// </summary>
    public static class PlayerName
    {
        public const int MaxKnownNameLength = Domain.Character.PlayerNameService.MaxKnownNameLength;
        public const string UnknownName = Domain.Character.PlayerNameService.UnknownName;
        // Set on the sender while a tell addressed to a specific character, not a typed name, is sent.
        public const string DirectTellVariable = Domain.Character.PlayerNameService.DirectTellVariable;

        private static IPlayerNameService Instance => FacadeAccessor.Resolve<IPlayerNameService>();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ReleaseKnownNamesOnExit()
        {
            Instance.ReleaseKnownNamesOnExit();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyNameOverridesOnEnter()
        {
            Instance.ApplyNameOverridesOnEnter();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearAfkOnExit()
        {
            Instance.ClearAfkOnExit();
        }

        public static bool IsAfk(uint player)
        {
            return Instance.IsAfk(player);
        }

        public static bool ToggleAfk(uint player)
        {
            return Instance.ToggleAfk(player);
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void PreventAmbiguousTellTargets()
        {
            Instance.PreventAmbiguousTellTargets();
        }

        public static string GetDisplayName(uint observer, uint target)
        {
            return Instance.GetDisplayName(observer, target);
        }

        public static string GetChatDisplayName(uint observer, uint target)
        {
            return Instance.GetChatDisplayName(observer, target);
        }

        /// <summary>Canonical name and current public descriptor for server audit records.</summary>
        public static string GetAuditName(uint target)
        {
            return Instance.GetAuditName(target);
        }

        /// <summary>Server-only identity snapshot for correlating messages with disguise history.</summary>
        public static string GetAuditIdentity(uint target)
        {
            return Instance.GetAuditIdentity(target);
        }

        public static void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage)
        {
            Instance.SendChatMessageWithChatNameOverride(observer, target, sendMessage);
        }

        public static string GetDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName)
        {
            return Instance.GetDisplayNameByPlayerId(observer, targetPlayerId, fallbackName);
        }

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
        public static string GetPlainDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName)
        {
            return Instance.GetPlainDisplayNameByPlayerId(observer, targetPlayerId, fallbackName);
        }

        /// <summary>
        /// Returns the observer's known name when present, otherwise the fallback name.
        /// Use for operational permission management surfaces that target persisted character records.
        /// </summary>
        public static string GetKnownNameOrFallbackByPlayerId(uint observer, string targetPlayerId, string fallbackName)
        {
            return Instance.GetKnownNameOrFallbackByPlayerId(observer, targetPlayerId, fallbackName);
        }

        public static List<string> SearchKnownPlayerIdsByName(uint observer, string searchText, int maxResults)
        {
            return Instance.SearchKnownPlayerIdsByName(observer, searchText, maxResults);
        }

        public static string GetColoredDisplayName(uint observer, uint target)
        {
            return Instance.GetColoredDisplayName(observer, target);
        }

        public static string GetColoredChatDisplayName(uint observer, uint target)
        {
            return Instance.GetColoredChatDisplayName(observer, target);
        }

        public static string SanitizeKnownName(string name)
        {
            return Instance.SanitizeKnownName(name);
        }

        public static string ValidateKnownName(string name)
        {
            return Instance.ValidateKnownName(name);
        }

        public static string ValidateKnownNameInput(string name)
        {
            return Instance.ValidateKnownNameInput(name);
        }

        public static string ValidateKnownNameAssignment(uint observer, uint target, string name)
        {
            return Instance.ValidateKnownNameAssignment(observer, target, name);
        }

        public static void SetKnownName(uint observer, uint target, string name)
        {
            Instance.SetKnownName(observer, target, name);
        }

        public static void ForgetKnownName(uint observer, uint target)
        {
            Instance.ForgetKnownName(observer, target);
        }

        public static int DeleteKnownNameReferences(string targetIdentityKey)
        {
            return Instance.DeleteKnownNameReferences(targetIdentityKey);
        }

        public static void RefreshNameOverridesForObserver(uint observer)
        {
            Instance.RefreshNameOverridesForObserver(observer);
        }

        public static void RefreshNameOverridesForPlayer(uint player)
        {
            Instance.RefreshNameOverridesForPlayer(player);
        }

        public static bool ShouldShowDescriptorForNamedPlayers(uint observer)
        {
            return Instance.ShouldShowDescriptorForNamedPlayers(observer);
        }

        public static bool ShouldShowOwnDescriptor(uint player)
        {
            return Instance.ShouldShowOwnDescriptor(player);
        }

        public static bool TryGetKnownName(uint observer, uint target, out string knownName)
        {
            return Instance.TryGetKnownName(observer, target, out knownName);
        }
    }
}
