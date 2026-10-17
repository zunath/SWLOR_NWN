namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// Whether one player list action can be used, and why not when it cannot.
    /// </summary>
    public class PlayerListActionState
    {
        public bool IsEnabled { get; }
        public string DisabledReason { get; }

        private PlayerListActionState(bool isEnabled, string disabledReason)
        {
            IsEnabled = isEnabled;
            DisabledReason = disabledReason;
        }

        public static PlayerListActionState Enabled() => new(true, string.Empty);

        public static PlayerListActionState Disabled(string reason) => new(false, reason);
    }
}
