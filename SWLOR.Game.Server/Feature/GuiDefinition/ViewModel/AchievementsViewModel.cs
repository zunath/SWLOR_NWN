using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AchievementsViewModel: GuiViewModelBase<AchievementsViewModel, GuiPayloadBase>
    {
        private int SelectedIndex { get; set; }
        private readonly List<AchievementType> _types = new();

        public bool ShowAll
        {
            get => Get<bool>();
            set
            {
                Set(value);
                LoadAchievements();
            }
        }

        public GuiBindingList<string> Names
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> Toggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> Colors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AcquiredDate
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ShowAll = true;
            LoadAchievements();
            LoadAchievement();

            WatchOnClient(model => model.ShowAll);
        }

        private void LoadAchievements()
        {
            var cdKey = GetPCPublicCDKey(Player);
            var dbAccount = DB.Get<Account>(cdKey) ?? new Account();

            var names = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var toggles = new GuiBindingList<bool>();
            _types.Clear();

            if (ShowAll)
            {
                foreach (var (type, detail) in Achievement.GetActiveAchievements())
                {
                    _types.Add(type);
                    names.Add(detail.Name);
                    colors.Add(dbAccount.Achievements.ContainsKey(type)
                        ? new GuiColor(0, 255, 0)
                        : new GuiColor(255, 0, 0));
                    toggles.Add(false);
                }
            }
            else
            {
                foreach (var (type, _) in dbAccount.Achievements)
                {
                    _types.Add(type);
                    var detail = Achievement.GetAchievement(type);

                    names.Add(detail.Name);
                    colors.Add(new GuiColor(0, 255, 0));
                    toggles.Add(false);
                }
            }

            SelectedIndex = -1;
            Names = names;
            Colors = colors;
            Toggles = toggles;
        }

        private void LoadAchievement()
        {
            if (SelectedIndex <= -1)
            {
                Name = string.Empty;
                Description = string.Empty;
                AcquiredDate = string.Empty;
                return;
            }

            var type = _types[SelectedIndex];
            var achievement = Achievement.GetAchievement(type);
            var cdKey = GetPCPublicCDKey(Player);
            var dbAccount = DB.Get<Account>(cdKey) ?? new Account();

            Name = achievement.Name;
            Description = achievement.Description;
            AcquiredDate = dbAccount.Achievements.ContainsKey(type)
                ? $"Acquired: {dbAccount.Achievements[type].ToString("g")}"
                : string.Empty;
        }

        public Action OnClickAchievement() => () =>
        {
            if (SelectedIndex > -1)
                Toggles[SelectedIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedIndex = index;
            Toggles[SelectedIndex] = true;

            LoadAchievement();
        };
    }
}
