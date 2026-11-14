using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HouseSpecializationService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HouseSpecializationViewModel : GuiViewModelBase<HouseSpecializationViewModel, HouseSpecializationPayload>
    {
        public const string MainContentElement = "housespecialization_content";
        public const string MainContentPartial = "HOUSESPECIALIZATION_MAIN";

        private string _houseId;
        private readonly List<SkillType> _skills = new();
        private int _selectedSkillIndex = -1;

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string CurrentText { get => Get<string>(); set => Set(value); }
        public string NextChangeText { get => Get<string>(); set => Set(value); }
        public string EffectText { get => Get<string>(); set => Set(value); }
        public string MessageText { get => Get<string>(); set => Set(value); }
        public string SkillHeaderText { get => Get<string>(); set => Set(value); }

        public GuiBindingList<string> SkillNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> SkillToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public bool CanSetWorkshop { get => Get<bool>(); set => Set(value); }
        public bool CanSetStorehouse { get => Get<bool>(); set => Set(value); }
        public bool CanSetGreenhouse { get => Get<bool>(); set => Set(value); }
        public string WorkshopTooltip { get => Get<string>(); set => Set(value); }
        public string StorehouseTooltip { get => Get<string>(); set => Set(value); }
        public string GreenhouseTooltip { get => Get<string>(); set => Set(value); }

        protected override void Initialize(HouseSpecializationPayload initialPayload)
        {
            _houseId = initialPayload?.HouseId ?? string.Empty;

            // Every bound property gets a value before the view is shown.
            HeaderText = string.Empty;
            CurrentText = string.Empty;
            NextChangeText = string.Empty;
            EffectText = string.Empty;
            MessageText = string.Empty;
            SkillHeaderText = string.Empty;
            SkillNames = new GuiBindingList<string>();
            SkillToggles = new GuiBindingList<bool>();
            CanSetWorkshop = false;
            CanSetStorehouse = false;
            CanSetGreenhouse = false;
            WorkshopTooltip = string.Empty;
            StorehouseTooltip = string.Empty;
            GreenhouseTooltip = string.Empty;

            LoadSkills();
            Refresh();
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        private void LoadSkills()
        {
            _skills.Clear();
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            foreach (var (skill, detail) in Skill.GetActiveCraftingSkills().OrderBy(pair => pair.Value.Name, StringComparer.Ordinal))
            {
                _skills.Add(skill);
                names.Add(detail.Name);
                toggles.Add(false);
            }

            _selectedSkillIndex = -1;
            SkillNames = names;
            SkillToggles = toggles;
        }

        /// <summary>Reads the house again and sets what the window shows and which choices are on.</summary>
        private void Refresh()
        {
            var house = string.IsNullOrWhiteSpace(_houseId) ? null : DB.Get<WorldProperty>(_houseId);
            if (house is not { PropertyType: PropertyType.House })
            {
                HeaderText = "This house is gone.";
                CurrentText = string.Empty;
                NextChangeText = string.Empty;
                EffectText = string.Empty;
                SkillHeaderText = string.Empty;
                CanSetWorkshop = false;
                CanSetStorehouse = false;
                CanSetGreenhouse = false;
                return;
            }

            var playerId = GetObjectUUID(Player);
            var isKeeper = HouseSpecializations.IsKeeper(house.Id, playerId);

            HeaderText = $"{house.CustomName} - Specialization";
            CurrentText = $"Current: {HouseSpecializationRules.Describe(house.HouseSpecialization, house.WorkshopSkill)}";
            var next = HouseSpecializationRules.GetNextChangeUtc(house.HouseSpecializationChangedUtc);
            NextChangeText = !next.HasValue || next.Value <= DateTime.UtcNow
                ? "It can be changed now."
                : $"It can change again on {next.Value:yyyy-MM-dd} UTC.";

            EffectText = $"Workshop: {HouseSpecializationRules.DescribeEffect(HouseSpecializationType.Workshop)}\n" +
                         $"Storehouse: {HouseSpecializationRules.DescribeEffect(HouseSpecializationType.Storehouse)}\n" +
                         $"Greenhouse: {HouseSpecializationRules.DescribeEffect(HouseSpecializationType.Greenhouse)}";
            SkillHeaderText = isKeeper ? "Workshop skill (pick one for a Workshop):" : "Only the house's keeper can change the specialization.";

            var skill = _selectedSkillIndex >= 0 && _selectedSkillIndex < _skills.Count ? _skills[_selectedSkillIndex] : SkillType.Invalid;
            SetChoice(house, playerId, HouseSpecializationType.Workshop, skill, enabled => CanSetWorkshop = enabled, tip => WorkshopTooltip = tip);
            SetChoice(house, playerId, HouseSpecializationType.Storehouse, SkillType.Invalid, enabled => CanSetStorehouse = enabled, tip => StorehouseTooltip = tip);
            SetChoice(house, playerId, HouseSpecializationType.Greenhouse, SkillType.Invalid, enabled => CanSetGreenhouse = enabled, tip => GreenhouseTooltip = tip);
        }

        private static void SetChoice(
            WorldProperty house,
            string playerId,
            HouseSpecializationType type,
            SkillType skill,
            System.Action<bool> setEnabled,
            System.Action<string> setTooltip)
        {
            var refusal = HouseSpecializations.CheckChange(house, playerId, type, skill);
            setEnabled(refusal == HouseSpecializationRefusal.None);
            setTooltip(HouseSpecializationRules.Describe(
                refusal,
                HouseSpecializationRules.GetNextChangeUtc(house.HouseSpecializationChangedUtc)));
        }

        public Action OnClickSkill() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _skills.Count)
                return;

            _selectedSkillIndex = index;
            for (var skill = 0; skill < SkillToggles.Count; skill++)
                SkillToggles[skill] = skill == index;

            Refresh();
        };

        public Action OnClickWorkshop() => () =>
        {
            if (_selectedSkillIndex < 0 || _selectedSkillIndex >= _skills.Count)
                return;

            var skill = _skills[_selectedSkillIndex];
            Confirm(HouseSpecializationType.Workshop, skill, $"Make this house a Workshop for {SkillNames[_selectedSkillIndex]}?");
        };

        public Action OnClickStorehouse() => () =>
            Confirm(HouseSpecializationType.Storehouse, SkillType.Invalid, "Make this house a Storehouse?");

        public Action OnClickGreenhouse() => () =>
            Confirm(HouseSpecializationType.Greenhouse, SkillType.Invalid, "Make this house a Greenhouse?");

        private void Confirm(HouseSpecializationType type, SkillType skill, string question)
        {
            ShowModal(
                $"{question} It cannot change again for {HouseSpecializationRules.ChangeIntervalDays} days, and nothing stored or planted is destroyed.",
                () =>
                {
                    var refusal = HouseSpecializations.Change(_houseId, GetObjectUUID(Player), type, skill);
                    MessageText = refusal == HouseSpecializationRefusal.None
                        ? $"The house is now a {HouseSpecializationRules.Describe(type, skill)}."
                        : HouseSpecializationRules.Describe(refusal, DB.Get<WorldProperty>(_houseId) is { } house
                            ? HouseSpecializationRules.GetNextChangeUtc(house.HouseSpecializationChangedUtc)
                            : null);
                    Refresh();
                },
                null,
                "Change",
                "Keep");
        }
    }
}
