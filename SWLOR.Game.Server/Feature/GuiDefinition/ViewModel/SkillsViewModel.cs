using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool _hasLoaded;

        private readonly List<SkillType> _viewableSkills;

        public BindingList<string> SkillNames
        {
            get => Get<BindingList<string>>();
            set => Set(value);
        }

        public BindingList<int> Levels
        {
            get => Get<BindingList<int>>();
            set => Set(value);
        }

        public BindingList<string> Titles
        {
            get => Get<BindingList<string>>();
            set => Set(value);
        }

        public BindingList<float> Progresses
        {
            get => Get<BindingList<float>>();
            set => Set(value);
        }

        public BindingList<string> DecayLockTexts
        {
            get => Get<BindingList<string>>();
            set => Set(value);
        }

        public BindingList<GuiColor> DecayLockColors
        {
            get => Get<BindingList<GuiColor>>();
            set => Set(value);
        }

        public BindingList<bool> DecayLockButtonEnabled
        {
            get => Get<BindingList<bool>>();
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
            SkillNames = new BindingList<string>();
            Levels = new BindingList<int>();
            Titles = new BindingList<string>();
            Progresses = new BindingList<float>();
            DecayLockTexts = new BindingList<string>();
            DecayLockColors = new BindingList<GuiColor>();
            DecayLockButtonEnabled = new BindingList<bool>();
        }


        public Action OnLoadWindow() => () =>
        {
            SelectedCategoryId = 0;
            var sw = new Stopwatch();
            sw.Start();

            if (!_hasLoaded)
            {
                var skills = Skill.GetAllActiveSkills();
                LoadSkills(skills);

                _hasLoaded = true;
            }

            WatchOnClient(model => model.SelectedCategoryId);

            sw.Stop();
            Console.WriteLine($"OnLoadWindow took {sw.ElapsedMilliseconds}ms");
        };

        private void LoadSkills(Dictionary<SkillType, SkillAttribute> skills)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            SkillNames.Clear();
            Levels.Clear();
            Titles.Clear();
            Progresses.Clear();
            DecayLockTexts.Clear();
            DecayLockColors.Clear();

            foreach (var (type, skill) in skills)
            {
                var playerSkill = dbPlayer.Skills[type];

                _viewableSkills.Add(type);
                SkillNames.Add(skill.Name);
                Levels.Add(playerSkill.Rank);
                Titles.Add(GetTitle(playerSkill.Rank));
                Progresses.Add(CalculateProgress(playerSkill.Rank, playerSkill.XP));
                DecayLockTexts.Add(GetDecayLockText(playerSkill.IsLocked, skill.ContributesToSkillCap));
                DecayLockColors.Add(GetDecayLockColor(playerSkill.IsLocked, skill.ContributesToSkillCap));
                DecayLockButtonEnabled.Add(skill.ContributesToSkillCap);
            }
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
