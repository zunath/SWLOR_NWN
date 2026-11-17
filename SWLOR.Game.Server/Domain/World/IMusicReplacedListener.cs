namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Told when the music a player hears was replaced by something other than a performance, such as the player's own music
    /// player. World owns the notice and <c>IMusicService</c> keeps the list; a listener registers itself at boot with
    /// <see cref="IMusicService.RegisterMusicReplacedListener"/>, so the music does not name the performance services that
    /// build after it (supply rule 3 of MaintainabilityRefactorDomainOrder.md; P5-15b).
    /// </summary>
    public interface IMusicReplacedListener
    {
        /// <summary>The player's music was replaced, or stopped, outside a performance.</summary>
        void OnPlayerMusicReplaced(uint player);
    }
}
