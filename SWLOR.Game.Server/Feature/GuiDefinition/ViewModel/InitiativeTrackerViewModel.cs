using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.InitiativeTrackerService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The initiative tracker: lists the trackers in the area to join or start, then the joined tracker's entries
    /// with the turn marker. Names resolve for each viewer the way the rest of the game shows them.
    /// </summary>
    public class InitiativeTrackerViewModel : GuiViewModelBase<InitiativeTrackerViewModel, GuiPayloadBase>,
        IGuiRefreshable<InitiativeTrackerRefreshEvent>
    {
        public const string MainContentElement = "initiative_tracker_content";
        public const string BrowsePartial = "INITIATIVE_TRACKER_BROWSE";
        public const string TrackerPartial = "INITIATIVE_TRACKER_LIST";
        // Hosts and DMs get a remove button on each row and a Reset button.
        public const string ManagerPartial = "INITIATIVE_TRACKER_MANAGE";

        // Characters that fit the name column beside the value and remove columns.
        private const int NameLength = 24;

        private static readonly GuiColor NormalValueColor = new(255, 255, 255);
        private static readonly GuiColor CurrentTurnColor = new(255, 90, 70);

        private readonly List<int> _sessionIds = new();
        private readonly List<int> _entryIds = new();
        private string _currentPartial;

        public GuiBindingList<string> SessionHostNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> SessionTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string BrowseStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HostText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> EntryValues
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> EntryValueColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> EntryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EntryNameTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string HostTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EmptyListText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StepText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CanMoveTurn
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _currentPartial = null;
            SessionHostNames = new GuiBindingList<string>();
            SessionTooltips = new GuiBindingList<string>();
            BrowseStatusText = string.Empty;
            HostText = string.Empty;
            HostTooltip = string.Empty;
            EntryValues = new GuiBindingList<string>();
            EntryValueColors = new GuiBindingList<GuiColor>();
            EntryNames = new GuiBindingList<string>();
            EntryNameTooltips = new GuiBindingList<string>();
            EmptyListText = string.Empty;
            StepText = InitiativeTrackerSession.MinStep.ToString();
            CanMoveTurn = false;

            Load();
        }

        public void Refresh(InitiativeTrackerRefreshEvent payload)
        {
            Load();
        }

        private uint Member => InitiativeTracker.GetMember(Player);

        private void Load()
        {
            var session = InitiativeTracker.GetSession(Member);
            if (session == null)
            {
                LoadBrowse();
                ShowPartial(BrowsePartial);
            }
            else
            {
                LoadTracker(session);
                ShowPartial(InitiativeTracker.IsManager(session, Member) ? ManagerPartial : TrackerPartial);
            }
        }

        private void ShowPartial(string partial)
        {
            if (_currentPartial == partial)
                return;

            _currentPartial = partial;
            ChangePartialView(MainContentElement, partial);
        }

        private void LoadBrowse()
        {
            var sessions = InitiativeTracker.GetSessionsInArea(InitiativeTracker.GetActingArea(Member));
            var hostNames = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            _sessionIds.Clear();

            foreach (var session in sessions)
            {
                var hostName = GetMemberName(session.Host);
                var count = session.Entries.Count == 1 ? "1 entry" : $"{session.Entries.Count} entries";
                _sessionIds.Add(session.Id);
                hostNames.Add($"{Shorten(hostName)} ({session.Entries.Count})");
                tooltips.Add($"Hosted by {hostName}, {count}");
            }

            SessionHostNames = hostNames;
            SessionTooltips = tooltips;
            BrowseStatusText = sessions.Count == 0
                ? "No trackers in this area yet."
                : "Join a tracker, or start your own.";
        }

        private void LoadTracker(InitiativeTrackerSession session)
        {
            var values = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var names = new GuiBindingList<string>();
            var nameTooltips = new GuiBindingList<string>();
            _entryIds.Clear();

            foreach (var entry in session.Entries)
            {
                _entryIds.Add(entry.Id);
                values.Add(entry.Value.ToString());
                colors.Add(entry.Id == session.CurrentEntryId ? CurrentTurnColor : NormalValueColor);
                var name = entry.IsNamed ? entry.Name : GetMemberName(entry.Owner);
                names.Add(Shorten(name));
                nameTooltips.Add(name);
            }

            var host = GetMemberName(session.Host);
            HostText = "Host: " + Shorten(host);
            HostTooltip = host;
            EntryValues = values;
            EntryValueColors = colors;
            EntryNames = names;
            EntryNameTooltips = nameTooltips;
            EmptyListText = session.Entries.Count == 0 ? "No entries yet. Press Edit to add yours." : string.Empty;
            StepText = session.Step.ToString();
            CanMoveTurn = InitiativeTracker.CanMoveTurn(session, Member);
        }

        // Labels draw past their cell, so long names are cut to fit; tooltips show them in full.
        private static string Shorten(string name)
        {
            return name.Length <= NameLength ? name : name[..(NameLength - 3)] + "...";
        }

        // Players see only the names they know, never a DM's; staff see canonical names.
        private string GetMemberName(uint member)
        {
            if (InitiativeTracker.IsDM(member) && !InitiativeTracker.IsDM(Member))
                return PlayerList.DMDescriptor;

            return UtilPlugin.StripColors(PlayerName.GetDisplayName(Player, member));
        }

        public Action OnClickStart() => () => InitiativeTracker.Start(Member);

        public Action OnClickJoin() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _sessionIds.Count)
                return;

            InitiativeTracker.Join(Member, _sessionIds[index]);
        };

        public Action OnClickLeave() => () => InitiativeTracker.Leave(Member);

        // No confirmation modal: a refresh from another member would swap the content under it.
        public Action OnClickReset() => () => InitiativeTracker.Reset(Member);

        public Action OnClickRemoveEntry() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _entryIds.Count)
                return;

            InitiativeTracker.RemoveEntry(Member, _entryIds[index]);
        };

        public Action OnClickPrevious() => () => InitiativeTracker.MoveTurn(Member, false);

        public Action OnClickNext() => () => InitiativeTracker.MoveTurn(Member, true);

        public Action OnClickDecreaseStep() => () => InitiativeTracker.ChangeStep(Member, -1);

        public Action OnClickIncreaseStep() => () => InitiativeTracker.ChangeStep(Member, 1);

        public Action OnClickEdit() => () => InitiativeTracker.ToggleWindow(Player, GuiWindowType.InitiativeTrackerEdit);
    }
}
