using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Bounties tab of City Affairs: the mayor and officers see the city's open bounties, withdraw one, and post a
    /// bounty on an online player who shows the PvP flag and accepted the bounty notice. <see cref="CityBounties"/> holds
    /// the rules and checks everything again when an action is taken.
    /// </summary>
    public partial class CityAffairsViewModel
    {
        private readonly List<string> _bountyIds = new();
        private int _selectedBountyIndex = -1;
        private List<BountyTarget> _bountyTargets = new();

        public string BountyStatusText { get => Get<string>(); set => Set(value); }
        public bool IsBountyListEmpty { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<string> BountyNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> BountyToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public bool CanWithdrawBounty { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<GuiComboEntry> TargetOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SelectedTargetIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                ShowSelectedTargetStatus();
            }
        }

        public string TargetStatusText { get => Get<string>(); set => Set(value); }
        public GuiColor TargetStatusColor { get => Get<GuiColor>(); set => Set(value); }
        public string RewardInput { get => Get<string>(); set => Set(value); }
        public bool CanPostBounty { get => Get<bool>(); set => Set(value); }

        private void InitializeBounties()
        {
            BountyStatusText = string.Empty;
            IsBountyListEmpty = true;
            BountyNames = new GuiBindingList<string>();
            BountyToggles = new GuiBindingList<bool>();
            CanWithdrawBounty = false;
            TargetOptions = new GuiBindingList<GuiComboEntry>();
            SelectedTargetIndex = 0;
            TargetStatusText = string.Empty;
            TargetStatusColor = GuiColor.White;
            RewardInput = string.Empty;
            CanPostBounty = false;
        }

        private void RefreshBounties()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
            {
                ShowError("This city no longer exists.");
                return;
            }

            var canPost = CityBounties.CanPost(city, GetObjectUUID(Player));
            var now = DateTime.UtcNow;
            var open = CityBounties.GetOpenForCity(_cityId);
            var selectedId = _selectedBountyIndex >= 0 && _selectedBountyIndex < _bountyIds.Count
                ? _bountyIds[_selectedBountyIndex]
                : string.Empty;

            _bountyIds.Clear();
            var names = new GuiBindingList<string>();
            foreach (var bounty in open)
            {
                _bountyIds.Add(bounty.Id);
                var label = string.IsNullOrWhiteSpace(bounty.TargetLabel) ? "Someone" : bounty.TargetLabel;
                names.Add($"{label} - {BountyRules.FormatCredits(bounty.Reward)} cr - {SituationRules.FormatTimeLeft(bounty.ExpiresUtc, now)}");
            }

            _selectedBountyIndex = Math.Max(-1, _bountyIds.IndexOf(selectedId));
            var toggles = new GuiBindingList<bool>();
            for (var index = 0; index < open.Count; index++)
            {
                toggles.Add(index == _selectedBountyIndex);
            }

            BountyStatusText = $"Treasury available for bounties: {BountyRules.FormatCredits(CityTreasuryRules.GetSpendable(city, restrictedFirst: false))} credits " +
                               "(restricted funds cannot pay one). A bounty runs " +
                               $"{BountyRules.LengthDays} days, and the reward returns to the treasury if nobody collects it.";
            BountyNames = names;
            BountyToggles = toggles;
            IsBountyListEmpty = open.Count == 0;
            CanWithdrawBounty = canPost && _selectedBountyIndex >= 0;

            RefreshBountyTargets(canPost);
        }

        private void RefreshBountyTargets(bool canPost)
        {
            var selectedPlayerId = SelectedTargetIndex >= 0 && SelectedTargetIndex < _bountyTargets.Count
                ? _bountyTargets[SelectedTargetIndex].PlayerId
                : string.Empty;

            _bountyTargets = canPost ? CityBounties.GetTargets(Player) : new List<BountyTarget>();
            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < _bountyTargets.Count; index++)
            {
                var target = _bountyTargets[index];
                options.Add(new GuiComboEntry(string.IsNullOrEmpty(target.Problem) ? target.Name : $"{target.Name} (unavailable)", index));
            }

            if (_bountyTargets.Count == 0)
                options.Add(new GuiComboEntry("No one online shows the PvP flag", 0));

            TargetOptions = options;
            SelectedTargetIndex = Math.Max(0, _bountyTargets.FindIndex(x => x.PlayerId == selectedPlayerId));
            CanPostBounty = canPost && _bountyTargets.Count > 0;
            ShowSelectedTargetStatus();
        }

        private void ShowSelectedTargetStatus()
        {
            if (SelectedTargetIndex < 0 || SelectedTargetIndex >= _bountyTargets.Count)
            {
                TargetStatusText = "Only a player who shows the PvP flag in the Player List and accepted the bounty notice can be given a bounty.";
                TargetStatusColor = GuiColor.White;
                return;
            }

            var problem = _bountyTargets[SelectedTargetIndex].Problem;
            TargetStatusText = string.IsNullOrEmpty(problem) ? "Ready to post." : problem;
            TargetStatusColor = string.IsNullOrEmpty(problem) ? GuiColor.Green : GuiColor.Red;
        }

        public Action SelectBounty() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _bountyIds.Count)
                return;

            _selectedBountyIndex = index;
            RefreshBounties();
        };

        public Action OnClickWithdrawBounty() => () =>
        {
            if (_selectedBountyIndex < 0 || _selectedBountyIndex >= _bountyIds.Count)
            {
                ShowError("Pick a bounty in the list first.");
                return;
            }

            var bountyId = _bountyIds[_selectedBountyIndex];
            var bounty = CityBounties.Get(bountyId);
            if (bounty == null)
            {
                ShowError("That bounty is gone.");
                RefreshBounties();
                return;
            }

            ShowModal(
                $"Withdraw the bounty of {BountyRules.FormatCredits(bounty.Reward)} credits on {bounty.TargetLabel}? The reward returns to the treasury and the Gazette says it was withdrawn.",
                () =>
                {
                    var error = CityBounties.Cancel(Player, bountyId);
                    if (string.IsNullOrEmpty(error))
                        ShowSuccess("Bounty withdrawn. The reward is back in the treasury.");
                    else
                        ShowError(error);

                    _selectedBountyIndex = -1;
                    RefreshBounties();
                },
                null,
                "Withdraw",
                "Cancel");
        };

        public Action OnClickPostBounty() => () =>
        {
            if (SelectedTargetIndex < 0 || SelectedTargetIndex >= _bountyTargets.Count)
            {
                ShowError("Pick a player who shows the PvP flag.");
                return;
            }

            if (!BountyRules.TryParseReward(RewardInput, out var reward))
            {
                ShowError("Enter the reward as a whole number of credits.");
                return;
            }

            var target = _bountyTargets[SelectedTargetIndex];
            if (!string.IsNullOrEmpty(target.Problem))
            {
                ShowError(target.Problem);
                return;
            }

            ShowModal(
                $"Post a bounty of {BountyRules.FormatCredits(reward)} credits on {target.Name}? The reward is held from the treasury until someone collects it " +
                $"or it runs out after {BountyRules.LengthDays} days. The bounty is public in the Gazette, and the player is told.",
                () =>
                {
                    var error = CityBounties.Post(Player, _cityId, target.Player, target.DisguiseId, reward);
                    if (string.IsNullOrEmpty(error))
                    {
                        RewardInput = string.Empty;
                        ShowSuccess("Bounty posted. It is in the City Gazette.");
                    }
                    else
                    {
                        ShowError(error);
                    }

                    RefreshBounties();
                },
                null,
                "Post",
                "Cancel");
        };
    }
}
