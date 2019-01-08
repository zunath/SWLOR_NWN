using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class DistributeSkillRanks: ConversationBase
    {
        // This determines the maximum level a player can reach via this distribution of skill ranks.
        private const int MaxRankForDistribution = 40;

        private class Model
        {
            public int SkillCategoryID { get; set; }
            public int SkillID { get; set; }
            public int DistributionType { get; set; }
        }

        private readonly IDataService _data;
        private readonly IColorTokenService _color;
        private readonly IPlayerStatService _playerStat;

        public DistributeSkillRanks(
            INWScript script, 
            IDialogService dialog,
            IDataService data,
            IColorTokenService color,
            IPlayerStatService playerStat) 
            : base(script, dialog)
        {
            _data = data;
            _color = color;
            _playerStat = playerStat;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "You have undistributed skill ranks. You are allowed to distribute these to skills within a single category up to rank 40. You MUST distribute all of these before you can acquire XP in any of the skills within this category.");

            DialogPage skillListPage = new DialogPage();
            DialogPage skillPage = new DialogPage(
                "<SET LATER>",
                "Distribute 1 Rank",
                "Distribute 5 Ranks",
                "Distribute 10 Ranks");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("SkillListPage", skillListPage);
            dialog.AddPage("SkillPage", skillPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            ClearPageResponses("MainPage");
            var pools = _data.Where<PCSkillPool>(x => x.PlayerID == GetPC().GlobalID && x.Levels > 0);

            foreach (var pool in pools)
            {
                var category = _data.Get<SkillCategory>(pool.SkillCategoryID);
                AddResponseToPage("MainPage", category.Name, true, category.ID);
            }
        }

        private void MainResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            var categoryID = (int)response.CustomData;
            var model = GetDialogCustomData<Model>();
            model.SkillCategoryID = categoryID;

            LoadSkillListPage();
            ChangePage("SkillListPage");
        }

        private void LoadSkillListPage()
        {
            var model = GetDialogCustomData<Model>();
            var category = _data.Get<SkillCategory>(model.SkillCategoryID);
            var pool = _data.Single<PCSkillPool>(x => x.PlayerID == GetPC().GlobalID && x.SkillCategoryID == model.SkillCategoryID);
            var skills = _data.Where<Skill>(x => x.SkillCategoryID == model.SkillCategoryID);

            string header = _color.Green("Category: ") + category.Name + "\n";
            header += _color.Green("Ranks to Distribute: ") + pool.Levels + "\n\n";
            header += "You may distribute ranks to any of the following skills. Note that you may only increase a rank to a maximum level of 40. You will not gain any new experience towards any of the following skills until *ALL* ranks have been distributed.";

            SetPageHeader("SkillListPage", header);

            ClearPageResponses("SkillListPage");
            foreach (var skill in skills)
            {
                AddResponseToPage("SkillListPage", skill.Name, true, skill.ID);
            }

        }

        private void SkillListResponses(int responseID)
        {
            var response = GetResponseByID("SkillListPage", responseID);
            var model = GetDialogCustomData<Model>();
            model.SkillID = (int) response.CustomData;

            LoadSkillPage();
            ChangePage("SkillPage");
        }

        private bool CanDistribute(int amount)
        {
            var model = GetDialogCustomData<Model>();
            var pcSkill = _data.Single<PCSkill>(x => x.PlayerID == GetPC().GlobalID && x.SkillID == model.SkillID);
            var pool = _data.Single<PCSkillPool>(x => x.PlayerID == GetPC().GlobalID && x.SkillCategoryID == model.SkillCategoryID);

            return pool.Levels >= amount && pcSkill.Rank + amount <= MaxRankForDistribution;
        }

        private void LoadSkillPage()
        {
            var model = GetDialogCustomData<Model>();
            var skill = _data.Get<Skill>(model.SkillID);
            var pcSkill = _data.Single<PCSkill>(x => x.PlayerID == GetPC().GlobalID && x.SkillID == model.SkillID);
            var pool = _data.Single<PCSkillPool>(x => x.PlayerID == GetPC().GlobalID && x.SkillCategoryID == model.SkillCategoryID);
            
            // Build the page header
            var header = _color.Green("Skill: ") + skill.Name + "\n";
            header += _color.Green("Current Rank: ") + pcSkill.Rank + "\n";
            header += _color.Green("Ranks to Distribute: ") + pool.Levels + "\n\n";

            if (pcSkill.Rank >= MaxRankForDistribution)
            {
                header += _color.Red("You cannot distribute any more ranks into this skill.");
            }
            else
            {
                header += _color.Green("You may distribute a skill rank into this skill.");
            }

            SetPageHeader("SkillPage", header);

            // Reset the option text.
            SetResponseText("SkillPage", 1, "Distribute 1 Rank");
            SetResponseText("SkillPage", 2, "Distribute 5 Ranks");
            SetResponseText("SkillPage", 3, "Distribute 10 Ranks");

            // Show/hide options based on whether distribution is allowed.
            SetResponseVisible("SkillPage", 1, CanDistribute(1));
            SetResponseVisible("SkillPage", 2, CanDistribute(5));
            SetResponseVisible("SkillPage", 3, CanDistribute(10));

            // If confirming, capitalize the message to alert the player.
            if (model.DistributionType == 1)
            {
                SetResponseText("SkillPage", 1, "CONFIRM DISTRIBUTE 1 RANK");
            }
            else if (model.DistributionType == 2)
            {
                SetResponseText("SkillPage", 2, "CONFIRM DISTRIBUTE 5 RANKS");
            }
            else if (model.DistributionType == 3)
            {
                SetResponseText("SkillPage", 3, "CONFIRM DISTRIBUTE 10 RANKS");
            }

        }

        private void SkillResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();

            // This is the first click. Mark the distribution type and wait for confirmation on the next click.
            if (model.DistributionType != responseID)
            {
                model.DistributionType = responseID;
            }
            // This is confirmation to distribute the rank(s). Do it.
            else
            {
                int amount;

                // Figure out how much to increase the skill by.
                if (responseID == 1) amount = 1;
                else if (responseID == 2) amount = 5;
                else if (responseID == 3) amount = 10;
                else
                {
                    // We shouldn't ever see this message, but just in case a future change breaks it...
                    GetPC().FloatingText("You cannot distribute this number of ranks into this skill.");
                    return;
                }

                // Ensure the player can distribute the ranks one more time.
                if (!CanDistribute(amount))
                {
                    GetPC().FloatingText("You cannot distribute these ranks into this skill.");
                    return;
                }

                // Let's do the distribution. Normally, you would want to run the SkillService methods but in this scenario
                // all of that's already been applied. You don't want to reapply the SP gains because they'll get more than they should.
                // Just set the ranks on the DB record and recalc stats.
                var pcSkill = _data.Single<PCSkill>(x => x.PlayerID == GetPC().GlobalID && x.SkillID == model.SkillID);
                pcSkill.Rank += amount;
                _data.SubmitDataChange(pcSkill, DatabaseActionType.Update);
                _playerStat.ApplyStatChanges(GetPC(), null);

                // Reduce the pool levels. Delete the record if it drops to zero.
                var pool = _data.Single<PCSkillPool>(x => x.PlayerID == GetPC().GlobalID && x.SkillCategoryID == model.SkillCategoryID);
                pool.Levels -= amount;

                if (pool.Levels <= 0)
                {
                    _data.SubmitDataChange(pool, DatabaseActionType.Delete);
                    EndConversation();
                    return;
                }
                else
                {
                    _data.SubmitDataChange(pool, DatabaseActionType.Update);
                }

                model.DistributionType = 0;
            }

            LoadSkillPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "SkillListPage":
                    SkillListResponses(responseID);
                    break;
                case "SkillPage":
                    SkillResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            if (beforeMovePage == "SkillPage")
            {
                var model = GetDialogCustomData<Model>();
                model.DistributionType = 0;
            }
        }

        public override void EndDialog()
        {
        }
    }
}
