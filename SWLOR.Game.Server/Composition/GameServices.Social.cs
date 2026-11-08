using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The chat commands (the facade <c>Service.ChatCommand</c> until P2-ZM).</summary>
        public static IChatCommandService ChatCommand => FacadeAccessor.Resolve<IChatCommandService>();

        /// <summary>The guard for tells started from a chat-log portrait (the facade <c>Service.ChatPortraitTells</c> until P2-ZM).</summary>
        public static IChatPortraitTellsService ChatPortraitTells => FacadeAccessor.Resolve<IChatPortraitTellsService>();

        /// <summary>The party caches (the facade <c>Service.Party</c> until P2-ZM).</summary>
        public static IPartyService Party => FacadeAccessor.Resolve<IPartyService>();

        /// <summary>Dance and Play Instrument performances (the facade <c>Service.Performance</c> until P2-ZM).</summary>
        public static IPerformanceService Performance => FacadeAccessor.Resolve<IPerformanceService>();

        /// <summary>The audience side of Performance (the facade <c>Service.PerformanceService.PerformanceAudience</c> until P2-ZM).</summary>
        public static IPerformanceAudienceService PerformanceAudience => FacadeAccessor.Resolve<IPerformanceAudienceService>();

        /// <summary>The venue rating of a performance (the facade <c>Service.PerformanceService.PerformanceVenue</c> until P2-ZM).</summary>
        public static IPerformanceVenueService PerformanceVenue => FacadeAccessor.Resolve<IPerformanceVenueService>();

        /// <summary>The engine side of the Performance windows (the facade <c>Service.PerformanceService.PerformanceWindows</c> until P2-ZM).</summary>
        public static IPerformanceWindowsService PerformanceWindows => FacadeAccessor.Resolve<IPerformanceWindowsService>();

        /// <summary>The data and actions behind the player list window (the facade <c>Service.PlayerList</c> until P2-ZM).</summary>
        public static IPlayerListService PlayerList => FacadeAccessor.Resolve<IPlayerListService>();
    }
}
