using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The combat recorder as the domains below combat see it (<c>ICombatRecording</c>).</summary>
        public static ICombatRecording CombatRecording => FacadeAccessor.Resolve<ICombatRecording>();

        /// <summary>Plays the visual effect an object carries for an animation event (the static facade <c>AnimationPlayer</c> until it was removed).</summary>
        public static IAnimationPlayerService AnimationPlayer => FacadeAccessor.Resolve<IAnimationPlayerService>();

        /// <summary>The authorization level of a player (the static facade <c>Authorization</c> until it was removed).</summary>
        public static IAuthorizationService Authorization => FacadeAccessor.Resolve<IAuthorizationService>();

        /// <summary>Queues jobs for the background worker process (the static facade <c>BackgroundJob</c> until it was removed).</summary>
        public static IBackgroundJobService BackgroundJob => FacadeAccessor.Resolve<IBackgroundJobService>();

        /// <summary>NWN data cached for the lifespan of the server (the static facade <c>Cache</c> until it was removed).</summary>
        public static ICacheService Cache => FacadeAccessor.Resolve<ICacheService>();

        /// <summary>The NUI-native conversation system (the static facade <c>Conversation</c> until it was removed).</summary>
        public static IConversationService Conversation => FacadeAccessor.Resolve<IConversationService>();

        /// <summary>Registry and entry point for code-driven NUI conversations (the static facade <c>ConversationMenu</c> until it was removed).</summary>
        public static IConversationMenuService ConversationMenu => FacadeAccessor.Resolve<IConversationMenuService>();

        /// <summary>The persistence layer (the static facade <c>DB</c> until it was removed).</summary>
        public static IDBService DB => FacadeAccessor.Resolve<IDBService>();

        /// <summary>Messages to the players near a creature (the static facade <c>Messaging</c> until it was removed).</summary>
        public static IMessagingService Messaging => FacadeAccessor.Resolve<IMessagingService>();

        /// <summary>The server and saved-character migrations (the static facade <c>Migration</c> until it was removed).</summary>
        public static IMigrationService Migration => FacadeAccessor.Resolve<IMigrationService>();

        /// <summary>Plays installed named animations (the static facade <c>NamedAnimation</c> until it was removed).</summary>
        public static INamedAnimationService NamedAnimation => FacadeAccessor.Resolve<INamedAnimationService>();

        /// <summary>Per-player visibility of tagged objects (the static facade <c>ObjectVisibility</c> until it was removed).</summary>
        public static IObjectVisibilityService ObjectVisibility => FacadeAccessor.Resolve<IObjectVisibilityService>();

        /// <summary>Optional diagnostics and throttled warnings for players (the static facade <c>PlayerFeedback</c> until it was removed).</summary>
        public static IPlayerFeedbackService PlayerFeedback => FacadeAccessor.Resolve<IPlayerFeedbackService>();

        /// <summary>The conversation snippets (the static facade <c>Snippet</c> until it was removed).</summary>
        public static ISnippetService Snippet => FacadeAccessor.Resolve<ISnippetService>();

        /// <summary>Draws and removes tile magic ground tiles (the static facade <c>TileMagic</c> until it was removed).</summary>
        public static ITileMagicService TileMagic => FacadeAccessor.Resolve<ITileMagicService>();

        /// <summary>The baked walkable locations of every area (the static facade <c>Walkmesh</c> until it was removed).</summary>
        public static IWalkmeshService Walkmesh => FacadeAccessor.Resolve<IWalkmeshService>();
    }
}
