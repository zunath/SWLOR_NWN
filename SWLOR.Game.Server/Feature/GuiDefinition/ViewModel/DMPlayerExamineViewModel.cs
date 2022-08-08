using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMPlayerExamineViewModel: GuiViewModelBase<DMPlayerExamineViewModel, DMPlayerExaminePayload>
    {
        [NWNEventHandler("examine_bef")]
        public static void ExaminePlayer()
        {
            var target = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));

            if (!GetIsPC(target) && !GetIsDM(target) && !GetIsDMPossessed(target))
                return;

            var payload = new DMPlayerExaminePayload(target);

            var dm = OBJECT_SELF;
            SetGuiPanelDisabled(dm, GuiPanel.ExamineCreature, true);
            Gui.TogglePlayerWindow(dm, GuiWindowType.DMPlayerExamine, payload);
            DelayCommand(1f, () => SetGuiPanelDisabled(dm, GuiPanel.ExamineCreature, false));
        }

        private uint _target;

        public const string PartialView = "PARTIAL";

        public const string DetailView = "DETAIL_VIEW";
        public const string SkillsView = "SKILLS_VIEW";
        public const string PerksView = "PERKS_VIEW";
        public const string NotesView = "NOTES_VIEW";

        public bool IsDetailsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsSkillsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsPerksToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsNotesToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CharacterType
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> SkillNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> SkillLevels
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> PerkLevels
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        protected override void Initialize(DMPlayerExaminePayload initialPayload)
        {
            _target = initialPayload.Target;

            IsDetailsToggled = true;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;

            ChangePartialView(PartialView, DetailView);
            LoadTargetDetails();

            WatchOnClient(model => model.Description);
        }

        private void LoadTargetDetails()
        {
            Name = GetName(_target);
            Description = GetDescription(_target);
            CharacterType = GetClassByPosition(1, _target) == ClassType.ForceSensitive
                ? "Force Sensitive"
                : "Standard";
        }

        private void LoadTargetSkills()
        {
            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            var skillNames = new GuiBindingList<string>();
            var skillLevels = new GuiBindingList<int>();
            foreach (var (type, detail) in Skill.GetAllActiveSkills())
            {
                skillNames.Add(detail.Name);
                skillLevels.Add(dbPlayer.Skills[type].Rank);
            }

            SkillNames = skillNames;
            SkillLevels = skillLevels;
        }

        private void LoadTargetPerks()
        {
            var playerId = GetObjectUUID(_target);
            var dbPlayer = DB.Get<Player>(playerId);

            var perkNames = new GuiBindingList<string>();
            var perkLevels = new GuiBindingList<int>();
            foreach (var (type, level) in dbPlayer.Perks)
            {
                var detail = Perk.GetPerkDetails(type);
                perkNames.Add(detail.Name);
                perkLevels.Add(level);
            }

            PerkNames = perkNames;
            PerkLevels = perkLevels;
        }

        private void LoadTargetNotes()
        {

        }

        public Action OnClickDetails() => () =>
        {
            IsDetailsToggled = true;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = false;

            ChangePartialView(PartialView, DetailView);
            LoadTargetDetails();
        };

        public Action OnClickSkills() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = true;
            IsPerksToggled = false;
            IsNotesToggled = false;

            ChangePartialView(PartialView, SkillsView);
            LoadTargetSkills();
        };

        public Action OnClickPerks() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = true;
            IsNotesToggled = false;

            ChangePartialView(PartialView, PerksView);
            LoadTargetPerks();
        };

        public Action OnClickNotes() => () =>
        {
            IsDetailsToggled = false;
            IsSkillsToggled = false;
            IsPerksToggled = false;
            IsNotesToggled = true;

            ChangePartialView(PartialView, NotesView);
            LoadTargetNotes();
        };
    }
}
