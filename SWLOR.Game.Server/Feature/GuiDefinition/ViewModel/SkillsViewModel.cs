using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SkillsViewModel : GuiViewModelBase<SkillsViewModel>
    {
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

        public BindingList<bool> DecayLocks
        {
            get => Get<BindingList<bool>>();
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

        public SkillsViewModel()
        {
            SkillNames = new BindingList<string>();
            Levels = new BindingList<int>();
            Titles = new BindingList<string>();
            Progresses = new BindingList<float>();
            DecayLocks = new BindingList<bool>();
            DecayLockTexts = new BindingList<string>();
            DecayLockColors = new BindingList<GuiColor>();
        }


        public Action OnLoadWindow() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var skills = Skill.GetAllActiveSkills();

            foreach (var (type, skill) in skills)
            {
                var playerSkill = dbPlayer.Skills[type];
                SkillNames.Add(skill.Name);
                Levels.Add(playerSkill.Rank);
                Titles.Add(GetTitle(playerSkill.Rank));
                Progresses.Add(CalculateProgress(playerSkill.Rank, playerSkill.XP));
                DecayLocks.Add(playerSkill.IsLocked);
                DecayLockTexts.Add(GetDecayLockText(playerSkill.IsLocked));
                DecayLockColors.Add(GetDecayLockColor(playerSkill.IsLocked));
            }
        };

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

        private string GetDecayLockText(bool isLocked)
        {
            return isLocked ? "LOCKED" : "UNLOCKED";
        }

        private GuiColor GetDecayLockColor(bool isLocked)
        {
            if (isLocked)
                return new GuiColor(255, 0, 0);
            else
                return new GuiColor(0, 255, 0);
        }

        public Action ToggleDecayLock() => () =>
        {

        };
    }
}
