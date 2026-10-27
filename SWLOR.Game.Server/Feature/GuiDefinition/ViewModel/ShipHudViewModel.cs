using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class ShipHudViewModel : GuiViewModelBase<ShipHudViewModel, GuiPayloadBase>,
        IGuiRefreshable<ShipHudRefreshEvent>
    {
        private const int ShipTabId = 0;
        private const int StatsTabId = 1;

        public const string TabContentElement = "shiphud_tab_content";
        public const string ShipTabPartial = "SHIPHUD_TAB_SHIP";
        public const string StatsTabPartial = "SHIPHUD_TAB_STATS";

        private const string NoTargetName = "No target selected";
        private const string NoValue = "-";

        private static readonly GuiTabGroup<ShipHudViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<ShipHudViewModel, GuiPayloadBase>()
                .AddTab(ShipTabId, ShipTabPartial)
                .AddTab(StatsTabId, StatsTabPartial, m => m.RefreshStats());

        private static readonly GuiToggleGroupSync TabToggles = new(ShipTabId, StatsTabId);

        private List<ShipHudModuleRow> _moduleRows = new();
        private List<string> _statLabels = new();
        private List<string> _statValues = new();

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        public string TargetName { get => Get<string>(); set => Set(value); }
        public string TargetShieldText { get => Get<string>(); set => Set(value); }
        public string TargetHullText { get => Get<string>(); set => Set(value); }
        public float TargetShieldProgress { get => Get<float>(); set => Set(value); }
        public float TargetHullProgress { get => Get<float>(); set => Set(value); }

        public string AllWeaponsText { get => Get<string>(); set => Set(value); }
        public bool AllWeaponsEnabled { get => Get<bool>(); set => Set(value); }

        public string ModuleSummary { get => Get<string>(); set => Set(value); }
        public bool NoModulesVisible { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<string> ModuleNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ModuleTargets { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<float> ModuleProgress { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public GuiBindingList<bool> ModuleCanRetarget { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public GuiBindingList<string> StatLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> StatValues { get => Get<GuiBindingList<string>>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _moduleRows = new List<ShipHudModuleRow>();
            _statLabels = new List<string>();
            _statValues = new List<string>();

            TargetName = NoTargetName;
            TargetShieldText = NoValue;
            TargetHullText = NoValue;
            TargetShieldProgress = 0f;
            TargetHullProgress = 0f;
            AllWeaponsText = ShipHudRules.GetAllWeaponsText(0, 0);
            AllWeaponsEnabled = false;
            ModuleSummary = string.Empty;
            NoModulesVisible = true;
            ModuleNames = new GuiBindingList<string>();
            ModuleTargets = new GuiBindingList<string>();
            ModuleProgress = new GuiBindingList<float>();
            ModuleCanRetarget = new GuiBindingList<bool>();
            StatLabels = new GuiBindingList<string>();
            StatValues = new GuiBindingList<string>();
            TabToggleValue = ShipTabId;

            WatchOnClient(model => model.TabToggleValue);

            SelectTab(ShipTabId);
            RefreshShip();
            SpaceHud.Register(Player);
        }

        public override Action OnWindowClosed() => () => SpaceHud.Unregister(Player);

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, v => TabToggleValue = v);
            Tabs.Select(this, TabContentElement, tabId);
        }

        public void Refresh(ShipHudRefreshEvent payload)
        {
            RefreshShip();
            if (SelectedTabId == StatsTabId)
                RefreshStats();
        }

        /// <summary>Pushes a value only when it differs from what the client already shows.</summary>
        private static void Assign<T>(T value, Func<T> current, Action<T> assign)
        {
            if (!EqualityComparer<T>.Default.Equals(value, current()))
                assign(value);
        }

        private void RefreshShip()
        {
            var status = Space.GetShipStatus(Player);
            if (status == null)
                return;

            RefreshTarget();

            var seat = ShipHudRules.PilotSeat;
            var (weaponsOn, weaponTotal) = SpaceHud.GetWeaponCounts(status, seat);
            Assign(ShipHudRules.GetAllWeaponsText(weaponsOn, weaponTotal), () => AllWeaponsText, v => AllWeaponsText = v);
            Assign(weaponTotal > 0, () => AllWeaponsEnabled, v => AllWeaponsEnabled = v);

            _moduleRows = SpaceHud.GetModuleRows(Player, status, seat);
            var summary = _moduleRows.Count == 1 ? "1 module on" : $"{_moduleRows.Count} modules on";
            Assign(summary, () => ModuleSummary, v => ModuleSummary = v);
            Assign(_moduleRows.Count == 0, () => NoModulesVisible, v => NoModulesVisible = v);

            AssignList(_moduleRows.Select(x => $"{x.SlotLabel}: {x.Name}").ToList(), () => ModuleNames, v => ModuleNames = v);
            AssignList(_moduleRows.Select(x => x.TargetText).ToList(), () => ModuleTargets, v => ModuleTargets = v);
            AssignList(_moduleRows.Select(x => x.Progress).ToList(), () => ModuleProgress, v => ModuleProgress = v);
            AssignList(_moduleRows.Select(x => x.CanRetarget).ToList(), () => ModuleCanRetarget, v => ModuleCanRetarget = v);
        }

        private void RefreshTarget()
        {
            var (target, targetStatus) = Space.GetCurrentTarget(Player);
            if (!GetIsObjectValid(target))
            {
                Assign(NoTargetName, () => TargetName, v => TargetName = v);
                Assign(NoValue, () => TargetShieldText, v => TargetShieldText = v);
                Assign(NoValue, () => TargetHullText, v => TargetHullText = v);
                Assign(0f, () => TargetShieldProgress, v => TargetShieldProgress = v);
                Assign(0f, () => TargetHullProgress, v => TargetHullProgress = v);
                return;
            }

            Assign(PlayerName.GetDisplayName(Player, target), () => TargetName, v => TargetName = v);
            if (targetStatus == null)
            {
                Assign(NoValue, () => TargetShieldText, v => TargetShieldText = v);
                Assign(NoValue, () => TargetHullText, v => TargetHullText = v);
                Assign(0f, () => TargetShieldProgress, v => TargetShieldProgress = v);
                Assign(0f, () => TargetHullProgress, v => TargetHullProgress = v);
                return;
            }

            Assign($"{targetStatus.Shield} / {targetStatus.MaxShield}", () => TargetShieldText, v => TargetShieldText = v);
            Assign($"{targetStatus.Hull} / {targetStatus.MaxHull}", () => TargetHullText, v => TargetHullText = v);
            Assign(ToProgress(targetStatus.Shield, targetStatus.MaxShield), () => TargetShieldProgress, v => TargetShieldProgress = v);
            Assign(ToProgress(targetStatus.Hull, targetStatus.MaxHull), () => TargetHullProgress, v => TargetHullProgress = v);
        }

        private static float ToProgress(int current, int maximum)
        {
            return maximum <= 0 ? 0f : Math.Clamp((float)current / maximum, 0f, 1f);
        }

        private void RefreshStats()
        {
            var status = Space.GetShipStatus(Player);
            if (status == null)
                return;

            var rows = ShipHudRules.BuildStatRows(SpaceHud.CollectStats(Player, status));
            var labels = rows.Select(x => x.Label).ToList();
            var values = rows.Select(x => x.Value).ToList();

            if (!labels.SequenceEqual(_statLabels))
            {
                _statLabels = labels;
                StatLabels = ToBindingList(labels);
            }

            if (!values.SequenceEqual(_statValues))
            {
                _statValues = values;
                StatValues = ToBindingList(values);
            }
        }

        private static GuiBindingList<T> ToBindingList<T>(IEnumerable<T> values)
        {
            var list = new GuiBindingList<T>();
            foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }

        /// <summary>Replaces a list column wholesale, and only when its contents changed.</summary>
        private static void AssignList<T>(
            List<T> values,
            Func<GuiBindingList<T>> current,
            Action<GuiBindingList<T>> assign)
        {
            if (!current().SequenceEqual(values))
                assign(ToBindingList(values));
        }

        public Action OnClickAllWeapons() => () =>
        {
            Space.ToggleAllWeapons(Player);
            RefreshShip();
        };

        public Action OnClickRetarget() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _moduleRows.Count)
                return;

            Space.RetargetShipToggle(Player, _moduleRows[index].Slot);
            RefreshShip();
        };
    }
}
