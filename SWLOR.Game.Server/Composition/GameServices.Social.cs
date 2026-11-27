using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The chat commands (the facade <c>Service.ChatCommand</c> until it was removed).</summary>
        public static IChatCommandService ChatCommand => FacadeAccessor.Resolve<IChatCommandService>();

        /// <summary>Chat handling and emote styles (the static facade <c>Communication</c> until it was removed).</summary>
        public static ICommunicationService Communication => FacadeAccessor.Resolve<ICommunicationService>();

        /// <summary>The extra hotbars: running a slot, and the abilities, combat modes and items a slot can hold.</summary>
        public static IExtraHotbarsService ExtraHotbars => FacadeAccessor.Resolve<IExtraHotbarsService>();

        /// <summary>What a player puts on an extra hotbar.</summary>
        public static IExtraHotbarsEditService ExtraHotbarsEdit => FacadeAccessor.Resolve<IExtraHotbarsEditService>();

        /// <summary>The hand rules of the extra hotbar equipment slots, and equipping a slot.</summary>
        public static IExtraHotbarsEquipService ExtraHotbarsEquip => FacadeAccessor.Resolve<IExtraHotbarsEquipService>();

        /// <summary>Redraws what the extra hotbar slots show.</summary>
        public static IExtraHotbarsStatusService ExtraHotbarsStatus => FacadeAccessor.Resolve<IExtraHotbarsStatusService>();

        /// <summary>The saved extra hotbars and their windows.</summary>
        public static IExtraHotbarsWindowService ExtraHotbarsWindow => FacadeAccessor.Resolve<IExtraHotbarsWindowService>();

        /// <summary>The guard for tells started from a chat-log portrait (the facade <c>Service.ChatPortraitTells</c> until it was removed).</summary>
        public static IChatPortraitTellsService ChatPortraitTells => FacadeAccessor.Resolve<IChatPortraitTellsService>();

        /// <summary>The party caches (the facade <c>Service.Party</c> until it was removed).</summary>
        public static IPartyService Party => FacadeAccessor.Resolve<IPartyService>();

        /// <summary>Dance and Play Instrument performances (the facade <c>Service.Performance</c> until it was removed).</summary>
        public static IPerformanceService Performance => FacadeAccessor.Resolve<IPerformanceService>();

        /// <summary>The clips a performer plays.</summary>
        public static IPerformanceAnimationService PerformanceAnimation => FacadeAccessor.Resolve<IPerformanceAnimationService>();

        /// <summary>The audience tick of Performance.</summary>
        public static IPerformanceAudienceService PerformanceAudience => FacadeAccessor.Resolve<IPerformanceAudienceService>();

        /// <summary>Whether a player may start a performance, and which instrument they play it on.</summary>
        public static IPerformanceEligibilityService PerformanceEligibility => FacadeAccessor.Resolve<IPerformanceEligibilityService>();

        /// <summary>The minutes a performer's Flourish banks for the watchers who follow them.</summary>
        public static IPerformanceFlourishService PerformanceFlourish => FacadeAccessor.Resolve<IPerformanceFlourishService>();

        /// <summary>A performer's hot bar and mode feats.</summary>
        public static IPerformanceHotbarService PerformanceHotbar => FacadeAccessor.Resolve<IPerformanceHotbarService>();

        /// <summary>Puts an Inspired status effect on a creature.</summary>
        public static IPerformanceInspiredService PerformanceInspired => FacadeAccessor.Resolve<IPerformanceInspiredService>();

        /// <summary>The live performance sessions and the players' performance preferences.</summary>
        public static IPerformanceRegistryService PerformanceRegistry => FacadeAccessor.Resolve<IPerformanceRegistryService>();

        /// <summary>What a rider perk of a performance requires, and which routines and sets a player has unlocked.</summary>
        public static IPerformanceRiderService PerformanceRider => FacadeAccessor.Resolve<IPerformanceRiderService>();

        /// <summary>Reads a live performing session into a snapshot.</summary>
        public static IPerformanceSnapshotService PerformanceSnapshot => FacadeAccessor.Resolve<IPerformanceSnapshotService>();

        /// <summary>The Cantina songs a musician owns and plays.</summary>
        public static IPerformanceSongService PerformanceSong => FacadeAccessor.Resolve<IPerformanceSongService>();

        /// <summary>The venue rating of a performance.</summary>
        public static IPerformanceVenueService PerformanceVenue => FacadeAccessor.Resolve<IPerformanceVenueService>();

        /// <summary>What each watcher and performer is doing between the audience ticks.</summary>
        public static IPerformanceWatchingService PerformanceWatching => FacadeAccessor.Resolve<IPerformanceWatchingService>();

        /// <summary>What a watcher does in the Watching window.</summary>
        public static IPerformanceWatchingWindowService PerformanceWatchingWindow => FacadeAccessor.Resolve<IPerformanceWatchingWindowService>();

        /// <summary>The engine side of the Performance windows.</summary>
        public static IPerformanceWindowsService PerformanceWindows => FacadeAccessor.Resolve<IPerformanceWindowsService>();

        /// <summary>The data and actions behind the player list window (the facade <c>Service.PlayerList</c> until it was removed).</summary>
        public static IPlayerListService PlayerList => FacadeAccessor.Resolve<IPlayerListService>();

        /// <summary>DM-hosted events: the schedule, joining and the way back (the facade <c>Service.HostedEvents</c> until the cutover).</summary>
        public static IHostedEventsService HostedEvents => FacadeAccessor.Resolve<IHostedEventsService>();
    }
}
