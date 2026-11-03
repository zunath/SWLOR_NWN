using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CityProjectsViewModel : GuiViewModelBase<CityProjectsViewModel, CityProjectsPayload>
    {
        public const string MainContentElement = "cityprojects_content";
        public const string MainContentPartial = "CITYPROJECTS_MAIN";

        private string _cityId;
        private readonly List<string> _projectIds = new();
        private readonly List<string> _materialResrefs = new();
        private string _selectedProjectId = string.Empty;
        private int _selectedMaterialIndex = -1;

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string MessageText { get => Get<string>(); set => Set(value); }
        public string ProjectsHeaderText { get => Get<string>(); set => Set(value); }
        public string DetailText { get => Get<string>(); set => Set(value); }
        public string MaterialsHeaderText { get => Get<string>(); set => Set(value); }

        public GuiBindingList<string> ProjectNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> ProjectToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> MaterialNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> MaterialToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public bool CanDonate { get => Get<bool>(); set => Set(value); }
        public string DonateTooltip { get => Get<string>(); set => Set(value); }
        public bool CanDonateAll { get => Get<bool>(); set => Set(value); }
        public string DonateAllTooltip { get => Get<string>(); set => Set(value); }
        public bool CanCancel { get => Get<bool>(); set => Set(value); }
        public string CancelTooltip { get => Get<string>(); set => Set(value); }

        protected override void Initialize(CityProjectsPayload initialPayload)
        {
            _cityId = initialPayload?.CityId ?? string.Empty;

            // Every bound property gets a value before the view is shown.
            HeaderText = string.Empty;
            MessageText = string.Empty;
            ProjectsHeaderText = string.Empty;
            DetailText = string.Empty;
            MaterialsHeaderText = string.Empty;
            ProjectNames = new GuiBindingList<string>();
            ProjectToggles = new GuiBindingList<bool>();
            MaterialNames = new GuiBindingList<string>();
            MaterialToggles = new GuiBindingList<bool>();
            CanDonate = false;
            DonateTooltip = string.Empty;
            CanDonateAll = false;
            DonateAllTooltip = string.Empty;
            CanCancel = false;
            CancelTooltip = string.Empty;

            Refresh();
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        /// <summary>Reads the city's projects and the player's inventory again and sets what the window shows.</summary>
        private void Refresh()
        {
            var now = DateTime.UtcNow;
            var city = string.IsNullOrWhiteSpace(_cityId) ? null : DB.Get<WorldProperty>(_cityId);
            var projects = city == null
                ? new List<CityProject>()
                : CityProjects.GetProjects(_cityId).Where(project => project.State == CityProjectState.Open).ToList();

            HeaderText = city == null ? "This city is gone." : $"{city.CustomName} - City Projects";
            ProjectsHeaderText = projects.Count == 0
                ? "The city has no projects under way."
                : "Projects (anyone can donate the materials they carry):";

            _projectIds.Clear();
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            foreach (var project in projects)
            {
                _projectIds.Add(project.Id);
                names.Add($"{CityProjectRules.GetName(project)} - {CityProjectRules.GetDonatedCount(project)} / {CityProjectRules.GetRequiredCount(project)} materials");
                toggles.Add(false);
            }

            if (!_projectIds.Contains(_selectedProjectId))
                _selectedProjectId = _projectIds.FirstOrDefault() ?? string.Empty;

            for (var index = 0; index < toggles.Count; index++)
                toggles[index] = _projectIds[index] == _selectedProjectId;

            ProjectNames = names;
            ProjectToggles = toggles;

            var selected = projects.FirstOrDefault(project => project.Id == _selectedProjectId);
            ShowProject(city, selected, now);
        }

        private void ShowProject(WorldProperty city, CityProject project, DateTime now)
        {
            _materialResrefs.Clear();
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            if (project == null)
            {
                DetailText = string.Empty;
                MaterialsHeaderText = string.Empty;
                _selectedMaterialIndex = -1;
                MaterialNames = names;
                MaterialToggles = toggles;
                UpdateButtons(city, null);
                return;
            }

            var donors = CityProjectRules.GetTopDonors(project);
            DetailText = $"{CityProjectRules.GetName(project)}\n" +
                         $"Credits held from the treasury: {project.CreditCost:N0}\n" +
                         $"{CityProjectRules.DescribeStatus(project, now)}\n" +
                         (donors.Count == 0
                             ? "No donations yet."
                             : "Top donors: " + string.Join(", ", donors.Select(donor => $"{donor.Label} ({donor.Items})")));

            MaterialsHeaderText = "Materials (pick one to donate):";
            foreach (var resref in project.RequiredMaterials.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                _materialResrefs.Add(resref);
                names.Add($"{CityProjects.GetItemName(resref)}: {project.DonatedMaterials.GetValueOrDefault(resref)} / {project.RequiredMaterials[resref]}   (you carry {CityProjects.CountCarried(Player, resref)})");
                toggles.Add(false);
            }

            if (_selectedMaterialIndex >= _materialResrefs.Count)
                _selectedMaterialIndex = -1;
            if (_selectedMaterialIndex >= 0)
                toggles[_selectedMaterialIndex] = true;

            MaterialNames = names;
            MaterialToggles = toggles;
            UpdateButtons(city, project);
        }

        private void UpdateButtons(WorldProperty city, CityProject project)
        {
            var playerId = GetObjectUUID(Player);
            var accepts = project != null && CityProjectRules.AcceptsDonations(project);
            var isStaff = GetIsDM(Player) || GetIsDMPossessed(Player);

            var carriesAny = accepts && project.RequiredMaterials.Keys.Any(resref =>
                CityProjectRules.GetRemaining(project, resref) > 0 && CityProjects.CountCarried(Player, resref) > 0);
            CanDonateAll = carriesAny && !isStaff;
            DonateAllTooltip = project == null ? "Pick a project first."
                : !accepts ? "This project has every material it needs."
                : isStaff ? "Only characters can donate."
                : carriesAny ? string.Empty : "You carry none of the materials it still needs.";

            var selected = _selectedMaterialIndex >= 0 && _selectedMaterialIndex < _materialResrefs.Count ? _materialResrefs[_selectedMaterialIndex] : null;
            var canDonateSelected = accepts && selected != null &&
                                    CityProjectRules.GetRemaining(project, selected) > 0 &&
                                    CityProjects.CountCarried(Player, selected) > 0;
            CanDonate = canDonateSelected && !isStaff;
            DonateTooltip = project == null ? "Pick a project first."
                : !accepts ? "This project has every material it needs."
                : selected == null ? "Pick a material first."
                : isStaff ? "Only characters can donate."
                : CityProjectRules.GetRemaining(project, selected) <= 0 ? "The project has all of that material."
                : canDonateSelected ? string.Empty : "You do not carry that material.";

            var canCancel = project != null && CityProjects.CanCancel(city, playerId);
            CanCancel = canCancel;
            CancelTooltip = project == null ? "Pick a project first."
                : canCancel ? string.Empty : "Only an official who can start projects can cancel one.";
        }

        public Action OnClickProject() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _projectIds.Count)
                return;

            _selectedProjectId = _projectIds[index];
            _selectedMaterialIndex = -1;
            MessageText = string.Empty;
            Refresh();
        };

        public Action OnClickMaterial() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _materialResrefs.Count)
                return;

            _selectedMaterialIndex = index;
            Refresh();
        };

        public Action OnClickDonate() => () =>
        {
            if (_selectedMaterialIndex < 0 || _selectedMaterialIndex >= _materialResrefs.Count || string.IsNullOrEmpty(_selectedProjectId))
                return;

            var resref = _materialResrefs[_selectedMaterialIndex];
            var taken = CityProjectDonations.Donate(Player, _selectedProjectId, resref, CityProjects.CountCarried(Player, resref));
            MessageText = taken > 0
                ? $"You donated {taken} {CityProjects.GetItemName(resref)}. Thank you."
                : "Nothing was donated. The project may already have all of that material.";
            Refresh();
        };

        public Action OnClickDonateAll() => () =>
        {
            if (string.IsNullOrEmpty(_selectedProjectId))
                return;

            var project = CityProjects.GetProject(_selectedProjectId);
            if (project == null)
            {
                Refresh();
                return;
            }

            var gifts = new List<string>();
            foreach (var resref in project.RequiredMaterials.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                var taken = CityProjectDonations.Donate(Player, _selectedProjectId, resref, CityProjects.CountCarried(Player, resref));
                if (taken > 0)
                    gifts.Add($"{taken} {CityProjects.GetItemName(resref)}");
            }

            MessageText = gifts.Count > 0 ? $"You donated {string.Join(", ", gifts)}. Thank you." : "You carry none of the materials it still needs.";
            Refresh();
        };

        public Action OnClickCancel() => () =>
        {
            var project = CityProjects.GetProject(_selectedProjectId);
            if (project == null)
                return;

            ShowModal(
                $"Cancel {CityProjectRules.GetName(project)}? The {project.CreditCost:N0} credits return to the balances they came from, and every donor's materials go to their overflow store.",
                () =>
                {
                    MessageText = CityProjects.Cancel(_selectedProjectId, GetObjectUUID(Player))
                        ? "The project was cancelled. Its credits and materials were returned."
                        : "The project could not be cancelled.";
                    _selectedMaterialIndex = -1;
                    Refresh();
                },
                null,
                "Cancel project",
                "Keep");
        };
    }
}
