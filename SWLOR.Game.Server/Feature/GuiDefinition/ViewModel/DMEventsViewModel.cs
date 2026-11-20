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
    public class DMEventsViewModel : GuiViewModelBase<DMEventsViewModel, GuiPayloadBase>,
        IGuiRefreshable<HostedEventsChangedRefreshEvent>
    {
        public const string ContentElement = "dm_events_content";
        public const string MainContentPartial = "DM_EVENTS_MAIN";

        private List<string> _eventIds = new();
        private string _selectedEventId = string.Empty;
        // The zone the start time box is typed in: the DM's Settings choice when the form was loaded.
        private PlayerTimeZone _formZone = PlayerTimeZones.Utc;

        // The instant the start text was last filled in from. Saving or a zone change keeps it while the DM leaves the
        // text alone, since a repeated hour reads back as its first copy.
        private DateTime? _formStartUtc;

        public GuiBindingList<string> EventNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> EventSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string EventName { get => Get<string>(); set => Set(value); }
        public string HostName { get => Get<string>(); set => Set(value); }
        public string StartText { get => Get<string>(); set => Set(value); }
        public string ServerTimeText { get => Get<string>(); set => Set(value); }
        public string Description { get => Get<string>(); set => Set(value); }
        public bool IsJoinOpen { get => Get<bool>(); set => Set(value); }
        public bool AnnounceOnStart { get => Get<bool>(); set => Set(value); }
        public string ArrivalText { get => Get<string>(); set => Set(value); }
        public string SummaryText { get => Get<string>(); set => Set(value); }
        public string EndText { get => Get<string>(); set => Set(value); }
        public bool IsSaved { get => Get<bool>(); set => Set(value); }
        // False while Discord owns the name, host, time and description.
        public bool IsDetailsEditable { get => Get<bool>(); set => Set(value); }
        public bool HasArrival { get => Get<bool>(); set => Set(value); }
        public bool IsStartEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsSendBackEnabled { get => Get<bool>(); set => Set(value); }
        public string FeedbackText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            EnsureMinimumSize();
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            _selectedEventId = string.Empty;
            FeedbackText = string.Empty;
            LoadForm(null);
            LoadEvents();

            WatchOnClient(model => model.EventName);
            WatchOnClient(model => model.HostName);
            WatchOnClient(model => model.StartText);
            WatchOnClient(model => model.Description);
            WatchOnClient(model => model.IsJoinOpen);
            WatchOnClient(model => model.AnnounceOnStart);

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
            if (current == null || current.Width >= DMEventsDefinition.MinimumWindowWidth && current.Height >= DMEventsDefinition.MinimumWindowHeight)
                return false;

            Geometry = new GuiRectangle(
                current.X,
                current.Y,
                Math.Max(current.Width, DMEventsDefinition.MinimumWindowWidth),
                Math.Max(current.Height, DMEventsDefinition.MinimumWindowHeight));
            return true;
        }

        // Keeps the DM's unsaved typing; only the list and the event's state are reloaded.
        public void Refresh(HostedEventsChangedRefreshEvent payload)
        {
            var selected = HostedEvents.GetEvent(_selectedEventId);
            if (!string.IsNullOrEmpty(_selectedEventId) && selected == null)
            {
                _selectedEventId = string.Empty;
                LoadForm(null);
            }
            else if (DiscordEventImport.IsSyncedFromDiscord(selected))
            {
                // The details cannot be typed in, so show Discord's latest. The options keep the DM's unsaved choices.
                LoadDetails(selected);
            }

            FollowTimeZoneChange();
            LoadEvents();
        }

        // The DM picked another time zone in Settings: show the typed start time on the new zone's clock.
        private void FollowTimeZoneChange()
        {
            var zone = PlayerTimeZones.ForPlayer(Player);
            if (zone.Id == _formZone.Id)
                return;

            if (HostedEventRules.TryParseLocalTime(StartText, out var local) &&
                PlayerTimeZones.TryToUtc(local, _formZone, out var startUtc))
            {
                startUtc = HostedEventRules.GetStartToSave(_formStartUtc, StartText, _formZone, startUtc);
                ShowStart(startUtc, zone);
            }

            _formZone = zone;
        }

        public Action OnSelectEvent() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _eventIds.Count)
                return;

            _selectedEventId = _eventIds[index];
            FeedbackText = string.Empty;
            LoadForm(HostedEvents.GetEvent(_selectedEventId));
            LoadEvents();
        };

        public Action OnClickNew() => () =>
        {
            _selectedEventId = string.Empty;
            FeedbackText = string.Empty;
            LoadForm(null);
            LoadEvents();
        };

        public Action OnClickSave() => () =>
        {
            if (TrySave())
                FeedbackText = "Saved.";
        };

        public Action OnClickStart() => () =>
        {
            if (!TrySave())
                return;

            var hostedEvent = HostedEvents.GetEvent(_selectedEventId);
            var joinNote = HostedEventRules.IsOpenToJoin(hostedEvent)
                ? "Players can join from /events."
                : "Players cannot join from /events until you set an arrival point and allow joining.";
            ShowModal($"Start {hostedEvent.Name} now? {joinNote}", () =>
            {
                var error = HostedEvents.Start(Player, _selectedEventId);
                FeedbackText = string.IsNullOrEmpty(error) ? "The event is running." : error;
            });
        };

        public Action OnClickEnd() => () =>
        {
            var hostedEvent = HostedEvents.GetEvent(_selectedEventId);
            if (hostedEvent == null)
                return;

            var prompt = hostedEvent.Status == HostedEventStatus.Running
                ? $"End {hostedEvent.Name}? Players there stay where they are and can use /leaveevent to go back."
                : DiscordEventImport.IsSyncedFromDiscord(hostedEvent)
                    ? $"Cancel {hostedEvent.Name}? It leaves /events until Discord gives it a new time."
                    : $"Cancel {hostedEvent.Name}? It is removed from /events.";
            ShowModal(prompt, () =>
            {
                var error = HostedEvents.End(Player, _selectedEventId);
                FeedbackText = string.IsNullOrEmpty(error) ? "The event was removed." : error;
            });
        };

        public Action OnClickSendBack() => () =>
        {
            var hostedEvent = HostedEvents.GetEvent(_selectedEventId);
            if (hostedEvent == null)
                return;

            var (_, online) = HostedEvents.CountParticipants(hostedEvent);
            ShowModal($"Send {online} online {(online == 1 ? "player" : "players")} at {hostedEvent.Name} back to where they joined from?", () =>
            {
                FeedbackText = HostedEvents.SendEveryoneBack(Player, _selectedEventId);
            });
        };

        public Action OnClickSetArrival() => () =>
        {
            var error = HostedEvents.SetArrivalPoint(Player, _selectedEventId);
            FeedbackText = string.IsNullOrEmpty(error) ? "Players who join arrive where you are standing." : error;
        };

        public Action OnClickClearArrival() => () =>
        {
            var error = HostedEvents.ClearArrivalPoint(Player, _selectedEventId);
            FeedbackText = string.IsNullOrEmpty(error) ? "Arrival point cleared." : error;
        };

        public Action OnClickGoToArrival() => () =>
        {
            var error = HostedEvents.GoToArrivalPoint(Player, _selectedEventId);
            if (!string.IsNullOrEmpty(error))
                FeedbackText = error;
        };

        private bool TrySave()
        {
            var error = HostedEvents.SaveDetails(
                Player,
                _selectedEventId,
                EventName,
                HostName,
                Description,
                StartText,
                _formStartUtc,
                _formZone,
                IsJoinOpen,
                AnnounceOnStart,
                out var saved);
            if (!string.IsNullOrEmpty(error))
            {
                FeedbackText = error;
                return false;
            }

            _selectedEventId = saved.Id;
            HostName = saved.HostName;
            ShowStart(saved.ScheduledStartUtc, _formZone);
            LoadEvents();
            return true;
        }

        private void ShowStart(DateTime startUtc, PlayerTimeZone zone)
        {
            _formStartUtc = startUtc;
            StartText = HostedEventRules.FormatStartTime(startUtc, zone);
        }

        private void LoadForm(HostedEvent hostedEvent)
        {
            _formZone = PlayerTimeZones.ForPlayer(Player);
            if (hostedEvent == null)
            {
                EventName = string.Empty;
                HostName = GetName(DMPresence.GetDM(Player));
                ShowStart(HostedEventRules.GetDefaultStartTime(DateTime.UtcNow, _formZone), _formZone);
                Description = string.Empty;
                IsJoinOpen = true;
                AnnounceOnStart = true;
                return;
            }

            LoadDetails(hostedEvent);
            IsJoinOpen = hostedEvent.IsJoinOpen;
            AnnounceOnStart = hostedEvent.AnnounceOnStart;
        }

        private void LoadDetails(HostedEvent hostedEvent)
        {
            EventName = hostedEvent.Name;
            HostName = hostedEvent.HostName;
            ShowStart(hostedEvent.ScheduledStartUtc, _formZone);
            Description = hostedEvent.Description;
        }

        private void LoadEvents()
        {
            var now = DateTime.UtcNow;
            var events = HostedEvents.GetEvents();
            _eventIds = events.Select(e => e.Id).ToList();

            var names = new GuiBindingList<string>();
            var selected = new GuiBindingList<bool>();
            foreach (var hostedEvent in events)
            {
                var marker = hostedEvent.Status == HostedEventStatus.Running
                    ? "[Live] "
                    : DiscordEventImport.IsSyncedFromDiscord(hostedEvent) ? "[Discord] " : string.Empty;
                names.Add(marker + hostedEvent.Name);
                selected.Add(hostedEvent.Id == _selectedEventId);
            }

            EventNames = names;
            EventSelected = selected;
            ServerTimeText = $"Now: {HostedEventRules.FormatZoneTime(now, _formZone)}";
            LoadState(HostedEvents.GetEvent(_selectedEventId), now);
        }

        private void LoadState(HostedEvent hostedEvent, DateTime now)
        {
            IsSaved = hostedEvent != null;
            IsDetailsEditable = !DiscordEventImport.IsSyncedFromDiscord(hostedEvent);
            if (hostedEvent == null)
            {
                HeaderText = "New event. Save it, then set the arrival point.";
                ArrivalText = "Arrival point: save the event first.";
                SummaryText = string.Empty;
                EndText = "Cancel Event";
                HasArrival = false;
                IsStartEnabled = true;
                IsSendBackEnabled = false;
                return;
            }

            var isRunning = hostedEvent.Status == HostedEventStatus.Running;
            var (total, online) = HostedEvents.CountParticipants(hostedEvent);
            var areaName = HostedEvents.GetArrivalAreaName(hostedEvent);

            HeaderText = HostedEventRules.DescribeTiming(hostedEvent, now, _formZone);
            HasArrival = areaName.Length > 0;
            ArrivalText = HasArrival
                ? $"Arrival point: {areaName}"
                : "Arrival point: none. Players cannot join until you set one.";
            SummaryText = DiscordEventImport.IsSyncedFromDiscord(hostedEvent)
                ? "Listed from Discord. Change its details there."
                : $"Players at the event: {total} ({online} online).";
            EndText = isRunning ? "End Event" : "Cancel Event";
            IsStartEnabled = !isRunning;
            IsSendBackEnabled = isRunning && online > 0;
        }
    }
}
