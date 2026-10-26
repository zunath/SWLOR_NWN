using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PlayerListViewModel : GuiViewModelBase<PlayerListViewModel, GuiPayloadBase>,
        IGuiRefreshable<PlayerListRefreshEvent>
    {
        public const string MainContentElement = "playerlist_content";
        public const string MainContentPartial = "PLAYERLIST_MAIN";
        // DMs get a layout with their own action row instead of a hidden row that would leave players a gap.
        public const string DMContentPartial = "PLAYERLIST_DM";

        // Characters that fit the 140 px name column and the descriptor column; tooltips show full values.
        private const int NameColumnLength = 17;
        private const int DescriptorColumnLength = 24;

        private static readonly GuiColor DescriptorColor = new(180, 180, 180);
        private static readonly GuiColor DisguisedDescriptorColor = new(255, 127, 0);
        private static readonly Dictionary<PlayerListStatus, (string Text, GuiColor Color)> StatusDisplay = new()
        {
            [PlayerListStatus.None] = (string.Empty, new GuiColor(255, 255, 255)),
            [PlayerListStatus.Leader] = ("Leader", new GuiColor(221, 181, 93)),
            [PlayerListStatus.Party] = ("Party", new GuiColor(0, 200, 0)),
            [PlayerListStatus.Grouped] = ("Grouped", new GuiColor(150, 150, 150)),
            [PlayerListStatus.Hostile] = ("Hostile", new GuiColor(230, 60, 60)),
            [PlayerListStatus.DM] = ("DM", new GuiColor(100, 180, 255)),
        };

        private List<PlayerListEntry> _entries = new();
        private readonly List<PlayerListEntry> _visibleEntries = new();
        private uint _selectedPlayer = OBJECT_INVALID;
        private bool _isInitialized;

        public bool IsGeneralRPFlagged { get => Get<bool>(); set => Set(value); }
        public bool IsPvPFlagged { get => Get<bool>(); set => Set(value); }
        public bool IsDMFlagged { get => Get<bool>(); set => Set(value); }
        public bool IsCraftingFlagged { get => Get<bool>(); set => Set(value); }
        public bool CanSetFlags { get => Get<bool>(); set => Set(value); }

        public string SearchText
        {
            get => Get<string>();
            set
            {
                Set(value);
                if (_isInitialized)
                    ShowRows();
            }
        }

        public GuiBindingList<string> RowStatuses { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RowStatusColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> RowNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowNameTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> RowSelections { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> RowDescriptors { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RowDescriptorColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> RowDescriptorTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> RowShowsGeneralRP { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> RowShowsPvP { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> RowShowsDM { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> RowShowsCrafting { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string SelectedText { get => Get<string>(); set => Set(value); }
        public string OnlineSummary { get => Get<string>(); set => Set(value); }

        public bool CanSendTell { get => Get<bool>(); set => Set(value); }
        public string SendTellReason { get => Get<string>(); set => Set(value); }
        public bool CanToggleHostile { get => Get<bool>(); set => Set(value); }
        public string ToggleHostileReason { get => Get<string>(); set => Set(value); }
        public string ToggleHostileText { get => Get<string>(); set => Set(value); }
        public bool CanCall { get => Get<bool>(); set => Set(value); }
        public string CallReason { get => Get<string>(); set => Set(value); }
        public bool CanAnswerCall { get => Get<bool>(); set => Set(value); }
        public string AnswerCallReason { get => Get<string>(); set => Set(value); }
        public bool CanDeclineCall { get => Get<bool>(); set => Set(value); }
        public string DeclineCallReason { get => Get<string>(); set => Set(value); }
        public bool CanEndCall { get => Get<bool>(); set => Set(value); }
        public string EndCallReason { get => Get<string>(); set => Set(value); }
        public string EndCallText { get => Get<string>(); set => Set(value); }
        public bool CanInvite { get => Get<bool>(); set => Set(value); }
        public string InviteReason { get => Get<string>(); set => Set(value); }
        public bool CanRemove { get => Get<bool>(); set => Set(value); }
        public string RemoveReason { get => Get<string>(); set => Set(value); }
        public bool CanMakeLeader { get => Get<bool>(); set => Set(value); }
        public string MakeLeaderReason { get => Get<string>(); set => Set(value); }
        public bool CanLeaveParty { get => Get<bool>(); set => Set(value); }
        public string LeavePartyReason { get => Get<string>(); set => Set(value); }
        public bool IsDMViewer { get => Get<bool>(); set => Set(value); }
        public bool CanJumpTo { get => Get<bool>(); set => Set(value); }
        public string JumpToReason { get => Get<string>(); set => Set(value); }
        public bool CanJumpHere { get => Get<bool>(); set => Set(value); }
        public string JumpHereReason { get => Get<string>(); set => Set(value); }
        public bool CanExamine { get => Get<bool>(); set => Set(value); }
        public string ExamineReason { get => Get<string>(); set => Set(value); }
        public bool CanBoot { get => Get<bool>(); set => Set(value); }
        public string BootReason { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isInitialized = false;
            _selectedPlayer = OBJECT_INVALID;
            if (DMPresence.IsControlledByDM(Player))
                Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);

            IsGeneralRPFlagged = false;
            IsPvPFlagged = false;
            IsDMFlagged = false;
            IsCraftingFlagged = false;
            CanSetFlags = false;
            SearchText = string.Empty;
            RowStatuses = new GuiBindingList<string>();
            RowStatusColors = new GuiBindingList<GuiColor>();
            RowNames = new GuiBindingList<string>();
            RowNameTooltips = new GuiBindingList<string>();
            RowSelections = new GuiBindingList<bool>();
            RowDescriptors = new GuiBindingList<string>();
            RowDescriptorColors = new GuiBindingList<GuiColor>();
            RowDescriptorTooltips = new GuiBindingList<string>();
            RowShowsGeneralRP = new GuiBindingList<bool>();
            RowShowsPvP = new GuiBindingList<bool>();
            RowShowsDM = new GuiBindingList<bool>();
            RowShowsCrafting = new GuiBindingList<bool>();
            SelectedText = string.Empty;
            OnlineSummary = string.Empty;
            CanSendTell = false;
            SendTellReason = string.Empty;
            CanToggleHostile = false;
            ToggleHostileReason = string.Empty;
            ToggleHostileText = "Mark Hostile";
            CanCall = false;
            CallReason = string.Empty;
            CanAnswerCall = false;
            AnswerCallReason = string.Empty;
            CanDeclineCall = false;
            DeclineCallReason = string.Empty;
            CanEndCall = false;
            EndCallReason = string.Empty;
            EndCallText = "End Call";
            CanInvite = false;
            InviteReason = string.Empty;
            CanRemove = false;
            RemoveReason = string.Empty;
            CanMakeLeader = false;
            MakeLeaderReason = string.Empty;
            CanLeaveParty = false;
            LeavePartyReason = string.Empty;
            IsDMViewer = DMPresence.IsControlledByDM(Player);
            CanJumpTo = false;
            JumpToReason = string.Empty;
            CanJumpHere = false;
            JumpHereReason = string.Empty;
            CanExamine = false;
            ExamineReason = string.Empty;
            CanBoot = false;
            BootReason = string.Empty;

            WatchOnClient(model => model.SearchText);

            ChangePartialView(MainContentElement, IsDMViewer ? DMContentPartial : MainContentPartial);

            _isInitialized = true;
            LoadEntries(PlayerList.BuildSubjects());
        }

        public void Refresh(PlayerListRefreshEvent payload)
        {
            if (!_isInitialized)
                return;

            LoadEntries(payload.Subjects);
        }

        private void LoadEntries(IReadOnlyList<PlayerListSubject> subjects)
        {
            _entries = PlayerList.BuildEntries(Player, subjects);
            LoadOwnFlags();
            ShowRows();
        }

        private void LoadOwnFlags()
        {
            var flags = PlayerList.GetFlags(Player);
            IsGeneralRPFlagged = flags.Contains(PlayerListFlagType.GeneralRP);
            IsPvPFlagged = flags.Contains(PlayerListFlagType.PvP);
            IsDMFlagged = flags.Contains(PlayerListFlagType.DM);
            IsCraftingFlagged = flags.Contains(PlayerListFlagType.Crafting);
            CanSetFlags = !DMPresence.IsControlledByDM(Player);
        }

        private void ShowRows()
        {
            _visibleEntries.Clear();
            _visibleEntries.AddRange(_entries.Where(entry =>
                PlayerListRules.MatchesSearch(SearchText, entry.Name, entry.Descriptor)));

            var statuses = new GuiBindingList<string>();
            var statusColors = new GuiBindingList<GuiColor>();
            var names = new GuiBindingList<string>();
            var nameTooltips = new GuiBindingList<string>();
            var selections = new GuiBindingList<bool>();
            var descriptors = new GuiBindingList<string>();
            var descriptorColors = new GuiBindingList<GuiColor>();
            var descriptorTooltips = new GuiBindingList<string>();
            var showsGeneralRP = new GuiBindingList<bool>();
            var showsPvP = new GuiBindingList<bool>();
            var showsDM = new GuiBindingList<bool>();
            var showsCrafting = new GuiBindingList<bool>();

            foreach (var entry in _visibleEntries)
            {
                var status = StatusDisplay[entry.Status];
                var name = GetDisplayName(entry);

                statuses.Add(status.Text);
                statusColors.Add(status.Color);
                names.Add(PlayerListRules.GetRowName(entry.Name, entry.Relation.TargetIsSelf, entry.IsAfk, NameColumnLength));
                nameTooltips.Add(string.IsNullOrEmpty(entry.Name)
                    ? "You have not given this player a name. Use /name to give them one."
                    : name);
                selections.Add(entry.Player == _selectedPlayer);
                descriptors.Add(DMListText.Fit(entry.Descriptor, DescriptorColumnLength));
                descriptorColors.Add(entry.IsDisguised ? DisguisedDescriptorColor : DescriptorColor);
                descriptorTooltips.Add(entry.IsDisguised ? $"{entry.Descriptor} (disguised)" : entry.Descriptor);
                showsGeneralRP.Add(entry.Flags.Contains(PlayerListFlagType.GeneralRP));
                showsPvP.Add(entry.Flags.Contains(PlayerListFlagType.PvP));
                showsDM.Add(entry.Flags.Contains(PlayerListFlagType.DM));
                showsCrafting.Add(entry.Flags.Contains(PlayerListFlagType.Crafting));
            }

            RowStatuses = statuses;
            RowStatusColors = statusColors;
            RowNames = names;
            RowNameTooltips = nameTooltips;
            RowSelections = selections;
            RowDescriptors = descriptors;
            RowDescriptorColors = descriptorColors;
            RowDescriptorTooltips = descriptorTooltips;
            RowShowsGeneralRP = showsGeneralRP;
            RowShowsPvP = showsPvP;
            RowShowsDM = showsDM;
            RowShowsCrafting = showsCrafting;

            OnlineSummary = _visibleEntries.Count == _entries.Count
                ? $"{_entries.Count} online"
                : $"{_visibleEntries.Count} of {_entries.Count} online";

            if (_visibleEntries.All(entry => entry.Player != _selectedPlayer))
                _selectedPlayer = OBJECT_INVALID;

            ShowActions();
        }

        private void ShowActions()
        {
            var selected = GetSelectedEntry();
            SelectedText = selected == null
                ? "No player selected"
                : $"Selected: {GetDisplayName(selected)}";

            var actions = PlayerListRules.Evaluate(selected?.Relation ?? GetViewerRelation());
            CanSendTell = actions.SendTell.IsEnabled;
            SendTellReason = actions.SendTell.DisabledReason;
            CanToggleHostile = actions.ToggleHostile.IsEnabled;
            ToggleHostileReason = actions.ToggleHostile.DisabledReason;
            ToggleHostileText = actions.ToggleHostileText;
            CanCall = actions.Call.IsEnabled;
            CallReason = actions.Call.DisabledReason;
            CanAnswerCall = actions.AnswerCall.IsEnabled;
            AnswerCallReason = actions.AnswerCall.DisabledReason;
            CanDeclineCall = actions.DeclineCall.IsEnabled;
            DeclineCallReason = actions.DeclineCall.DisabledReason;
            CanEndCall = actions.EndCall.IsEnabled;
            EndCallReason = actions.EndCall.DisabledReason;
            EndCallText = actions.EndCallText;
            CanInvite = actions.Invite.IsEnabled;
            InviteReason = actions.Invite.DisabledReason;
            CanRemove = actions.Remove.IsEnabled;
            RemoveReason = actions.Remove.DisabledReason;
            CanMakeLeader = actions.MakeLeader.IsEnabled;
            MakeLeaderReason = actions.MakeLeader.DisabledReason;
            CanLeaveParty = actions.LeaveParty.IsEnabled;
            LeavePartyReason = actions.LeaveParty.DisabledReason;
            CanJumpTo = actions.JumpTo.IsEnabled;
            JumpToReason = actions.JumpTo.DisabledReason;
            CanJumpHere = actions.JumpHere.IsEnabled;
            JumpHereReason = actions.JumpHere.DisabledReason;
            CanExamine = actions.Examine.IsEnabled;
            ExamineReason = actions.Examine.DisabledReason;
            CanBoot = actions.Boot.IsEnabled;
            BootReason = actions.Boot.DisabledReason;
        }

        // With nothing selected, only the viewer's own party state matters (for Leave Party).
        private PlayerListRelation GetViewerRelation()
        {
            var self = _entries.FirstOrDefault(entry => entry.Relation.TargetIsSelf)?.Relation;
            return new PlayerListRelation
            {
                ViewerIsDM = DMPresence.IsControlledByDM(Player),
                ViewerIsInParty = self?.ViewerIsInParty ?? false,
                ViewerIsPartyLeader = self?.ViewerIsPartyLeader ?? false,
                ViewerIsInSpace = self?.ViewerIsInSpace ?? false,
                ViewerCallState = self?.ViewerCallState ?? PlayerListCallState.None,
            };
        }

        private PlayerListEntry GetSelectedEntry()
        {
            return _visibleEntries.FirstOrDefault(entry => entry.Player == _selectedPlayer);
        }

        private static string GetDisplayName(PlayerListEntry entry)
        {
            return PlayerListRules.GetRowName(entry.Name, entry.Relation.TargetIsSelf, entry.IsAfk);
        }

        // How the selected player is referred to in prompts and feedback: their name, or their descriptor.
        private static string GetReference(PlayerListEntry entry)
        {
            return string.IsNullOrEmpty(entry.Name) ? entry.Descriptor : entry.Name;
        }

        public Action OnSelectRow() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _visibleEntries.Count)
                return;

            var previous = _visibleEntries.FindIndex(entry => entry.Player == _selectedPlayer);
            if (previous >= 0 && previous < RowSelections.Count)
                RowSelections[previous] = false;

            _selectedPlayer = _visibleEntries[index].Player;
            RowSelections[index] = true;
            ShowActions();
        };

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
        };

        public Action OnClickToggleGeneralRP() => () => ToggleFlag(PlayerListFlagType.GeneralRP);
        public Action OnClickTogglePvP() => () => ToggleFlag(PlayerListFlagType.PvP);
        public Action OnClickToggleDM() => () => ToggleFlag(PlayerListFlagType.DM);
        public Action OnClickToggleCrafting() => () => ToggleFlag(PlayerListFlagType.Crafting);

        private void ToggleFlag(PlayerListFlagType flag)
        {
            var isFlagged = PlayerList.GetFlags(Player).Contains(flag);
            PlayerList.SetFlag(Player, flag, !isFlagged);
            LoadOwnFlags();
        }

        public Action OnClickSendTell() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.SendTell))
                return;

            var target = selected.Player;
            ShowInputModal($"Send a tell to {GetReference(selected)}:", string.Empty, () =>
            {
                var error = PlayerList.SendTell(Player, target, ModalInputText);
                if (!string.IsNullOrEmpty(error))
                    DMPresence.SendMessage(Player, ColorToken.Red(error));
            });
        };

        public Action OnClickCall() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.Call))
                return;

            HoloCom.StartCall(Player, selected.Player);
        };

        public Action OnClickAnswerCall() => () =>
        {
            if (!CheckViewerAction(actions => actions.AnswerCall))
                return;

            HoloCom.AnswerCall(Player);
        };

        public Action OnClickDeclineCall() => () =>
        {
            if (!CheckViewerAction(actions => actions.DeclineCall))
                return;

            HoloCom.DeclineCall(Player);
        };

        public Action OnClickEndCall() => () =>
        {
            if (!CheckViewerAction(actions => actions.EndCall))
                return;

            HoloCom.EndCall(Player);
        };

        public Action OnClickToggleHostile() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.ToggleHostile))
                return;

            var isHostile = !PlayerList.IsHostileTo(Player, selected.Player);
            PlayerList.SetHostile(Player, selected.Player, isHostile);
            DMPresence.SendMessage(Player, isHostile
                ? $"You marked {GetReference(selected)} as hostile."
                : $"You no longer mark {GetReference(selected)} as hostile.");
        };

        public Action OnClickJumpTo() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.JumpTo))
                return;

            PlayerList.JumpTo(Player, selected.Player);
        };

        public Action OnClickJumpHere() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.JumpHere))
                return;

            var error = DMPresence.Summon(Player, selected.Player);
            if (!string.IsNullOrEmpty(error))
                DMPresence.SendMessage(Player, ColorToken.Red(error));
        };

        public Action OnClickExamine() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.Examine))
                return;

            DMPresence.CloseWindow(Player, GuiWindowType.DMPlayerExamine);
            DMPresence.ToggleWindow(Player, GuiWindowType.DMPlayerExamine, new DMPlayerExaminePayload(selected.Player));
        };

        public Action OnClickBoot() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.Boot))
                return;

            var target = selected.Player;
            var targetId = GetObjectUUID(target);
            ShowModal($"Disconnect {GetReference(selected)} ({GetPCPlayerName(target)}) from the server?", () =>
            {
                // The player may log off while the confirmation is open, and their object ID can be reused.
                if (!GetIsObjectValid(target) || GetObjectUUID(target) != targetId)
                {
                    DMPresence.SendMessage(Player, ColorToken.Red("That player is no longer online."));
                    return;
                }

                var state = PlayerListRules.Evaluate(PlayerList.GetRelation(Player, target)).Boot;
                if (!state.IsEnabled)
                {
                    DMPresence.SendMessage(Player, ColorToken.Red(state.DisabledReason));
                    return;
                }

                DMPresence.Boot(Player, target);
            });
        };

        public Action OnClickInvite() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.Invite))
                return;

            PlayerList.Invite(Player, selected.Player);
        };

        public Action OnClickRemove() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.Remove))
                return;

            PlayerList.RemoveFromParty(selected.Player);
        };

        public Action OnClickMakeLeader() => () =>
        {
            var selected = GetSelectedEntry();
            if (selected == null || !CheckAction(actions => actions.MakeLeader))
                return;

            if (!PlayerList.MakeLeader(Player, selected.Player))
                DMPresence.SendMessage(Player, ColorToken.Red("Party leadership could not be transferred."));
        };

        public Action OnClickLeaveParty() => () =>
        {
            var relation = PlayerList.GetRelation(Player, Player);
            relation.HasTarget = false;
            var state = PlayerListRules.Evaluate(relation).LeaveParty;
            if (!state.IsEnabled)
            {
                DMPresence.SendMessage(Player, ColorToken.Red(state.DisabledReason));
                PlayerList.QueueRefresh();
                return;
            }

            PlayerList.RemoveFromParty(Player);
        };

        // Rechecks an action that needs no selection, such as answering a call.
        private bool CheckViewerAction(Func<PlayerListActions, PlayerListActionState> getState)
        {
            var relation = PlayerList.GetRelation(Player, Player);
            relation.HasTarget = false;
            var state = getState(PlayerListRules.Evaluate(relation));
            if (state.IsEnabled)
                return true;

            DMPresence.SendMessage(Player, ColorToken.Red(state.DisabledReason));
            PlayerList.QueueRefresh();
            return false;
        }

        // Rechecks an action against the engine's current state; the list can be a frame behind.
        private bool CheckAction(Func<PlayerListActions, PlayerListActionState> getState)
        {
            var state = getState(PlayerListRules.Evaluate(PlayerList.GetRelation(Player, _selectedPlayer)));
            if (state.IsEnabled)
                return true;

            DMPresence.SendMessage(Player, ColorToken.Red(state.DisabledReason));
            PlayerList.QueueRefresh();
            return false;
        }
    }
}
