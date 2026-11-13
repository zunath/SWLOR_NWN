using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Dance and Play Instrument performances: the hot bar cues, the beat loop, the live sessions and the players'
    /// performance preferences. Static callers reach the container's instance of this
    /// interface through <c>GameServices.Performance</c>; the event handlers are on <see cref="PerformanceService"/>.
    /// </summary>
    public interface IPerformanceService
    {
        /// <summary>The Dance ability: starts or stops a dance.</summary>
        void ToggleDance(uint activator, int level);

        /// <summary>The Play Instrument ability: starts or stops a musical performance.</summary>
        void TogglePlayInstrument(uint activator, int level);

        /// <summary>True for the cue, Flourish and Options feats of a performance's hot bar.</summary>
        bool IsModeFeat(FeatType feat);

        /// <summary>A performer's own Dance or Play Instrument feat doubles as the Stop button, so it is routed here before the busy check can refuse it.</summary>
        bool IsModeFeat(uint activator, FeatType feat);

        /// <summary>The performer's live session, when they have one.</summary>
        bool TryGetSession(uint performer, out PerformanceSession session);

        /// <summary>A copy of the live sessions.</summary>
        IReadOnlyList<PerformanceSession> GetSessions();

        /// <summary>The player's performance preferences, created on first use.</summary>
        PerformancePreference GetPreference(uint player);

        /// <summary>True when the player has the perk that unlocks the routine.</summary>
        bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine);

        /// <summary>True when the player has the perk that unlocks the set.</summary>
        bool IsSetUnlocked(uint player, PerformanceSetType set);

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

        /// <summary>
        /// Whether the player may play music right now and which instrument family they are using.
        /// An equipped instrument from <see cref="PerformanceInstrumentCatalog"/> sets the family. For tests, the module or player
        /// local int PERFORMANCE_ALLOW_NO_INSTRUMENT lets anyone play; a local string PERFORMANCE_INSTRUMENT_FAMILY on the player or
        /// module then picks the family (Strings by default).
        /// </summary>
        bool HasInstrumentEquipped(uint player, out PerformanceInstrumentFamily family);

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

        /// <summary>The Cantina songs the player owns, plus the starter song every player owns, in catalog order.</summary>
        IReadOnlyList<PerformanceSongOption> GetAvailableSongs(string playerId);

        /// <summary>The song the musician plays: their pick when they own it, else the first collected Cantina song, else the starter.</summary>
        int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong);

        /// <summary>The Performance skill rank that unlocks a perk's first level, or 0 when it has no rank requirement.</summary>
        int GetRequiredRank(PerkType perk);
    }
}
