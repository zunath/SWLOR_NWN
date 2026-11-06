using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of Dance and Play Instrument performances. The logic lives in <see cref="IPerformanceService"/>;
    /// the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Performance
    {
        private static IPerformanceService Instance => FacadeAccessor.Resolve<IPerformanceService>();

        public static void ToggleDance(uint activator, int level)
        {
            Instance.ToggleDance(activator, level);
        }

        public static void TogglePlayInstrument(uint activator, int level)
        {
            Instance.TogglePlayInstrument(activator, level);
        }

        public static bool IsModeFeat(FeatType feat)
        {
            return Instance.IsModeFeat(feat);
        }

        /// <summary>A performer's own Dance or Play Instrument feat doubles as the Stop button, so it is routed here before the busy check can refuse it.</summary>
        public static bool IsModeFeat(uint activator, FeatType feat)
        {
            return Instance.IsModeFeat(activator, feat);
        }

        public static bool TryGetSession(uint performer, out PerformanceSession session)
        {
            return Instance.TryGetSession(performer, out session);
        }

        public static IReadOnlyList<PerformanceSession> GetSessions()
        {
            return Instance.GetSessions();
        }

        public static PerformancePreference GetPreference(uint player)
        {
            return Instance.GetPreference(player);
        }

        public static bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine)
        {
            return Instance.IsRoutineUnlocked(player, routine);
        }

        public static bool IsSetUnlocked(uint player, PerformanceSetType set)
        {
            return Instance.IsSetUnlocked(player, set);
        }

        public static bool SetRoutine(uint player, PerformanceRoutineType routine)
        {
            return Instance.SetRoutine(player, routine);
        }

        public static bool SetSet(uint player, PerformanceSetType set)
        {
            return Instance.SetSet(player, set);
        }

        /// <summary>Picks the blended second routine. Only a dancer with Showstopper can use it.</summary>
        public static bool SetSecondRoutine(uint player, PerformanceRoutineType routine)
        {
            return Instance.SetSecondRoutine(player, routine);
        }

        /// <summary>Picks the woven second set. Only a musician with Virtuoso can use it.</summary>
        public static bool SetSecondSet(uint player, PerformanceSetType set)
        {
            return Instance.SetSecondSet(player, set);
        }

        /// <summary>
        /// Picks the Cantina song a musician plays. The song must be one they own (or the starter song). A band's audience
        /// hears the new song at the next audience tick when the musician leads the band.
        /// </summary>
        public static bool SetSong(uint player, int trackId)
        {
            return Instance.SetSong(player, trackId);
        }

        /// <summary>
        /// Switches between active play and Roleplay mode. Roleplay mode drops the cues and Flourish from the hot bar, scores
        /// the show at the Rough rate, and keeps it going while the performer talks or emotes.
        /// </summary>
        public static void SetRoleplay(uint player, bool roleplay)
        {
            Instance.SetRoleplay(player, roleplay);
        }

        public static void SetTempo(uint player, PerformanceTempo tempo)
        {
            Instance.SetTempo(player, tempo);
        }

        /// <summary>
        /// Whether the player may play music right now and which instrument family they are using.
        /// An equipped instrument from <see cref="PerformanceInstrumentCatalog"/> sets the family. For tests, the module or player
        /// local int PERFORMANCE_ALLOW_NO_INSTRUMENT lets anyone play; a local string PERFORMANCE_INSTRUMENT_FAMILY on the player or
        /// module then picks the family (Strings by default).
        /// </summary>
        public static bool HasInstrumentEquipped(uint player, out PerformanceInstrumentFamily family)
        {
            return Instance.HasInstrumentEquipped(player, out family);
        }

        public static void HandleInput(uint activator, FeatType feat)
        {
            Instance.HandleInput(activator, feat);
        }

        public static void EndPerformance(uint player, PerformanceEndReason reason)
        {
            Instance.EndPerformance(player, reason);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverOnLogin()
        {
            Instance.RecoverOnLogin();
        }

        /// <summary>Talking or emoting keeps a Roleplay mode performance going. Chat commands do not count.</summary>
        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void KeepRoleplayGoing()
        {
            Instance.KeepRoleplayGoing();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void EndOnLogout()
        {
            Instance.EndOnLogout();
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void EndOnAreaExit()
        {
            Instance.EndOnAreaExit();
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void EndOnDeath()
        {
            Instance.EndOnDeath();
        }

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void EndOnDamage()
        {
            Instance.EndOnDamage();
        }

        [NWNEventHandler(ScriptName.OnInputAttackObjectBefore)]
        public static void EndOnAttack()
        {
            Instance.EndOnAttack();
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void EndOnInstrumentUnequipped()
        {
            Instance.EndOnInstrumentUnequipped();
        }

        /// <summary>The Cantina songs the player owns, plus the starter song every player owns, in catalog order.</summary>
        public static IReadOnlyList<PerformanceSongOption> GetAvailableSongs(string playerId)
        {
            return Instance.GetAvailableSongs(playerId);
        }

        /// <summary>The song the musician plays: their pick when they own it, else the first collected Cantina song, else the starter.</summary>
        public static int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong)
        {
            return Instance.ChooseSong(playerId, preferredTrackId);
        }

        /// <summary>The Performance skill rank that unlocks a perk's first level, or 0 when it has no rank requirement.</summary>
        public static int GetRequiredRank(PerkType perk)
        {
            return Instance.GetRequiredRank(perk);
        }
    }
}
