using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Dance and Play Instrument performances: the beat loop, the session, the end triggers and the players'
    /// performance preferences. Static callers reach the container's instance of this
    /// interface through <c>GameServices.Performance</c>; the event handlers are on <see cref="PerformanceService"/>.
    /// </summary>
    public interface IPerformanceService
    {
        /// <summary>The Dance ability: starts or stops a dance.</summary>
        void ToggleDance(uint activator, int level);

        /// <summary>The Play Instrument ability: starts or stops a musical performance.</summary>
        void TogglePlayInstrument(uint activator, int level);

        /// <summary>A performer's own Dance or Play Instrument feat doubles as the Stop button, so it is routed here before the busy check can refuse it.</summary>
        bool IsModeFeat(uint activator, FeatType feat);

        /// <summary>Picks the routine; a dancer in a session switches at once.</summary>
        bool SetRoutine(uint player, PerformanceRoutineType routine);

        /// <summary>Picks the set; a musician in a session switches at once.</summary>
        bool SetSet(uint player, PerformanceSetType set);

        /// <summary>Picks the blended second routine. Only a dancer with Showstopper can use it.</summary>
        bool SetSecondRoutine(uint player, PerformanceRoutineType routine);

        /// <summary>Picks the woven second set. Only a musician with Virtuoso can use it.</summary>
        bool SetSecondSet(uint player, PerformanceSetType set);

        /// <summary>
        /// Picks the Cantina song a musician plays. The song must be one they own (or the starter song). A band's audience
        /// hears the new song at the next audience tick when the musician leads the band.
        /// </summary>
        bool SetSong(uint player, int trackId);

        /// <summary>
        /// Switches between active play and Roleplay mode. Roleplay mode drops the cues and Flourish from the hot bar, scores
        /// the show at the Rough rate, and keeps it going while the performer talks or emotes.
        /// </summary>
        void SetRoleplay(uint player, bool roleplay);

        /// <summary>Picks the tempo; a performer in a session changes it at once.</summary>
        void SetTempo(uint player, PerformanceTempo tempo);

        /// <summary>Handles a press of a hot bar feat during a performance.</summary>
        void HandleInput(uint activator, FeatType feat);

        /// <summary>Ends the player's performance, if they have one, and restores their hot bar.</summary>
        void EndPerformance(uint player, PerformanceEndReason reason);

        /// <summary>Restores the hot bar of a player who logs in after an interrupted performance.</summary>
        void RecoverOnLogin();

        /// <summary>Talking or emoting keeps a Roleplay mode performance going. Chat commands do not count.</summary>
        void KeepRoleplayGoing();

        /// <summary>Ends the performance of a player who leaves the module.</summary>
        void EndOnLogout();

        /// <summary>The work of <see cref="EndOnLogout"/> for one player (a hot reload's quiescence step).</summary>
        void EndOnLogoutFor(uint player);

        /// <summary>Ends the performance of a player who leaves the area.</summary>
        void EndOnAreaExit();

        /// <summary>Ends the performance of a player who dies.</summary>
        void EndOnDeath();

        /// <summary>Ends the performance of a player who takes damage.</summary>
        void EndOnDamage();

        /// <summary>Ends the performance of a player who attacks.</summary>
        void EndOnAttack();

        /// <summary>Ends a musician's performance when the instrument is unequipped.</summary>
        void EndOnInstrumentUnequipped();
    }
}
