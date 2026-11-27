using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Succession tab (Player City Revamp, "Acting mayor and stepping down"): the mayor names or ends an Acting Mayor and
    /// steps down by naming an interim holder; the named player accepts or declines here.
    /// </summary>
    public partial class ManageCityViewModel
    {
        private readonly List<string> _actingCandidateIds = new();
        private readonly List<string> _interimHolderIds = new();

        public string SuccessionStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The mayor's controls: naming an Acting Mayor and stepping down.</summary>
        public bool IsMayorSuccessionVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>The named interim holder's Accept and Decline buttons.</summary>
        public bool IsStepDownReplyVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string StepDownReplyText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> ActingCandidateOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedActingCandidateIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> ActingTermOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedActingTermIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool CanNameActingMayor
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanEndActingMayor
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string StepDownHelpText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> InterimHolderOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedInterimHolderIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool CanStepDown
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanCancelStepDown
        {
            get => Get<bool>();
            set => Set(value);
        }

        private void InitializeSuccessionTab()
        {
            SuccessionStatusText = string.Empty;
            IsMayorSuccessionVisible = false;
            IsStepDownReplyVisible = false;
            StepDownReplyText = string.Empty;
            ActingCandidateOptions = new GuiBindingList<GuiComboEntry>();
            SelectedActingCandidateIndex = 0;
            ActingTermOptions = BuildActingTermOptions();
            SelectedActingTermIndex = ActingMayorRules.TermChoicesDays.Count - 1;
            CanNameActingMayor = false;
            CanEndActingMayor = false;
            StepDownHelpText = string.Empty;
            InterimHolderOptions = new GuiBindingList<GuiComboEntry>();
            SelectedInterimHolderIndex = 0;
            CanStepDown = false;
            CanCancelStepDown = false;

            WatchOnClient(model => model.SelectedActingCandidateIndex);
            WatchOnClient(model => model.SelectedActingTermIndex);
            WatchOnClient(model => model.SelectedInterimHolderIndex);
        }

        private static GuiBindingList<GuiComboEntry> BuildActingTermOptions()
        {
            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < ActingMayorRules.TermChoicesDays.Count; index++)
            {
                var days = ActingMayorRules.TermChoicesDays[index];
                options.Add(new GuiComboEntry(days == 1 ? "1 day" : $"{days} days", index));
            }

            return options;
        }

        /// <summary>Reads the city again and rebuilds the tab. Runs each time the tab is shown.</summary>
        private void RefreshSuccessionTab()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
                return;

            var playerId = GetObjectUUID(Player);
            var now = DateTime.UtcNow;
            var role = CityPowers.GetRole(city, playerId);
            var isMayor = role == CityRole.Mayor;
            var isElectionOpen = ElectionBallot.GetCityElection(city.Id) != null;

            IsMayorSuccessionVisible = isMayor;
            RefreshStepDownReply(city, playerId, now);
            SuccessionStatusText = BuildSuccessionStatus(city, now);

            LoadActingMayorControls(city, isMayor, now);
            LoadStepDownControls(city, isMayor, isElectionOpen, now);
        }

        private string BuildSuccessionStatus(WorldProperty city, DateTime now)
        {
            var lines = new List<string>
            {
                $"Mayor: {GetPlainName(city.OwnerPlayerId)}"
            };

            lines.Add(ActingMayorRules.IsActive(city.ActingMayorPlayerId, city.ActingMayorEndsUtc, now)
                ? ActingMayorRules.BuildStatusText(GetPlainName(city.ActingMayorPlayerId), city.ActingMayorEndsUtc.Value, now)
                : "Acting Mayor: none.");

            if (city.StepDown != null)
                lines.Add(StepDownRules.BuildStatusText(city.StepDown, GetPlainName(city.StepDown.InterimHolderPlayerId), now));

            return string.Join("\n", lines);
        }

        private string GetPlainName(string playerId)
        {
            var player = string.IsNullOrWhiteSpace(playerId) ? null : DB.Get<Player>(playerId);
            return PlayerName.GetPlainDisplayNameByPlayerId(Player, playerId, player?.Name);
        }

        private string GetPickerName(string playerId, string fallbackName)
        {
            // The mayor picks citizens by the name they know them by, as in the Officers tab.
            return PlayerName.GetKnownNameOrFallbackByPlayerId(Player, playerId, fallbackName);
        }

        private void RefreshStepDownReply(WorldProperty city, string playerId, DateTime now)
        {
            var waiting = StepDownRules.IsWaitingForAnswerFrom(city.StepDown, playerId, now);
            IsStepDownReplyVisible = waiting;
            StepDownReplyText = waiting
                ? "The mayor named you to hold the seat. If you accept, the mayor steps down with no powers, a snap election opens at once, " +
                  "and you have the Acting Mayor's powers until it ends. If nobody wins that election, you become mayor. " +
                  $"You have {ElectionRules.GetDaysLeft(now, city.StepDown.AcceptDeadlineUtc)} days left to answer."
                : string.Empty;
        }

        private void LoadActingMayorControls(WorldProperty city, bool isMayor, DateTime now)
        {
            _actingCandidateIds.Clear();
            var options = new GuiBindingList<GuiComboEntry>();

            if (isMayor)
            {
                foreach (var citizen in CitySuccession.GetActingMayorCandidates(city))
                {
                    options.Add(new GuiComboEntry(GetPickerName(citizen.Id, citizen.Name), _actingCandidateIds.Count));
                    _actingCandidateIds.Add(citizen.Id);
                }
            }

            var hasCandidates = _actingCandidateIds.Count > 0;
            if (!hasCandidates)
                options.Add(new GuiComboEntry("No citizens in good standing", 0));

            ActingCandidateOptions = options;
            SelectedActingCandidateIndex = 0;
            SelectedActingTermIndex = ActingMayorRules.TermChoicesDays.Count - 1;
            CanNameActingMayor = isMayor && hasCandidates;
            CanEndActingMayor = isMayor && !string.IsNullOrWhiteSpace(city.ActingMayorPlayerId);
        }

        private void LoadStepDownControls(WorldProperty city, bool isMayor, bool isElectionOpen, DateTime now)
        {
            _interimHolderIds.Clear();
            var options = new GuiBindingList<GuiComboEntry>();

            if (isMayor)
            {
                foreach (var holderId in CitySuccession.GetEligibleHolders(city, now))
                {
                    var holder = DB.Get<Player>(holderId);
                    options.Add(new GuiComboEntry(GetPickerName(holderId, holder?.Name), _interimHolderIds.Count));
                    _interimHolderIds.Add(holderId);
                }
            }

            var hasHolders = _interimHolderIds.Count > 0;
            if (!hasHolders)
                options.Add(new GuiComboEntry("No Acting Mayor or officers", 0));

            InterimHolderOptions = options;
            SelectedInterimHolderIndex = 0;

            var isPending = StepDownRules.IsPending(city.StepDown);
            CanStepDown = isMayor && hasHolders && city.StepDown == null && !isElectionOpen;
            CanCancelStepDown = isMayor && isPending;
            StepDownHelpText = !isMayor
                ? string.Empty
                : city.StepDown != null
                    ? "A step-down is under way. Until the named player accepts, you keep every power."
                    : isElectionOpen
                        ? "You cannot step down while an election is open."
                        : !hasHolders
                            ? "You have no Acting Mayor or officers to take over. Name an Acting Mayor first, then step down."
                            : $"Pick your Acting Mayor or an officer to hold the seat. They have {StepDownRules.AcceptDays} days to accept in Manage City. " +
                              "When they do, a snap election opens at once, you step down with no powers, and they have the Acting Mayor's powers until it ends. " +
                              "You cannot run in that election.";
        }

        public Action NameActingMayor() => () =>
        {
            if (SelectedActingCandidateIndex < 0 || SelectedActingCandidateIndex >= _actingCandidateIds.Count)
            {
                ShowSuccessionError("Pick a citizen to name Acting Mayor.");
                return;
            }

            var targetId = _actingCandidateIds[SelectedActingCandidateIndex];
            var days = SelectedActingTermIndex >= 0 && SelectedActingTermIndex < ActingMayorRules.TermChoicesDays.Count
                ? ActingMayorRules.TermChoicesDays[SelectedActingTermIndex]
                : ActingMayorRules.MaxTermDays;
            var name = ActingCandidateOptions[SelectedActingCandidateIndex].Label;

            ShowModal(
                $"Name {name} Acting Mayor for {days} days? They can decide City Affairs, change taxes, pay upkeep, start projects, enact Edicts into empty slots, and post requests. " +
                "They cannot withdraw from the treasury, repeal Edicts, appoint, or grant permissions. You can end the term at any time.",
                () => ConfirmNameActingMayor(targetId, days),
                null,
                "Name",
                "Cancel");
        };

        private void ConfirmNameActingMayor(string targetId, int days)
        {
            var error = CitySuccession.NameActingMayor(_cityId, GetObjectUUID(Player), targetId, days);
            if (string.IsNullOrEmpty(error))
                ShowSuccessionSuccess("Acting Mayor named. The Gazette announces it.");
            else
                ShowSuccessionError(error);

            RefreshSuccessionTab();
        }

        public Action EndActingMayor() => () =>
        {
            ShowModal(
                "End the Acting Mayor's term now? Anything they did stays done.",
                () =>
                {
                    var error = CitySuccession.EndActingMayor(_cityId, GetObjectUUID(Player));
                    if (string.IsNullOrEmpty(error))
                        ShowSuccessionSuccess("The Acting Mayor's term has ended.");
                    else
                        ShowSuccessionError(error);

                    RefreshSuccessionTab();
                },
                null,
                "End Term",
                "Cancel");
        };

        public Action StepDown() => () =>
        {
            if (SelectedInterimHolderIndex < 0 || SelectedInterimHolderIndex >= _interimHolderIds.Count)
            {
                ShowSuccessionError("Pick who will hold the seat.");
                return;
            }

            var holderId = _interimHolderIds[SelectedInterimHolderIndex];
            var name = InterimHolderOptions[SelectedInterimHolderIndex].Label;
            ShowModal(
                $"Name {name} to hold the seat if you step down? They have {StepDownRules.AcceptDays} days to accept. You keep every power until they do. " +
                "Once they accept, a snap election opens, you have no powers until it ends, and you cannot run in it.",
                () => ConfirmStepDown(holderId),
                null,
                "Step Down",
                "Cancel");
        };

        private void ConfirmStepDown(string holderId)
        {
            var error = CitySuccession.BeginStepDown(_cityId, GetObjectUUID(Player), holderId);
            if (string.IsNullOrEmpty(error))
                ShowSuccessionSuccess("The named player has been asked. They accept in Manage City within 7 days.");
            else
                ShowSuccessionError(error);

            RefreshSuccessionTab();
        }

        public Action CancelStepDown() => () =>
        {
            ShowModal(
                "Withdraw your step-down? You stay mayor with every power.",
                () =>
                {
                    var error = CitySuccession.CancelStepDown(_cityId, GetObjectUUID(Player));
                    if (string.IsNullOrEmpty(error))
                        ShowSuccessionSuccess("The step-down was withdrawn.");
                    else
                        ShowSuccessionError(error);

                    RefreshSuccessionTab();
                },
                null,
                "Withdraw",
                "Cancel");
        };

        public Action AcceptStepDown() => () =>
        {
            ShowModal(
                "Accept? The mayor steps down with no powers, a snap election opens at once, and you have the Acting Mayor's powers until it ends. " +
                "If nobody wins the election, you become mayor.",
                () =>
                {
                    var error = CitySuccession.AcceptStepDown(_cityId, GetObjectUUID(Player));
                    if (string.IsNullOrEmpty(error))
                        ShowSuccessionSuccess("You hold the seat until the snap election ends.");
                    else
                        ShowSuccessionError(error);

                    RefreshPermissions();
                    RefreshSuccessionTab();
                },
                null,
                "Accept",
                "Cancel");
        };

        public Action DeclineStepDown() => () =>
        {
            ShowModal(
                "Decline? The mayor's step-down is cancelled.",
                () =>
                {
                    var error = CitySuccession.CancelStepDown(_cityId, GetObjectUUID(Player));
                    if (string.IsNullOrEmpty(error))
                        ShowSuccessionSuccess("You declined. The step-down is cancelled.");
                    else
                        ShowSuccessionError(error);

                    RefreshSuccessionTab();
                },
                null,
                "Decline",
                "Cancel");
        };

        private void ShowSuccessionError(string message)
        {
            Instructions = message;
            InstructionsColor = GuiColor.Red;
        }

        private void ShowSuccessionSuccess(string message)
        {
            Instructions = message;
            InstructionsColor = GuiColor.Green;
        }
    }
}
