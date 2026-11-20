using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.InitiativeTrackerService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Sets or removes your own initiative tracker value. Hosts and DMs also add named entries for NPCs.
    /// </summary>
    public class InitiativeTrackerEditViewModel : GuiViewModelBase<InitiativeTrackerEditViewModel, GuiPayloadBase>,
        IGuiRefreshable<InitiativeTrackerRefreshEvent>
    {
        public const string MainContentElement = "initiative_tracker_edit_content";
        public const string NoTrackerPartial = "INITIATIVE_TRACKER_EDIT_NONE";
        public const string OwnPartial = "INITIATIVE_TRACKER_EDIT_OWN";
        public const string OwnAndNamedPartial = "INITIATIVE_TRACKER_EDIT_HOST";
        // DMs have no entry of their own; they add named entries for whatever they are running.
        public const string NamedPartial = "INITIATIVE_TRACKER_EDIT_DM";

        private string _currentPartial;

        public string ValueText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string NamedEntryName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string NamedEntryValue
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasOwnEntry
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _currentPartial = null;
            var entry = InitiativeTracker.GetSession(Member)?.GetEntryOf(Member);
            ValueText = entry?.Value.ToString() ?? string.Empty;
            NamedEntryName = string.Empty;
            NamedEntryValue = string.Empty;
            HasOwnEntry = entry != null;

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
            HasOwnEntry = session?.GetEntryOf(Member) != null;

            string partial;
            if (session == null)
                partial = NoTrackerPartial;
            else if (InitiativeTracker.IsDM(Member))
                partial = NamedPartial;
            else if (InitiativeTracker.IsManager(session, Member))
                partial = OwnAndNamedPartial;
            else
                partial = OwnPartial;

            if (_currentPartial == partial)
                return;

            _currentPartial = partial;
            ChangePartialView(MainContentElement, partial);

            // The client drops input values that arrive before their text edits exist, and the base class
            // does not resend them after a layout (NuiLayoutRules R7). Watching sends each value again,
            // so start watching one tick after the swap, once the controls are on screen.
            var windowToken = WindowToken;
            Scheduler.Schedule(() => SendInputs(windowToken), TimeSpan.Zero);
        }

        private void SendInputs(int windowToken)
        {
            if (WindowToken != windowToken || !Gui.IsWindowOpen(Player, GuiWindowType.InitiativeTrackerEdit))
                return;

            WatchOnClient(model => model.ValueText);
            WatchOnClient(model => model.NamedEntryName);
            WatchOnClient(model => model.NamedEntryValue);
        }

        private bool TryParseValue(string text, out int value)
        {
            if (int.TryParse(text?.Trim(), out value) &&
                value >= InitiativeTrackerSession.MinValue &&
                value <= InitiativeTrackerSession.MaxValue)
                return true;

            DMPresence.SendMessage(Player, ColorToken.Red($"Enter a whole number from {InitiativeTrackerSession.MinValue} to {InitiativeTrackerSession.MaxValue}."));
            return false;
        }

        public Action OnClickSet() => () =>
        {
            if (TryParseValue(ValueText, out var value))
                InitiativeTracker.SetOwnEntry(Member, value);
        };

        public Action OnClickRoll() => () =>
        {
            InitiativeTracker.RollOwnEntry(Member);
            var entry = InitiativeTracker.GetSession(Member)?.GetEntryOf(Member);
            if (entry != null)
                ValueText = entry.Value.ToString();
        };

        public Action OnClickRemove() => () => InitiativeTracker.RemoveOwnEntry(Member);

        public Action OnClickAddNamed() => () =>
        {
            if (string.IsNullOrWhiteSpace(NamedEntryName))
            {
                DMPresence.SendMessage(Player, ColorToken.Red("Enter a name for the entry."));
                return;
            }

            if (!TryParseValue(NamedEntryValue, out var value))
                return;

            InitiativeTracker.AddNamedEntry(Member, NamedEntryName, value);
            NamedEntryName = string.Empty;
            NamedEntryValue = string.Empty;
        };
    }
}
