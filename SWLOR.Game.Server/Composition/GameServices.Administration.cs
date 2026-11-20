using SWLOR.Game.Server.Domain.Administration;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The DM blueprint palettes (the facade <c>Service.DMBlueprintCatalog</c> until P2-ZN).</summary>
        public static IDMBlueprintCatalogService DMBlueprintCatalog => FacadeAccessor.Resolve<IDMBlueprintCatalogService>();

        /// <summary>Each DM's DM Creator favorites (the facade <c>Service.DMCreatorFavorite</c> until P2-ZN).</summary>
        public static IDMCreatorFavoriteService DMCreatorFavorite => FacadeAccessor.Resolve<IDMCreatorFavoriteService>();

        /// <summary>Each DM's Creature Manager favorites (the facade <c>Service.DMCreatureManagerFavorite</c> until P2-ZN).</summary>
        public static IDMCreatureManagerFavoriteService DMCreatureManagerFavorite => FacadeAccessor.Resolve<IDMCreatureManagerFavoriteService>();

        /// <summary>The DM chooser and creator details (the facade <c>Service.DMObjectDetails</c> until P2-ZN).</summary>
        public static IDMObjectDetailsService DMObjectDetails => FacadeAccessor.Resolve<IDMObjectDetailsService>();

        /// <summary>The DM area and object search (the facade <c>Service.DMObjectSearch</c> until P2-ZN).</summary>
        public static IDMObjectSearchService DMObjectSearch => FacadeAccessor.Resolve<IDMObjectSearchService>();

        /// <summary>The DM player search (the facade <c>Service.DMPlayerDirectory</c> until P2-ZN).</summary>
        public static IDMPlayerDirectoryService DMPlayerDirectory => FacadeAccessor.Resolve<IDMPlayerDirectoryService>();

        /// <summary>The DM chat and display preferences (the facade <c>Service.DMPreference</c> until P2-ZN).</summary>
        public static IDMPreferenceService DMPreference => FacadeAccessor.Resolve<IDMPreferenceService>();

        /// <summary>DM avatar visibility and movement (the facade <c>Service.DMPresence</c> until P2-ZN).</summary>
        public static IDMPresenceService DMPresence => FacadeAccessor.Resolve<IDMPresenceService>();
    }
}
