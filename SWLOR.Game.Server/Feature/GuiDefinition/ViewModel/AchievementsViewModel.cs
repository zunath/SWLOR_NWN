using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AchievementsViewModel: GuiViewModelBase<AchievementsViewModel, GuiPayloadBase>,
        IGuiRefreshable<AchievementUnlockedRefreshEvent>
    {
        private const int EntriesPerPage = 25;
        private int SelectedIndex { get; set; }
        private readonly List<AchievementType> _types = new();

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

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                UpdatePagination();
                Search();
            }
        }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }
        

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedPageIndex = -1;
            UpdatePagination();
            Search();

            WatchOnClient(model => model.SelectedPageIndex);
        }

        private void Search()
        {
            var cdKey = GetPCPublicCDKey(Player);
            var dbAccount = DB.Get<Account>(cdKey) ?? new Account(cdKey);
            var achievements = Achievement.GetActiveAchievements()
                .Skip(SelectedPageIndex * EntriesPerPage)
                .Take(EntriesPerPage);

            var names = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var toggles = new GuiBindingList<bool>();
            _types.Clear();

            foreach (var (type, detail) in achievements)
            {
                _types.Add(type);
                names.Add(detail.Name);
                colors.Add(dbAccount.Achievements.ContainsKey(type)
                    ? GuiColor.Green
                    : GuiColor.Red);
                toggles.Add(false);
            }

            SelectedIndex = -1;
            Names = names;
            Colors = colors;
            Toggles = toggles;

            LoadAchievement();
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
            var dbAccount = DB.Get<Account>(cdKey) ?? new Account(cdKey);

            Name = achievement.Name;
            Description = achievement.Description;
            AcquiredDate = dbAccount.Achievements.ContainsKey(type)
                ? $"Acquired: {dbAccount.Achievements[type].ToString("g")}"
                : string.Empty;
        }

        private void UpdatePagination()
        {
            var totalRecordCount = Achievement.GetActiveAchievements().Count;
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / EntriesPerPage + (totalRecordCount % EntriesPerPage == 0 ? 0 : 1));

            // Always add page 1.
            pageNumbers.Add(new GuiComboEntry($"Page 1", 0));
            for (var x = 2; x <= pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x - 1));
            }

            PageNumbers = pageNumbers;

            // In the event no results are found, default the index to zero
            if (pages <= 0)
                SelectedPageIndex = 0;

            // Otherwise, if current page is outside the new page bounds,
            // set it to the last page in the list.
            else if (SelectedPageIndex > pages - 1)
                SelectedPageIndex = pages - 1;
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

        public Action OnClickPreviousPage() => () =>
        {
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndex = newPage;
        };

        public Action OnClickNextPage() => () =>
        {
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            SelectedPageIndex = newPage;
        };

        public void Refresh(AchievementUnlockedRefreshEvent payload)
        {
            var achievement = payload.Type;
            if (!_types.Contains(achievement))
                return;

            var index = _types.IndexOf(achievement);
            if (SelectedIndex == index)
            {
                LoadAchievement();
            }

            Colors[index] = GuiColor.Green;
        }
    }
}
