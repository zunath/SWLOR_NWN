using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CombatGuiViewModel : GuiViewModelBase<CombatGuiViewModel, GuiPayloadBase>
    {
        private const string BranchNone = "";
        private const string BranchSaber = "saber";
        private const string BranchGuns = "guns";

        private const string ResultModeNone = "none";
        private const string ResultModeForms = "forms";
        private const string ResultModeVariants = "variants";
        private const string ResultModeGuns = "guns";

        private static readonly System.Random _random = new();

        // Guns emotes are regular built-in animations (safe to play directly).
        private static readonly List<(string Name, Animation Animation)> _gunsEmotes = new()
        {
            ("Point Pistol", Animation.PointPistol),
        };

        private string _branch = BranchNone;
        private int _selectedFormIndex = -1;
        private CombatAnimation.CombatRole _selectedRole = CombatAnimation.CombatRole.Stance;
        private string _resultMode = ResultModeNone;

        // Parallel data backing the visible result list.
        private List<string> _resultResrefs = new();
        private List<Animation> _resultAnimations = new();

        // Category toggles
        public bool IsSaberToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsGunsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        // Selected form badge (Saber branch)
        public bool IsFormBadgeVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string FormBadgeText
        {
            get => Get<string>();
            set => Set(value);
        }

        // Role buttons (only visible when a saber form is chosen)
        public bool AreRolesVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsStanceToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAttackToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsDefenseToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        // Results list
        public GuiBindingList<string> ResultNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ResultToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ResultNames = new GuiBindingList<string>();
            ResultToggles = new GuiBindingList<bool>();
            ResetPanelState();
        }

        private void ResetPanelState()
        {
            _branch = BranchNone;
            _selectedFormIndex = -1;
            _resultMode = ResultModeNone;

            IsSaberToggled = false;
            IsGunsToggled = false;
            IsFormBadgeVisible = false;
            FormBadgeText = string.Empty;
            AreRolesVisible = false;
            IsStanceToggled = false;
            IsAttackToggled = false;
            IsDefenseToggled = false;

            ClearResults();
        }

        private void ClearResults()
        {
            _resultResrefs = new List<string>();
            _resultAnimations = new List<Animation>();
            ResultNames = new GuiBindingList<string>();
            ResultToggles = new GuiBindingList<bool>();
        }

        public Action OnSelectSaber() => () =>
        {
            _branch = BranchSaber;
            IsSaberToggled = true;
            IsGunsToggled = false;

            _selectedFormIndex = -1;
            IsFormBadgeVisible = false;
            FormBadgeText = string.Empty;
            AreRolesVisible = false;
            IsStanceToggled = false;
            IsAttackToggled = false;
            IsDefenseToggled = false;

            ShowFormsList();
        };

        public Action OnSelectGuns() => () =>
        {
            _branch = BranchGuns;
            IsGunsToggled = true;
            IsSaberToggled = false;

            _selectedFormIndex = -1;
            IsFormBadgeVisible = false;
            FormBadgeText = string.Empty;
            AreRolesVisible = false;

            ShowGunsList();
        };

        public Action OnClearForm() => () =>
        {
            _selectedFormIndex = -1;
            IsFormBadgeVisible = false;
            FormBadgeText = string.Empty;
            AreRolesVisible = false;
            IsStanceToggled = false;
            IsAttackToggled = false;
            IsDefenseToggled = false;

            ShowFormsList();
        };

        public Action OnSelectRole(int role) => () =>
        {
            if (_branch != BranchSaber || _selectedFormIndex < 0)
                return;

            _selectedRole = (CombatAnimation.CombatRole)role;
            IsStanceToggled = _selectedRole == CombatAnimation.CombatRole.Stance;
            IsAttackToggled = _selectedRole == CombatAnimation.CombatRole.Attack;
            IsDefenseToggled = _selectedRole == CombatAnimation.CombatRole.Defense;

            var form = CombatAnimation.Forms[_selectedFormIndex];
            var variants = form.GetVariants(_selectedRole);

            // Populate the variant list and roll a random one.
            _resultMode = ResultModeVariants;
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            _resultResrefs = new List<string>();
            _resultAnimations = new List<Animation>();

            var rolledIndex = variants.Count > 0 ? _random.Next(variants.Count) : -1;
            for (var i = 0; i < variants.Count; i++)
            {
                names.Add(variants[i]);
                toggles.Add(i == rolledIndex);
                _resultResrefs.Add(variants[i]);
                _resultAnimations.Add(Animation.Invalid);
            }

            ResultNames = names;
            ResultToggles = toggles;

            // Fire the random pick.
            if (rolledIndex >= 0)
                CombatAnimation.PlayCombat(Player, _resultResrefs[rolledIndex]);
        };

        public Action OnSelectResult() => () =>
        {
            var index = NuiGetEventArrayIndex();

            switch (_resultMode)
            {
                case ResultModeForms:
                    SelectForm(index);
                    break;
                case ResultModeVariants:
                    HighlightAndPlayVariant(index);
                    break;
                case ResultModeGuns:
                    PlayGuns(index);
                    break;
            }
        };

        private void SelectForm(int index)
        {
            if (index < 0 || index >= CombatAnimation.Forms.Count)
                return;

            _selectedFormIndex = index;
            var form = CombatAnimation.Forms[index];

            IsFormBadgeVisible = true;
            FormBadgeText = form.Name;
            AreRolesVisible = true;
            IsStanceToggled = false;
            IsAttackToggled = false;
            IsDefenseToggled = false;

            _resultMode = ResultModeNone;
            ClearResults();
        }

        private void HighlightAndPlayVariant(int index)
        {
            if (index < 0 || index >= _resultResrefs.Count)
                return;

            var toggles = new GuiBindingList<bool>();
            for (var i = 0; i < ResultNames.Count; i++)
                toggles.Add(i == index);
            ResultToggles = toggles;

            CombatAnimation.PlayCombat(Player, _resultResrefs[index]);
        }

        private void PlayGuns(int index)
        {
            if (index < 0 || index >= _resultAnimations.Count)
                return;

            var toggles = new GuiBindingList<bool>();
            for (var i = 0; i < ResultNames.Count; i++)
                toggles.Add(i == index);
            ResultToggles = toggles;

            CombatAnimation.PlayStandardLooping(Player, _resultAnimations[index]);
        }

        private void ShowFormsList()
        {
            _resultMode = ResultModeForms;
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            _resultResrefs = new List<string>();
            _resultAnimations = new List<Animation>();

            foreach (var form in CombatAnimation.Forms)
            {
                names.Add(form.Name);
                toggles.Add(false);
                _resultResrefs.Add(string.Empty);
                _resultAnimations.Add(Animation.Invalid);
            }

            ResultNames = names;
            ResultToggles = toggles;
        }

        private void ShowGunsList()
        {
            _resultMode = ResultModeGuns;
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            _resultResrefs = new List<string>();
            _resultAnimations = new List<Animation>();

            foreach (var (name, animation) in _gunsEmotes)
            {
                names.Add(name);
                toggles.Add(false);
                _resultResrefs.Add(string.Empty);
                _resultAnimations.Add(animation);
            }

            ResultNames = names;
            ResultToggles = toggles;
        }

        public Action OnClickReset() => () =>
        {
            CombatAnimation.ResetAll(Player);
            SendMessageToPC(Player, "Animazioni resettate.");
        };
    }
}
