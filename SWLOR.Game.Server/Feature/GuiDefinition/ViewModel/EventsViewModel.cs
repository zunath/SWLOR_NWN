using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.HostedEventService;
using SWLOR.Game.Server.Service.TimeZoneService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class EventsViewModel : GuiViewModelBase<EventsViewModel, GuiPayloadBase>,
        IGuiRefreshable<HostedEventsChangedRefreshEvent>
    {
        public const string ContentElement = "events_content";
        public const string MainContentPartial = "EVENTS_MAIN";

        private List<string> _eventIds = new();
        private string _selectedEventId = string.Empty;
        private PlayerTimeZone _zone = PlayerTimeZones.Utc;

        public string ListSummaryText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> EventNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> EventStatuses { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> EventSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public string SelectedName { get => Get<string>(); set => Set(value); }
        public string SelectedHost { get => Get<string>(); set => Set(value); }
        public string SelectedTiming { get => Get<string>(); set => Set(value); }
        public string SelectedDescription { get => Get<string>(); set => Set(value); }
        public bool IsJoinEnabled { get => Get<bool>(); set => Set(value); }
        public string JoinTooltip { get => Get<string>(); set => Set(value); }
        public string JoinHintText { get => Get<string>(); set => Set(value); }
        public bool IsLeaveEnabled { get => Get<bool>(); set => Set(value); }
        public string CurrentEventText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            EnsureMinimumSize();
            _selectedEventId = string.Empty;
            StatusText = string.Empty;
            LoadEvents();
            ChangePartialView(ContentElement, MainContentPartial);
            // Values unchanged since the last open are not resent, so the new controls need them pushed again.
            RepublishBindings();
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName != nameof(Geometry) || !EnsureMinimumSize() || WindowToken <= 0)
                return;

            // A client resize can commit after its watch callback. Republish on the next tick.
            DelayCommand(0.0f, () =>
            {
                if (Gui.IsWindowOpen(Player, WindowType))
                    OnPropertyChanged(nameof(Geometry));
            });
        }

        private bool EnsureMinimumSize()
        {
            var current = Geometry;
            if (current == null || current.Width >= EventsDefinition.MinimumWindowWidth && current.Height >= EventsDefinition.MinimumWindowHeight)
                return false;

            Geometry = new GuiRectangle(
                current.X,
                current.Y,
                Math.Max(current.Width, EventsDefinition.MinimumWindowWidth),
                Math.Max(current.Height, EventsDefinition.MinimumWindowHeight));
            return true;
        }

        public void Refresh(HostedEventsChangedRefreshEvent payload)
        {
            LoadEvents();
        }

        public Action OnSelectEvent() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _eventIds.Count)
                return;

            _selectedEventId = _eventIds[index];
            StatusText = string.Empty;
            LoadEvents();
        };

        public Action OnClickJoin() => () =>
        {
            var error = HostedEvents.Join(Player, _selectedEventId);
            StatusText = string.IsNullOrEmpty(error) ? "Traveling to the event..." : error;
        };

        public Action OnClickLeave() => () =>
        {
            var error = HostedEvents.Leave(Player);
            StatusText = string.IsNullOrEmpty(error) ? "Going back to where you joined from..." : error;
        };

        private string DescribeZone()
        {
            return _zone.Id == PlayerTimeZones.UtcId
                ? "Times in UTC; change in Settings."
                : $"Times are {PlayerTimeZones.GetAbbreviation(DateTime.UtcNow, _zone)}.";
        }

        private void LoadEvents()
        {
            var now = DateTime.UtcNow;
            _zone = PlayerTimeZones.ForPlayer(Player);
            var events = HostedEvents.GetEvents();
            _eventIds = events.Select(e => e.Id).ToList();
            if (!_eventIds.Contains(_selectedEventId))
                _selectedEventId = _eventIds.FirstOrDefault() ?? string.Empty;

            var names = new GuiBindingList<string>();
            var statuses = new GuiBindingList<string>();
            var selected = new GuiBindingList<bool>();
            foreach (var hostedEvent in events)
            {
                names.Add(hostedEvent.Name);
                statuses.Add(HostedEventRules.GetListStatus(hostedEvent, now));
                selected.Add(hostedEvent.Id == _selectedEventId);
            }

            EventNames = names;
            EventStatuses = statuses;
            EventSelected = selected;

            var running = events.Count(e => e.Status == HostedEventStatus.Running);
            ListSummaryText = events.Count == 0
                ? "No events are running or scheduled right now."
                : $"{running} running, {events.Count - running} upcoming. {DescribeZone()}";

            var returnPoint = HostedEvents.GetReturnPoint(Player);
            LoadSelection(HostedEvents.GetEvent(_selectedEventId), returnPoint?.EventId ?? string.Empty, now);
            LoadCurrentEvent(returnPoint);
        }

        private void LoadSelection(HostedEvent hostedEvent, string currentEventId, DateTime now)
        {
            if (hostedEvent == null)
            {
                SelectedName = string.Empty;
                SelectedHost = string.Empty;
                SelectedTiming = string.Empty;
                SelectedDescription = string.Empty;
                IsJoinEnabled = false;
                JoinTooltip = string.Empty;
                JoinHintText = string.Empty;
                return;
            }

            SelectedName = hostedEvent.Name;
            SelectedHost = $"Hosted by {hostedEvent.HostName}";
            SelectedTiming = HostedEventRules.DescribeTiming(hostedEvent, now, _zone);
            SelectedDescription = hostedEvent.Description;
            var refusal = HostedEventRules.GetJoinRefusal(hostedEvent, new HostedEventTravelState { CurrentEventId = currentEventId });
            IsJoinEnabled = string.IsNullOrEmpty(refusal);
            JoinTooltip = IsJoinEnabled
                ? "Travel to the event. Leave Event or /leaveevent brings you back."
                : refusal;
            JoinHintText = IsJoinEnabled ? "Open to join." : JoinTooltip;
        }

        private void LoadCurrentEvent(HostedEventReturnPoint returnPoint)
        {
            IsLeaveEnabled = returnPoint != null;
            CurrentEventText = returnPoint == null
                ? "You are not at an event."
                : $"You are at {returnPoint.EventName}.";
        }
    }
}
