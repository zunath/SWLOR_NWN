using System;
using System.Collections.Generic;
using System.Diagnostics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SkillsViewModel : GuiViewModelBase<SkillsViewModel>
    {
        private readonly List<SkillType> _viewableSkills;

        public GuiBindingList<string> SkillNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> Levels
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        public GuiBindingList<string> Titles
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<float> Progresses
        {
            get => Get<GuiBindingList<float>>();
            set => Set(value);
        }

        public GuiBindingList<string> DecayLockTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> DecayLockColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<bool> DecayLockButtonEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (value == 0)
                {
                    LoadSkills(Skill.GetAllActiveSkills());
                }
                else
                {
                    var skillsInCategory = Skill.GetActiveSkillsByCategory((SkillCategoryType)value);
                    LoadSkills(skillsInCategory);
                }
            }
        }


        public SkillsViewModel()
        {
            _viewableSkills = new List<SkillType>();
            SkillNames = new GuiBindingList<string>();
            Levels = new GuiBindingList<int>();
            Titles = new GuiBindingList<string>();
            Progresses = new GuiBindingList<float>();
            DecayLockTexts = new GuiBindingList<string>();
            DecayLockColors = new GuiBindingList<GuiColor>();
            DecayLockButtonEnabled = new GuiBindingList<bool>();
        }


        public Action OnLoadWindow() => () =>
        {
            SelectedCategoryId = 0;
            LoadSkills(Skill.GetAllActiveSkills());
            WatchOnClient(model => model.SelectedCategoryId);
        };

        private void LoadSkills(Dictionary<SkillType, SkillAttribute> skills)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            _viewableSkills.Clear();
            var skillNames = new GuiBindingList<string>();
            var levels = new GuiBindingList<int>();
            var titles = new GuiBindingList<string>();
            var progresses = new GuiBindingList<float>();
            var decayLockTexts = new GuiBindingList<string>();
            var decayLockColors = new GuiBindingList<GuiColor>();
            var decayLockButtonEnabled = new GuiBindingList<bool>();
            
            foreach (var (type, skill) in skills)
            {
                var playerSkill = dbPlayer.Skills[type];

                _viewableSkills.Add(type);
                skillNames.Add(skill.Name);
                levels.Add(playerSkill.Rank);
                titles.Add(GetTitle(playerSkill.Rank));
                progresses.Add(CalculateProgress(playerSkill.Rank, playerSkill.XP));
                decayLockTexts.Add(GetDecayLockText(playerSkill.IsLocked, skill.ContributesToSkillCap));
                decayLockColors.Add(GetDecayLockColor(playerSkill.IsLocked, skill.ContributesToSkillCap));
                decayLockButtonEnabled.Add(skill.ContributesToSkillCap);
            }

            SkillNames = skillNames;
            Levels = levels;
            Titles = titles;
            Progresses = progresses;
            DecayLockTexts = decayLockTexts;
            DecayLockColors = decayLockColors;
            DecayLockButtonEnabled = decayLockButtonEnabled;
        }

        private string GetTitle(int rank)
        {
            switch (rank)
            {
                case <= 3:
                    return "Untrained";
                case <= 7:
                    return "Neophyte";
                case <= 13:
                    return "Novice";
                case <= 20:
                    return "Apprentice";
                case <= 35:
                    return "Journeyman";
                case <= 50:
                    return "Expert";
                case <= 65:
                    return "Adept";
                case <= 80:
                    return "Master";
                case <= 100:
                    return "Grandmaster";
            }

            return "Untrained";
        }

        private float CalculateProgress(int rank, float xp)
        {
            var nextLevelXP = Skill.GetRequiredXP(rank + 1);
            return nextLevelXP / xp;
        }

        private string GetDecayLockText(bool isLocked, bool contributesToSkillCap)
        {
            if (!contributesToSkillCap)
                return "N/A";

            return isLocked ? "LOCKED" : "UNLOCKED";
        }

        private GuiColor GetDecayLockColor(bool isLocked, bool contributesToSkillCap)
        {
            if (!contributesToSkillCap)
                return new GuiColor(169, 169, 169);

            if (isLocked)
                return new GuiColor(255, 0, 0);
            else
                return new GuiColor(0, 255, 0);
        }

        public Action ToggleDecayLock() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var index = NuiGetEventArrayIndex();
            var selectedSkill = _viewableSkills[index];
            var isLocked = !dbPlayer.Skills[selectedSkill].IsLocked;

            dbPlayer.Skills[selectedSkill].IsLocked = isLocked;

            DB.Set(playerId, dbPlayer);

            DecayLockColors[index] = GetDecayLockColor(isLocked, true);
            DecayLockTexts[index] = GetDecayLockText(isLocked, true);
        };
    }
}
