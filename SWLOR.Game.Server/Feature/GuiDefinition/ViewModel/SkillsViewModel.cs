using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SkillsViewModel : GuiViewModelBase<SkillsViewModel, GuiPayloadBase>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private readonly List<SkillType> _viewableSkills = new();

        // One row DTO per skill, replacing the eleven hand-synced parallel
        // GuiBindingList instances LoadSkills used to build in lockstep.
        private sealed class SkillEntry
        {
            public SkillType Type { get; }
            public string Name { get; }
            public int Level { get; }
            public string Title { get; }
            public float Progress { get; }
            public string RawXPAmount { get; }
            public string Description { get; }
            public string DecayLockText { get; }
            public GuiColor DecayLockColor { get; }
            public bool DecayLockEnabled { get; }
            public bool DistributeRPXPEnabled { get; }
            public string DistributeRPXPTooltip { get; }

            public SkillEntry(
                SkillType type,
                string name,
                int level,
                string title,
                float progress,
                string rawXPAmount,
                string description,
                string decayLockText,
                GuiColor decayLockColor,
                bool decayLockEnabled,
                bool distributeRPXPEnabled,
                string distributeRPXPTooltip)
            {
                Type = type;
                Name = name;
                Level = level;
                Title = title;
                Progress = progress;
                RawXPAmount = rawXPAmount;
                Description = description;
                DecayLockText = decayLockText;
                DecayLockColor = decayLockColor;
                DecayLockEnabled = decayLockEnabled;
                DistributeRPXPEnabled = distributeRPXPEnabled;
                DistributeRPXPTooltip = distributeRPXPTooltip;
            }
        }

        private static readonly GuiTableSource<SkillsViewModel, SkillEntry> SkillsTable =
            new GuiTableSource<SkillsViewModel, SkillEntry>()
                .Column((m, v) => m.SkillNames = v, r => r.Name)
                .Column((m, v) => m.Levels = v, r => r.Level)
                .Column((m, v) => m.Titles = v, r => r.Title)
                .Column((m, v) => m.Progresses = v, r => r.Progress)
                .Column((m, v) => m.RawXPAmounts = v, r => r.RawXPAmount)
                .Column((m, v) => m.Descriptions = v, r => r.Description)
                .Column((m, v) => m.DecayLockTexts = v, r => r.DecayLockText)
                .Column((m, v) => m.DecayLockColors = v, r => r.DecayLockColor)
                .Column((m, v) => m.DecayLockButtonEnabled = v, r => r.DecayLockEnabled)
                .Column((m, v) => m.DistributeRPXPButtonEnabled = v, r => r.DistributeRPXPEnabled)
                .Column((m, v) => m.DistributeRPXPButtonTooltips = v, r => r.DistributeRPXPTooltip);

        public string AvailableXP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string XPDebt
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

        public GuiBindingList<string> RawXPAmounts
        {
            get => Get<GuiBindingList<string>>();
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
                    LoadSkills(Skill.GetAllActiveSkillsForDisplay());
                }
                else
                {
                    var skillsInCategory = Skill.GetActiveSkillsByCategoryForDisplay((SkillCategoryType)value);
                    LoadSkills(skillsInCategory);
                }
            }
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedCategoryId = 0;
            LoadSkills(Skill.GetAllActiveSkillsForDisplay());
            WatchOnClient(model => model.SelectedCategoryId);
        }

        private void LoadSkills(Dictionary<SkillType, SkillAttribute> skills)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            var rows = new List<SkillEntry>();

            foreach (var (type, skill) in skills)
            {
                // Exclude any skills which are restricted by character type.
                if (skill.CharacterTypeRestriction != CharacterType.Invalid &&
                    skill.CharacterTypeRestriction != dbPlayer.CharacterType)
                {
                    continue;
                }

                var playerSkill = dbPlayer.Skills[type];

                rows.Add(new SkillEntry(
                    type,
                    skill.Name,
                    playerSkill.Rank,
                    GetTitle(playerSkill.Rank),
                    CalculateProgress(type, playerSkill.Rank, playerSkill.XP),
                    CalculateRawXPAmounts(type, playerSkill.Rank, playerSkill.XP),
                    skill.Description,
                    GetDecayLockText(playerSkill.IsLocked, skill.ContributesToSkillCap),
                    GetDecayLockColor(playerSkill.IsLocked, skill.ContributesToSkillCap),
                    skill.ContributesToSkillCap,
                    dbPlayer.UnallocatedXP > 0,
                    $"Distribute RP XP ({dbPlayer.UnallocatedXP})"));
            }

            // Row-index lookups (ToggleDecayLock, OnClickDistributeRPXP, the
            // SkillXP refresh) index into this in lockstep with the bound lists.
            _viewableSkills.Clear();
            foreach (var row in rows)
                _viewableSkills.Add(row.Type);

            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";
            XPDebt = $"XP Debt: {dbPlayer.XPDebt}";
            SkillsTable.Refresh(this, rows);
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

        private float CalculateProgress(SkillType type, int rank, int xp)
        {
            var skill = Skill.GetSkillDetails(type);
            if (rank >= skill.MaxRank)
                return 1f;

            var nextLevelXP = Skill.GetRequiredXP(rank);
            return (float)xp / nextLevelXP;
        }

        private string CalculateRawXPAmounts(SkillType type, int rank, int xp)
        {
            var skill = Skill.GetSkillDetails(type);
            if (rank >= skill.MaxRank)
                return "0 / 0";

            var nextLevelXP = Skill.GetRequiredXP(rank);
            return $"{xp} / {nextLevelXP}";
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
                return GuiColor.Grey;

            if (isLocked)
                return GuiColor.Red;
            else
                return GuiColor.Green;
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
            if (GetResRef(GetArea(Player)) == "char_migration")
            {
                FloatingTextStringOnCreature($"XP cannot be distributed in this area.", Player, false);
                return;
            }

            if (GetIsDead(Player) || GetCurrentHitPoints(Player) <= 0)
            {
                FloatingTextStringOnCreature($"XP cannot be distributed while dead.", Player, false);
                return;
            }

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
            foreach (var skill in payload.ModifiedSkills)
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                var index = _viewableSkills.IndexOf(skill);
                if (index < 0)
                    continue;

                var pcSkill = dbPlayer.Skills[skill];

                Levels[index] = pcSkill.Rank;
                Titles[index] = GetTitle(pcSkill.Rank);
                Progresses[index] = CalculateProgress(skill, pcSkill.Rank, pcSkill.XP);
                RawXPAmounts[index] = CalculateRawXPAmounts(skill, pcSkill.Rank, pcSkill.XP);
            }
        }

        public void Refresh(RPXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";
            XPDebt = $"XP Debt: {dbPlayer.XPDebt}";

            var distributeTooltips = new GuiBindingList<string>();
            var distributeToggles = new GuiBindingList<bool>();

            var distributeText = $"Distribute RP XP ({dbPlayer.UnallocatedXP})";
            foreach(var unused in _viewableSkills)
            {
                distributeTooltips.Add(distributeText);
                distributeToggles.Add(dbPlayer.UnallocatedXP > 0);
            }

            DistributeRPXPButtonTooltips = distributeTooltips;
            DistributeRPXPButtonEnabled = distributeToggles;
        }
    }
}
