namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The names the engine shows players: the name overrides, the chat display names, the AFK flag and the tell handler.
    /// </summary>
    public interface IPlayerNameOverrideService
    {
        void ApplyNameOverridesOnEnter();

        void ClearAfkOnExit();

        bool IsAfk(uint player);

        bool ToggleAfk(uint player);

        void PreventAmbiguousTellTargets();

        string GetChatDisplayName(uint observer, uint target);

        void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage);

        string GetColoredChatDisplayName(uint observer, uint target);

        void RefreshNameOverridesForObserver(uint observer);

        void RefreshNameOverridesForPlayer(uint player);

        /// <summary>Sets the name <paramref name="observer"/> sees for <paramref name="target"/> to the one it resolves now.</summary>
        void ApplyNameOverride(uint observer, uint target);
    }
}
