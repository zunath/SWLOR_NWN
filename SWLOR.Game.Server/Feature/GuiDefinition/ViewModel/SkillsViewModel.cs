﻿using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SkillsViewModel : GuiViewModelBase<SkillsViewModel, GuiPayloadBase>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private readonly List<SkillType> _viewableSkills = new();

        public string AvailableXP
        {
            get => Get<string>();
            set => Set(value);
        }

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

        public GuiBindingList<string> Descriptions
        {
            get => Get<GuiBindingList<string>>();
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

        public GuiBindingList<bool> DistributeRPXPButtonEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> DistributeRPXPButtonTooltips
        {
            get => Get<GuiBindingList<string>>();
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
        
        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedCategoryId = 0;
            LoadSkills(Skill.GetAllActiveSkills());
            WatchOnClient(model => model.SelectedCategoryId);
        }

        private void LoadSkills(Dictionary<SkillType, SkillAttribute> skills)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            _viewableSkills.Clear();
            var skillNames = new GuiBindingList<string>();
            var levels = new GuiBindingList<int>();
            var titles = new GuiBindingList<string>();
            var progresses = new GuiBindingList<float>();
            var descriptions = new GuiBindingList<string>();
            var decayLockTexts = new GuiBindingList<string>();
            var decayLockColors = new GuiBindingList<GuiColor>();
            var decayLockButtonEnabled = new GuiBindingList<bool>();
            var distributeRPXPButtonEnabled = new GuiBindingList<bool>();
            var distributeRPXPTooltips = new GuiBindingList<string>();

            foreach (var (type, skill) in skills)
            {
                var playerSkill = dbPlayer.Skills[type];

                _viewableSkills.Add(type);
                skillNames.Add(skill.Name);
                levels.Add(playerSkill.Rank);
                titles.Add(GetTitle(playerSkill.Rank));
                progresses.Add(CalculateProgress(playerSkill.Rank, playerSkill.XP));
                descriptions.Add(skill.Description);
                decayLockTexts.Add(GetDecayLockText(playerSkill.IsLocked, skill.ContributesToSkillCap));
                decayLockColors.Add(GetDecayLockColor(playerSkill.IsLocked, skill.ContributesToSkillCap));
                decayLockButtonEnabled.Add(skill.ContributesToSkillCap);
                distributeRPXPButtonEnabled.Add(dbPlayer.UnallocatedXP > 0);
                distributeRPXPTooltips.Add($"Distribute RP XP ({dbPlayer.UnallocatedXP})");
            }

            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";
            SkillNames = skillNames;
            Levels = levels;
            Titles = titles;
            Progresses = progresses;
            Descriptions = descriptions;
            DecayLockTexts = decayLockTexts;
            DecayLockColors = decayLockColors;
            DecayLockButtonEnabled = decayLockButtonEnabled;
            DistributeRPXPButtonEnabled = distributeRPXPButtonEnabled;
            DistributeRPXPButtonTooltips = distributeRPXPTooltips;
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
            return xp / nextLevelXP;
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

            DB.Set(dbPlayer);

            DecayLockColors[index] = GetDecayLockColor(isLocked, true);
            DecayLockTexts[index] = GetDecayLockText(isLocked, true);
        };

        public Action OnClickDistributeRPXP() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var index = NuiGetEventArrayIndex();
            var name = SkillNames[index];

            var payload = new RPXPPayload
            {
                MaxRPXP = dbPlayer.UnallocatedXP,
                Skill = _viewableSkills[index],
                SkillName = name
            };

            Gui.TogglePlayerWindow(Player, GuiWindowType.DistributeRPXP, payload);
        };

        public void Refresh(SkillXPRefreshEvent payload)
        {
            var skill = payload.Type;
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var index = _viewableSkills.IndexOf(skill);
            var pcSkill = dbPlayer.Skills[skill];

            Levels[index] = pcSkill.Rank;
            Titles[index] = GetTitle(pcSkill.Rank);
            Progresses[index] = CalculateProgress(pcSkill.Rank, pcSkill.XP);
        }

        public void Refresh(RPXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";
        }
    }
}
