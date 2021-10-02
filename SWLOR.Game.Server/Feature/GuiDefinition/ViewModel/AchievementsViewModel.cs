using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AchievementsViewModel: GuiViewModelBase<AchievementsViewModel>
    {
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

        public GuiBindingList<string> Descriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> Colors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> AcquiredDates
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public AchievementsViewModel()
        {
            Names = new GuiBindingList<string>();
            Descriptions = new GuiBindingList<string>();
            Colors = new GuiBindingList<GuiColor>();
        }

        public Action OnLoadWindow() => () =>
        {
            ShowAll = true;
            LoadAchievements();
            WatchOnClient(model => model.ShowAll);
        };

        private void LoadAchievements()
        {
            var cdKey = GetPCPublicCDKey(Player);
            var dbAccount = DB.Get<Account>(cdKey);

            var names = new GuiBindingList<string>();
            var descriptions = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var acquiredDates = new GuiBindingList<string>();

            if (ShowAll)
            {
                foreach (var (type, detail) in Achievement.GetActiveAchievements())
                {
                    names.Add(detail.Name);
                    descriptions.Add(detail.Description);

                    if (dbAccount.Achievements.ContainsKey(type))
                    {
                        var date = dbAccount.Achievements[type];
                        colors.Add(new GuiColor(0, 255, 0));
                        acquiredDates.Add(date.ToString("g"));
                    }
                    else
                    {
                        colors.Add(new GuiColor(255, 0, 0));
                        acquiredDates.Add("LOCKED");
                    }
                }
            }
            else
            {
                foreach (var (type, date) in dbAccount.Achievements)
                {
                    var detail = Achievement.GetAchievement(type);

                    names.Add(detail.Name);
                    descriptions.Add(detail.Description);
                    colors.Add(new GuiColor(0, 255, 0));
                    acquiredDates.Add(date.ToString("g"));
                }
            }

            Names = names;
            Descriptions = descriptions;
            Colors = colors;
            AcquiredDates = acquiredDates;
        }
    }
}
