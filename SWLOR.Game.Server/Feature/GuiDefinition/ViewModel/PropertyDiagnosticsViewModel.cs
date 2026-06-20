using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyDiagnosticsViewModel : GuiViewModelBase<PropertyDiagnosticsViewModel, GuiPayloadBase>
    {
        private readonly List<PropertyLoadDiagnostic> _diagnostics = new();
        private int _selectedPropertyIndex;

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor StatusColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiBindingList<string> PropertyRows
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PropertyTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PropertySelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsPropertySelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _selectedPropertyIndex = -1;
            IsPropertySelected = false;

            if (Authorization.GetAuthorizationLevel(Player) != AuthorizationLevel.Admin)
            {
                StatusText = "Admin authorization required.";
                StatusColor = GuiColor.Red;
                PropertyRows = new GuiBindingList<string>();
                PropertyTooltips = new GuiBindingList<string>();
                PropertySelections = new GuiBindingList<bool>();
                return;
            }

            LoadDiagnostics("Loaded property diagnostics.");
        }

        private void LoadDiagnostics(string statusText)
        {
            _diagnostics.Clear();
            _diagnostics.AddRange(Property.GetPropertyLoadDiagnostics());

            var rows = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var selections = new GuiBindingList<bool>();

            foreach (var diagnostic in _diagnostics)
            {
                rows.Add(FormatRow(diagnostic));
                tooltips.Add(FormatTooltip(diagnostic));
                selections.Add(false);
            }

            _selectedPropertyIndex = -1;
            IsPropertySelected = false;
            PropertyRows = rows;
            PropertyTooltips = tooltips;
            PropertySelections = selections;
            StatusText = statusText;
            StatusColor = GuiColor.Green;
        }

        private static string FormatRow(PropertyLoadDiagnostic diagnostic)
        {
            var name = string.IsNullOrWhiteSpace(diagnostic.Name)
                ? "[Unnamed]"
                : diagnostic.Name;
            if (name.Length > 36)
                name = name.Substring(0, 36);

            return $"{diagnostic.State} | {diagnostic.LoadType} | {diagnostic.PropertyType} | {name}";
        }

        private static string FormatTooltip(PropertyLoadDiagnostic diagnostic)
        {
            var queueText = diagnostic.IsQueued ? "yes" : "no";
            var failure = string.IsNullOrWhiteSpace(diagnostic.Failure)
                ? "none"
                : diagnostic.Failure;
            var name = string.IsNullOrWhiteSpace(diagnostic.Name)
                ? "[Unnamed]"
                : diagnostic.Name;

            return
                $"Id: {diagnostic.PropertyId}\n" +
                $"Name: {name}\n" +
                $"Owner: {diagnostic.OwnerPlayerId}\n" +
                $"Type: {diagnostic.PropertyType}\n" +
                $"Load: {diagnostic.LoadType}\n" +
                $"State: {diagnostic.State}\n" +
                $"Queued: {queueText}\n" +
                $"Priority: {diagnostic.QueuePriority}\n" +
                $"Children: {diagnostic.SpawnedChildCount} / {diagnostic.ExpectedChildCount}\n" +
                $"Area Valid: {diagnostic.IsLoadedAreaValid}\n" +
                $"Phase: {diagnostic.LastPhase}\n" +
                $"Waiters: {diagnostic.WaiterCount}\n" +
                $"Failure: {failure}";
        }

        private PropertyLoadDiagnostic GetSelectedDiagnostic()
        {
            return _selectedPropertyIndex < 0 || _selectedPropertyIndex >= _diagnostics.Count
                ? null
                : _diagnostics[_selectedPropertyIndex];
        }

        public Action OnSelectProperty() => () =>
        {
            if (_selectedPropertyIndex > -1)
                PropertySelections[_selectedPropertyIndex] = false;

            _selectedPropertyIndex = NuiGetEventArrayIndex();
            PropertySelections[_selectedPropertyIndex] = true;
            IsPropertySelected = true;

            var diagnostic = GetSelectedDiagnostic();
            StatusText = diagnostic == null
                ? string.Empty
                : $"{diagnostic.Name}: {diagnostic.State}";
            StatusColor = GuiColor.White;
        };

        public Action OnRefresh() => () =>
        {
            LoadDiagnostics("Refreshed property diagnostics.");
        };

        public Action OnRetryLoad() => () =>
        {
            var diagnostic = GetSelectedDiagnostic();
            if (diagnostic == null)
                return;

            var result = Property.RetryPropertyLoad(diagnostic.PropertyId);
            LoadDiagnostics(result
                ? "Load retry queued."
                : "Unable to queue load retry.");
            StatusColor = result ? GuiColor.Green : GuiColor.Red;
        };

        public Action OnAbortQueue() => () =>
        {
            var diagnostic = GetSelectedDiagnostic();
            if (diagnostic == null)
                return;

            var result = Property.AbortQueuedPropertyLoad(diagnostic.PropertyId);
            LoadDiagnostics(result
                ? "Queued load aborted."
                : "Unable to abort this property load.");
            StatusColor = result ? GuiColor.Green : GuiColor.Red;
        };

        public Action OnNotifyWaiters() => () =>
        {
            var diagnostic = GetSelectedDiagnostic();
            if (diagnostic == null)
                return;

            Property.NotifyPropertyLoadWaitersForStaff(diagnostic.PropertyId);
            LoadDiagnostics("Waiters notified.");
        };
    }
}
