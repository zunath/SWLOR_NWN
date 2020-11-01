using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ViewAchievementsDialog: DialogBase
    {
        private class Model
        {
            public AchievementType Type { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string AchievementDetailId = "ACHIEVEMENT_DETAIL_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(AchievementDetailId, AchievementDetailInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var cdKey = GetPCPublicCDKey(player);
            var account = DB.Get<Account>(cdKey) ?? new Account();
            var achievements = account.Achievements;
            var activeAchievements = Achievement.GetActiveAchievements();
            var model = GetDataModel<Model>();

            foreach (var achievement in activeAchievements)
            {
                var text = achievements.ContainsKey(achievement.Key) ?
                    ColorToken.Green(achievement.Value.Name) :
                    ColorToken.Red(achievement.Value.Name);
                page.AddResponse(text, () =>
                {
                    model.Type = achievement.Key;
                    ChangePage(AchievementDetailId);
                });
            }
        }

        private void AchievementDetailInit(DialogPage page)
        {
            var player = GetPC();
            var cdKey = GetPCPublicCDKey(player);
            var account = DB.Get<Account>(cdKey) ?? new Account();
            var activeAchievements = Achievement.GetActiveAchievements();
            var model = GetDataModel<Model>();
            var achievement = activeAchievements[model.Type];

            page.Header = ColorToken.Green("Name: ") + achievement.Name + "\n" +
                ColorToken.Green("Description: ") + achievement.Description + "\n\n";

            if (account.Achievements.ContainsKey(model.Type))
            {
                var pcAchievement = account.Achievements[model.Type];
                page.Header += ColorToken.Green("Unlocked: ") + pcAchievement.ToString("yyyy-MM-dd hh:mm:ss");
            }
            else
            {
                page.Header += ColorToken.Red("Not yet acquired.");
            }
        }
    }
}
