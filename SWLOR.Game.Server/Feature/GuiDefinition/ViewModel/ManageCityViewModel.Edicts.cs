using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Edicts tab: the city's slots, the Edicts it holds, and the Edicts it can enact.</summary>
    public partial class ManageCityViewModel
    {
        private readonly List<EdictType> _heldEdictTypes = new();
        private readonly List<EdictType> _offeredEdictTypes = new();
        private readonly List<string> _targetCityIds = new();
        private EdictType _selectedOfferedType;
        private string _selectedTargetCityId = string.Empty;

        /// <summary>Whether the player may enact Edicts into empty slots: the mayor, an Acting Mayor, or a Manage Edicts holder.</summary>
        public bool CanManageEdicts
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>Whether the player may repeal Edicts: the mayor or a Manage Edicts holder. An Acting Mayor may not.</summary>
        public bool CanRepealEdicts
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string EdictSlotsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EdictFundsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsNoHeldEdictsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> HeldEdictTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> HeldEdictTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> HeldEdictRepealEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> HeldEdictRepealTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> OfferedEdictNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> OfferedEdictTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> OfferedEdictToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string SelectedEdictText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsTargetPickerVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> TargetCityNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> TargetCityToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsEnactEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        private void InitializeEdictsTab()
        {
            EdictSlotsText = string.Empty;
            EdictFundsText = string.Empty;
            SelectedEdictText = string.Empty;
            IsNoHeldEdictsVisible = false;
            IsTargetPickerVisible = false;
            IsEnactEnabled = false;
            HeldEdictTexts = new GuiBindingList<string>();
            HeldEdictTooltips = new GuiBindingList<string>();
            HeldEdictRepealEnabled = new GuiBindingList<bool>();
            HeldEdictRepealTooltips = new GuiBindingList<string>();
            OfferedEdictNames = new GuiBindingList<string>();
            OfferedEdictTooltips = new GuiBindingList<string>();
            OfferedEdictToggles = new GuiBindingList<bool>();
            TargetCityNames = new GuiBindingList<string>();
            TargetCityToggles = new GuiBindingList<bool>();
        }

        /// <summary>Reads the city again and rebuilds both lists. Runs each time the tab is shown.</summary>
        private void RefreshEdictsTab()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
                return;

            RefreshPermissions();

            var now = DateTime.UtcNow;
            var conditions = EdictChanges.GetConditions(city);
            var level = EdictChanges.GetCityLevel(city);
            var nextCheck = EdictChanges.GetNextCheckUtc(city, now);

            EdictSlotsText = $"Edict slots: {city.Edicts.Count} of {EdictRules.GetSlots(level)} used - city level {level} ({Property.GetCityLevelName(level)})";
            EdictFundsText = EdictRules.BuildFundsText(city.Treasury, city.RestrictedTreasury, conditions, now, nextCheck);

            // The City tab's treasury and upkeep lines follow the charges and part-weeks.
            Treasury = $"Treasury: {city.Treasury} cr";
            RefreshUpkeep();

            LoadHeldEdicts(city, conditions, now);
            LoadOfferedEdicts(city, level, now, nextCheck);
        }

        private void LoadHeldEdicts(WorldProperty city, CityWideCondition conditions, DateTime now)
        {
            var texts = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var repealEnabled = new GuiBindingList<bool>();
            var repealTooltips = new GuiBindingList<string>();

            _heldEdictTypes.Clear();
            foreach (var (type, edict) in city.Edicts.OrderBy(x => (int)x.Key))
            {
                var targetName = string.IsNullOrWhiteSpace(edict.TargetCityId)
                    ? null
                    : DB.Get<WorldProperty>(edict.TargetCityId)?.CustomName;
                var canRepeal = CanRepealEdicts && EdictRules.CanRepeal(edict, now);

                _heldEdictTypes.Add(type);
                texts.Add(EdictRules.BuildHeldText(
                    type, edict, targetName, conditions, now, EdictChanges.IsPausedBySituation(city, type, edict, now)));
                tooltips.Add(EdictCatalog.Get(type)?.Description ?? string.Empty);
                repealEnabled.Add(canRepeal);
                repealTooltips.Add(GetRepealTooltip(edict, now));
            }

            HeldEdictTexts = texts;
            HeldEdictTooltips = tooltips;
            HeldEdictRepealEnabled = repealEnabled;
            HeldEdictRepealTooltips = repealTooltips;
            IsNoHeldEdictsVisible = texts.Count == 0;
        }

        private string GetRepealTooltip(CityEdict edict, DateTime now)
        {
            if (!CanRepealEdicts)
                return "Only the mayor, or a delegate with the Manage Edicts permission, can repeal Edicts.";
            if (!EdictRules.CanRepeal(edict, now))
                return $"Locked for {EdictRules.FormatRemaining(edict.LockEndsUtc.Value - now)} more.";

            return edict.FromPlatform
                ? "Repeal this Edict. It came from your election platform, so this marks a broken promise on the city's record. Nothing is refunded."
                : "Repeal this Edict. Nothing is refunded.";
        }

        private void LoadOfferedEdicts(WorldProperty city, int level, DateTime now, DateTime nextCheck)
        {
            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            _offeredEdictTypes.Clear();
            foreach (var detail in EdictRules.GetOfferedEdicts(Edict.IsWildSpaceStation(city)).Where(x => !city.Edicts.ContainsKey(x.Type)))
            {
                var levelNote = level < detail.MinimumCityLevel ? $" (needs level {detail.MinimumCityLevel})" : string.Empty;
                _offeredEdictTypes.Add(detail.Type);
                names.Add($"{detail.Name} - {detail.WeeklyCost:N0} cr a week{levelNote}");
                tooltips.Add(detail.Description);
                toggles.Add(detail.Type == _selectedOfferedType);
            }

            if (!_offeredEdictTypes.Contains(_selectedOfferedType))
            {
                _selectedOfferedType = EdictType.Invalid;
                _selectedTargetCityId = string.Empty;
            }

            OfferedEdictNames = names;
            OfferedEdictTooltips = tooltips;
            OfferedEdictToggles = toggles;
            ShowSelectedOffer(city, now, nextCheck);
        }

        /// <summary>Shows the chosen Edict's text, the city picker when it names a city, and whether Enact is available.</summary>
        private void ShowSelectedOffer(WorldProperty city, DateTime now, DateTime nextCheck)
        {
            var detail = EdictCatalog.Get(_selectedOfferedType);
            if (detail == null)
            {
                SelectedEdictText = "Choose an Edict to read what it does and what it costs.";
                IsTargetPickerVisible = false;
                TargetCityNames = new GuiBindingList<string>();
                TargetCityToggles = new GuiBindingList<bool>();
                IsEnactEnabled = false;
                return;
            }

            SelectedEdictText = EdictRules.BuildOfferText(detail, EdictRules.GetProration(detail.WeeklyCost, now, nextCheck));
            IsTargetPickerVisible = detail.NamesTargetCity;
            LoadTargetCities(detail.NamesTargetCity);
            IsEnactEnabled = CanManageEdicts && (!detail.NamesTargetCity || _selectedTargetCityId.Length > 0);
        }

        private void LoadTargetCities(bool namesTarget)
        {
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            _targetCityIds.Clear();

            if (namesTarget)
            {
                var query = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                    .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
                var count = (int)DB.SearchCount(query);
                var cities = count > 0
                    ? DB.Search(query.AddPaging(count, 0)).Where(x => x.Id != _cityId).OrderBy(x => x.CustomName).ToList()
                    : new List<WorldProperty>();

                if (!cities.Any(x => x.Id == _selectedTargetCityId))
                    _selectedTargetCityId = string.Empty;

                foreach (var other in cities)
                {
                    _targetCityIds.Add(other.Id);
                    names.Add(other.CustomName);
                    toggles.Add(other.Id == _selectedTargetCityId);
                }
            }

            TargetCityNames = names;
            TargetCityToggles = toggles;
        }

        public Action SelectOfferedEdict() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _offeredEdictTypes.Count)
                return;

            _selectedOfferedType = _offeredEdictTypes[index];
            _selectedTargetCityId = string.Empty;
            Instructions = string.Empty;

            for (var row = 0; row < OfferedEdictToggles.Count; row++)
            {
                OfferedEdictToggles[row] = row == index;
            }

            var city = DB.Get<WorldProperty>(_cityId);
            var now = DateTime.UtcNow;
            ShowSelectedOffer(city, now, EdictChanges.GetNextCheckUtc(city, now));
        };

        public Action SelectTargetCity() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _targetCityIds.Count)
                return;

            _selectedTargetCityId = _targetCityIds[index];
            for (var row = 0; row < TargetCityToggles.Count; row++)
            {
                TargetCityToggles[row] = row == index;
            }

            IsEnactEnabled = CanManageEdicts;
        };

        public Action EnactEdict() => () =>
        {
            var type = _selectedOfferedType;
            var targetCityId = _selectedTargetCityId;

            if (!RefreshPermissions() || !CanManageEdicts)
            {
                ShowEdictError("Insufficient permissions.");
                return;
            }

            var plan = Edict.CheckEnact(_cityId, type, targetCityId);
            if (plan.Refusal != EdictRefusal.None)
            {
                ShowEdictError(DescribeRefusal(plan.Refusal, type));
                return;
            }

            var name = EdictRules.GetName(type);
            var target = string.IsNullOrWhiteSpace(targetCityId) ? null : DB.Get<WorldProperty>(targetCityId);
            var naming = target == null ? string.Empty : $", naming {target.CustomName}";
            var prompt = plan.TakesEffect
                ? $"Enact {name}{naming}? The treasury pays {plan.Charge:N0} cr now for the days left until the next upkeep check, then {EdictRules.GetWeeklyCost(type):N0} cr a week with upkeep. It cannot be repealed for {EdictRules.LockDays} days, and nothing is refunded."
                : $"Enact {name}{naming}? It costs nothing until that city's mayor also enacts an Alliance naming this city. It takes a slot while it waits, and its {EdictRules.LockDays}-day lock starts when both cities have enacted it.";

            ShowModal(prompt, () => ConfirmEnact(type, targetCityId));
        };

        private void ConfirmEnact(EdictType type, string targetCityId)
        {
            // The permission may have been revoked while the prompt was open.
            if (!RefreshPermissions() || !CanManageEdicts)
            {
                ShowEdictError("Insufficient permissions.");
                return;
            }

            var refusal = Edict.Enact(_cityId, type, targetCityId, GetObjectUUID(Player));
            if (refusal != EdictRefusal.None)
            {
                ShowEdictError(DescribeRefusal(refusal, type));
                return;
            }

            _selectedOfferedType = EdictType.Invalid;
            _selectedTargetCityId = string.Empty;
            Instructions = $"{EdictRules.GetName(type)} enacted.";
            InstructionsColor = GuiColor.Green;
        }

        public Action RepealEdict() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _heldEdictTypes.Count)
                return;

            var type = _heldEdictTypes[index];
            if (!RefreshPermissions() || !CanRepealEdicts)
            {
                ShowEdictError("Insufficient permissions.");
                return;
            }

            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null || !city.Edicts.TryGetValue(type, out var edict))
            {
                ShowEdictError(DescribeRefusal(EdictRefusal.NotHeld, type));
                return;
            }

            if (!EdictRules.CanRepeal(edict, DateTime.UtcNow))
            {
                ShowEdictError(DescribeRefusal(EdictRefusal.StillLocked, type));
                return;
            }

            var warning = edict.FromPlatform
                ? " It came from your election platform, so repealing it marks a broken promise on the city's record."
                : string.Empty;
            ShowModal(
                $"Repeal {EdictRules.GetName(type)}? Nothing is refunded for the days already paid.{warning}",
                () => ConfirmRepeal(type));
        };

        private void ConfirmRepeal(EdictType type)
        {
            if (!RefreshPermissions() || !CanRepealEdicts)
            {
                ShowEdictError("Insufficient permissions.");
                return;
            }

            var refusal = Edict.Repeal(_cityId, type, GetObjectUUID(Player));
            if (refusal != EdictRefusal.None)
            {
                ShowEdictError(DescribeRefusal(refusal, type));
                return;
            }

            Instructions = $"{EdictRules.GetName(type)} repealed.";
            InstructionsColor = GuiColor.Green;
        }

        private string DescribeRefusal(EdictRefusal refusal, EdictType type)
        {
            var city = DB.Get<WorldProperty>(_cityId);
            var conditions = city == null ? CityWideCondition.None : EdictChanges.GetConditions(city);
            return EdictRules.DescribeRefusal(refusal, type, conditions);
        }

        private void ShowEdictError(string message)
        {
            Instructions = message;
            InstructionsColor = GuiColor.Red;
        }
    }
}
