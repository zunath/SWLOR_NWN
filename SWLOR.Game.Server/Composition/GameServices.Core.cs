using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Plays the visual effect an object carries for an animation event (the static facade <c>AnimationPlayer</c> until P2-ZI).</summary>
        public static IAnimationPlayerService AnimationPlayer => FacadeAccessor.Resolve<IAnimationPlayerService>();

        /// <summary>The authorization level of a player (the static facade <c>Authorization</c> until P2-ZI).</summary>
        public static IAuthorizationService Authorization => FacadeAccessor.Resolve<IAuthorizationService>();

        /// <summary>Queues jobs for the background worker process (the static facade <c>BackgroundJob</c> until P2-ZI).</summary>
        public static IBackgroundJobService BackgroundJob => FacadeAccessor.Resolve<IBackgroundJobService>();

        /// <summary>NWN data cached for the lifespan of the server (the static facade <c>Cache</c> until P2-ZI).</summary>
        public static ICacheService Cache => FacadeAccessor.Resolve<ICacheService>();

        /// <summary>Chat handling and emote styles (the static facade <c>Communication</c> until P2-ZI).</summary>
        public static ICommunicationService Communication => FacadeAccessor.Resolve<ICommunicationService>();

        /// <summary>The NUI-native conversation system (the static facade <c>Conversation</c> until P2-ZI).</summary>
        public static IConversationService Conversation => FacadeAccessor.Resolve<IConversationService>();

        /// <summary>Registry and entry point for code-driven NUI conversations (the static facade <c>ConversationMenu</c> until P2-ZI).</summary>
        public static IConversationMenuService ConversationMenu => FacadeAccessor.Resolve<IConversationMenuService>();

        /// <summary>The persistence layer (the static facade <c>DB</c> until P2-ZI).</summary>
        public static IDBService DB => FacadeAccessor.Resolve<IDBService>();

        /// <summary>Messages to the players near a creature (the static facade <c>Messaging</c> until P2-ZI).</summary>
        public static IMessagingService Messaging => FacadeAccessor.Resolve<IMessagingService>();

        /// <summary>The server and saved-character migrations (the static facade <c>Migration</c> until P2-ZI).</summary>
        public static IMigrationService Migration => FacadeAccessor.Resolve<IMigrationService>();

        /// <summary>Plays installed named animations (the static facade <c>NamedAnimation</c> until P2-ZI).</summary>
        public static INamedAnimationService NamedAnimation => FacadeAccessor.Resolve<INamedAnimationService>();

        /// <summary>Per-player visibility of tagged objects (the static facade <c>ObjectVisibility</c> until P2-ZI).</summary>
        public static IObjectVisibilityService ObjectVisibility => FacadeAccessor.Resolve<IObjectVisibilityService>();

        /// <summary>Optional diagnostics and throttled warnings for players (the static facade <c>PlayerFeedback</c> until P2-ZI).</summary>
        public static IPlayerFeedbackService PlayerFeedback => FacadeAccessor.Resolve<IPlayerFeedbackService>();

        /// <summary>The conversation snippets (the static facade <c>Snippet</c> until P2-ZI).</summary>
        public static ISnippetService Snippet => FacadeAccessor.Resolve<ISnippetService>();

        /// <summary>Draws and removes tile magic ground tiles (the static facade <c>TileMagic</c> until P2-ZI).</summary>
        public static ITileMagicService TileMagic => FacadeAccessor.Resolve<ITileMagicService>();

        /// <summary>The baked walkable locations of every area (the static facade <c>Walkmesh</c> until P2-ZI).</summary>
        public static IWalkmeshService Walkmesh => FacadeAccessor.Resolve<IWalkmeshService>();
    }
}
