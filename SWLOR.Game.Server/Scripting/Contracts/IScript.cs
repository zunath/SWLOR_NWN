namespace SWLOR.Game.Server.Scripting.Contracts
{
    public interface IScript
    {
        /// <summary>
        /// This method should only be used to subscribe to other events, such as module heartbeat.
        /// Use the MessageHub.Instance object to do subscriptions.
        /// </summary>
        void SubscribeEvents();

        /// <summary>
        /// This method should only be used to unsubscribe an event you subscribed to in the SubscribeEvents method.
        /// Use the MessageHub.Instance object to do unsubscribing.
        /// </summary>
        void UnsubscribeEvents();

        /// <summary>
        /// This method is called only if you hook an NWN object up with a "script_#" event.
        /// Refer to the Wiki on hooking up events: https://github.com/zunath/SWLOR_NWN/wiki/Event-Hooking,-Subscriptions,-and-Publishing
        /// </summary>
        void Main();
    }
}
