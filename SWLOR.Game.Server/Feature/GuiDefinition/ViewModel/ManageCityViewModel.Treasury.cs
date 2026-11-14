using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Treasury tab: the city's treasury history (<see cref="CityTreasuryHistoryLog"/>), for the mayor and those who may
    /// withdraw from the treasury.
    /// </summary>
    public partial class ManageCityViewModel
    {
        public string TreasuryHistoryHeader
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor TreasuryHistoryHeaderColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public bool IsTreasuryHistoryVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> TreasuryHistoryTimes
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> TreasuryHistoryAmounts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> TreasuryHistoryAmountColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> TreasuryHistoryTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> TreasuryHistoryTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private void InitializeTreasuryTab()
        {
            TreasuryHistoryHeader = string.Empty;
            TreasuryHistoryHeaderColor = GuiColor.White;
            IsTreasuryHistoryVisible = false;
            TreasuryHistoryTimes = new GuiBindingList<string>();
            TreasuryHistoryAmounts = new GuiBindingList<string>();
            TreasuryHistoryAmountColors = new GuiBindingList<GuiColor>();
            TreasuryHistoryTexts = new GuiBindingList<string>();
            TreasuryHistoryTooltips = new GuiBindingList<string>();
        }

        /// <summary>Reads the history again. Runs each time the tab is shown; permission is checked as it is read.</summary>
        private void RefreshTreasuryTab()
        {
            var times = new GuiBindingList<string>();
            var amounts = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var texts = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();

            RefreshPermissions();
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null || !CanAccessTreasury)
            {
                TreasuryHistoryHeader = "Only the mayor and those who may withdraw from the treasury can see its history.";
                TreasuryHistoryHeaderColor = GuiColor.Red;
                IsTreasuryHistoryVisible = false;
            }
            else
            {
                var entries = CityTreasuryHistoryLog.Get(city.Id);
                var names = new Dictionary<string, string>();
                foreach (var entry in entries)
                {
                    var name = GetTreasuryActorName(entry.PlayerId, names);
                    var text = CityTreasuryHistoryRules.Describe(entry, name);
                    times.Add(CityTreasuryHistoryRules.FormatTime(entry.TimeUtc));
                    amounts.Add(CityTreasuryHistoryRules.FormatAmount(entry.Amount));
                    colors.Add(entry.Amount > 0 ? GuiColor.Green : entry.Amount < 0 ? GuiColor.Red : GuiColor.Grey);
                    texts.Add(text);
                    tooltips.Add($"{text}\nBalance after: {CityTreasuryHistoryRules.FormatCredits(entry.Balance)} cr");
                }

                TreasuryHistoryHeader = entries.Count == 0
                    ? $"No treasury changes in the last {CityTreasuryHistoryRules.KeepDays} days."
                    : $"Treasury changes in the last {CityTreasuryHistoryRules.KeepDays} days (UTC), newest first. Hover a line for the balance after it.";
                TreasuryHistoryHeaderColor = GuiColor.White;
                IsTreasuryHistoryVisible = entries.Count > 0;
            }

            TreasuryHistoryTimes = times;
            TreasuryHistoryAmounts = amounts;
            TreasuryHistoryAmountColors = colors;
            TreasuryHistoryTexts = texts;
            TreasuryHistoryTooltips = tooltips;
        }

        /// <summary>The name of the player a line names, as this reader knows them, or empty for a line naming no one.</summary>
        private string GetTreasuryActorName(string playerId, Dictionary<string, string> names)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return string.Empty;

            if (!names.TryGetValue(playerId, out var name))
            {
                var dbPlayer = DB.Get<Player>(playerId);
                name = PlayerName.GetPlainDisplayNameByPlayerId(Player, playerId, dbPlayer?.Name);
                names[playerId] = name;
            }

            return name;
        }
    }
}
