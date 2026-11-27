namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The server's two subscriptions to process-wide events, held so they can be removed. Each is a named handler kept
    /// by this service, never a lambda, so <see cref="ServerTeardown"/> can unsubscribe it.
    /// </summary>
    public interface IProcessEventSubscriptions
    {
        /// <summary>True while the unhandled-exception handler is subscribed.</summary>
        bool IsSubscribedToUnhandledException { get; }

        /// <summary>True while the process-exit handler is subscribed.</summary>
        bool IsSubscribedToProcessExit { get; }

        /// <summary>
        /// Subscribes to the unhandled-exception event; <paramref name="onUnhandled"/> receives the event's exception
        /// object. Does nothing when already subscribed.
        /// </summary>
        void SubscribeUnhandledException(Action<object> onUnhandled);

        /// <summary>
        /// Subscribes to the process-exit event; <paramref name="onExit"/> runs when the process exits. Does nothing when
        /// already subscribed.
        /// </summary>
        void SubscribeProcessExit(Action onExit);

        /// <summary>Removes both subscriptions. Safe to call when neither (or only one) is subscribed.</summary>
        void UnsubscribeAll();
    }
}
