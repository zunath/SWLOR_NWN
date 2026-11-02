using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CityGazetteViewModel : GuiViewModelBase<CityGazetteViewModel, CityGazettePayload>
    {
        public const string MainContentElement = "citygazette_content";
        public const string MainContentPartial = "CITYGAZETTE_MAIN";

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string EmptyText { get => Get<string>(); set => Set(value); }
        public bool IsEmpty { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<string> RowDates { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowOfficials { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }

        protected override void Initialize(CityGazettePayload initialPayload)
        {
            var cityId = initialPayload?.CityId ?? string.Empty;
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);

            // Secret entries stay out for every reader, the mayor included.
            var entries = city == null
                ? new List<CityGazetteEntry>()
                : CityGazette.GetEntries(city.Id, false);

            var dates = new GuiBindingList<string>();
            var officials = new GuiBindingList<string>();
            var texts = new GuiBindingList<string>();
            var names = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                dates.Add(entry.TimeUtc.ToString("yyyy-MM-dd"));
                officials.Add(DescribeOfficial(entry, names));
                texts.Add(entry.Text);
            }

            HeaderText = city == null ? "City Gazette" : $"{city.CustomName} Gazette";
            EmptyText = "Nothing has been published here yet.";
            IsEmpty = entries.Count == 0;
            RowDates = dates;
            RowOfficials = officials;
            RowTexts = texts;

            ChangePartialView(MainContentElement, MainContentPartial);
        }

        private string DescribeOfficial(CityGazetteEntry entry, Dictionary<string, string> names)
        {
            if (string.IsNullOrWhiteSpace(entry.OfficialPlayerId))
                return entry.OfficialTitle;

            if (!names.TryGetValue(entry.OfficialPlayerId, out var name))
            {
                var dbOfficial = DB.Get<Player>(entry.OfficialPlayerId);
                name = PlayerName.GetPlainDisplayNameByPlayerId(Player, entry.OfficialPlayerId, dbOfficial?.Name);
                names[entry.OfficialPlayerId] = name;
            }

            return string.IsNullOrWhiteSpace(entry.OfficialTitle)
                ? name
                : $"{name}\n{entry.OfficialTitle}";
        }
    }
}
