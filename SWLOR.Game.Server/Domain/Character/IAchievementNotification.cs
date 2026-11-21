namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The window that tells a player an achievement was unlocked. Character owns the interface and the UI domain draws it
    /// (supply rule 2 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public interface IAchievementNotification
    {
        /// <summary>Reserves the GUI ids the notification draws with. Run when the module loads; running it again keeps the reservation.</summary>
        void ReserveIds();

        /// <summary>Displays the notification window with the achievement's name.</summary>
        void Display(uint player, string name);
    }
}
