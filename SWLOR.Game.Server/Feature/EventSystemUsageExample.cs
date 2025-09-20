using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Comprehensive example showing how to use the simplified event system.
    /// This demonstrates the key features and patterns using ScriptHandlerAttribute.
    /// </summary>
    public class EventSystemUsageExample
    {
        private readonly IEventService _eventService;

        public EventSystemUsageExample(IEventService eventService)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        /// <summary>
        /// Example of publishing an event using the abstracted event system.
        /// This hides all NWN script implementation details from the caller.
        /// </summary>
        public void PublishModuleLoadEvent()
        {
            var moduleLoadEvent = new OnModuleLoad();
            _eventService.Publish(moduleLoadEvent);
        }

        /// <summary>
        /// Example of subscribing to events using the abstracted event system.
        /// This provides a clean, type-safe API for event subscription.
        /// </summary>
        public IDisposable SubscribeToModuleLoadEvents()
        {
            return _eventService.Subscribe<OnModuleLoad>(HandleModuleLoadEvent);
        }

        /// <summary>
        /// Example of subscribing to events by name.
        /// This allows for more flexible event handling.
        /// </summary>
        public IDisposable SubscribeToModuleLoadEventsByName()
        {
            return _eventService.Subscribe<OnModuleLoad>("Module.OnLoad", HandleModuleLoadEvent);
        }

        /// <summary>
        /// Example event handler method.
        /// </summary>
        private void HandleModuleLoadEvent(OnModuleLoad evt)
        {
            // Handle the event
            Console.WriteLine($"Module loaded at {evt.Timestamp}");
        }

        /// <summary>
        /// Example of using the ScriptHandlerAttribute for automatic registration.
        /// This method will be automatically discovered and registered by the ScriptRegistry.
        /// The script name is automatically derived from the event type using a simple convention.
        /// </summary>
        [ScriptHandlerAttribute<OnModuleLoad>]
        public void HandleModuleLoadViaAttribute(OnModuleLoad evt)
        {
            // This method will be automatically wired to the NWN script
            // No manual registration required!
            // Script name is automatically derived: OnModuleLoad -> "mod_load"
            // (removes "On" prefix, converts PascalCase to snake_case)
            Console.WriteLine($"Module load handled via attribute at {evt.Timestamp}");
        }


        /// <summary>
        /// Example of checking if there are subscribers before publishing.
        /// This can be useful for performance optimization.
        /// </summary>
        public void ConditionalPublish()
        {
            if (_eventService.HasSubscribers<OnModuleLoad>())
            {
                var moduleLoadEvent = new OnModuleLoad();
                _eventService.Publish(moduleLoadEvent);
            }
        }

        /// <summary>
        /// Example of getting subscriber count for monitoring/debugging.
        /// </summary>
        public void LogSubscriberCounts()
        {
            var moduleLoadSubscribers = _eventService.GetSubscriberCount<OnModuleLoad>();
            Console.WriteLine($"Module load event has {moduleLoadSubscribers} subscribers");
        }
    }
}
