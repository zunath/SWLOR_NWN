﻿using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class DistributeSkillRanks: ConversationBase
    {
        // This determines the maximum level a player can reach via this distribution of skill ranks.
        private const int MaxRankForDistribution = 100;

        private class Model
        {
            public SkillCategory SkillCategoryID { get; set; }
            public Skill SkillID { get; set; }
            public int DistributionType { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "You have undistributed skill ranks. You are allowed to distribute these to skills within a single category up to rank " + MaxRankForDistribution + ". You MUST distribute all of these before you can acquire XP in any of the skills within this category.");

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
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            
            foreach (var pool in player.SkillPools)
            {
                var category = pool.Key;
                var attr = category.GetAttribute<SkillCategory, SkillCategoryAttribute>();
                AddResponseToPage("MainPage", attr.Name, true, category);
            }
        }

        private void MainResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            var categoryID = (SkillCategory)response.CustomData;
            var model = GetDialogCustomData<Model>();
            model.SkillCategoryID = categoryID;

            LoadSkillListPage();
            ChangePage("SkillListPage");
        }

        private void LoadSkillListPage()
        {
            var model = GetDialogCustomData<Model>();
            var category = model.SkillCategoryID;
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var pool = player.SkillPools[model.SkillCategoryID];
            var skillTypes = SkillService.GetAllSkillsInCategory(model.SkillCategoryID);
            var categoryAttr = category.GetAttribute<SkillCategory, SkillCategoryAttribute>();

            string header = ColorTokenService.Green("Category: ") + categoryAttr.Name + "\n";
            header += ColorTokenService.Green("Ranks to Distribute: ") + pool + "\n\n";
            header += "You may distribute ranks to any of the following skills. Note that you may only increase a rank to a maximum level of 40. You will not gain any new experience towards any of the following skills until *ALL* ranks have been distributed.";

            SetPageHeader("SkillListPage", header);

            ClearPageResponses("SkillListPage");
            foreach (var skillType in skillTypes)
            {
                var skill = SkillService.GetSkill(skillType);
                AddResponseToPage("SkillListPage", skill.Name, true, skillType);
            }

        }

        private void SkillListResponses(int responseID)
        {
            var response = GetResponseByID("SkillListPage", responseID);
            var model = GetDialogCustomData<Model>();
            model.SkillID = (Skill) response.CustomData;

            LoadSkillPage();
            ChangePage("SkillPage");
        }

        private bool CanDistribute(int amount)
        {
            var model = GetDialogCustomData<Model>();
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var skill = SkillService.GetSkill(model.SkillID);
            var pcSkill = player.Skills[model.SkillID];
            var pool = player.SkillPools[model.SkillCategoryID];

            return pool >= amount && pcSkill.Rank + amount < MaxRankForDistribution && pcSkill.Rank + amount <= skill.MaxRank;
        }

        private void LoadSkillPage()
        {
            var model = GetDialogCustomData<Model>();
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var skill = SkillService.GetSkill(model.SkillID);
            var pcSkill = player.Skills[model.SkillID];
            var pool = player.SkillPools[model.SkillCategoryID];
            
            // Build the page header
            var header = ColorTokenService.Green("Skill: ") + skill.Name + "\n";
            header += ColorTokenService.Green("Current Rank: ") + pcSkill.Rank + "\n";
            header += ColorTokenService.Green("Ranks to Distribute: ") + pool + "\n\n";

            if (pcSkill.Rank >= MaxRankForDistribution || pcSkill.Rank >= skill.MaxRank)
            {
                header += ColorTokenService.Red("You cannot distribute any more ranks into this skill.");
            }
            else
            {
                header += ColorTokenService.Green("You may distribute a skill rank into this skill.");
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
                var player = DataService.Player.GetByID(GetPC().GlobalID);
                var pcSkill = player.Skills[model.SkillID];
                var skill = SkillService.GetSkill(model.SkillID);

                // Prevent the player from adding too many ranks.
                if (pcSkill.Rank + amount > skill.MaxRank)
                {
                    GetPC().FloatingText("You cannot distribute this number of ranks into this skill.");
                    return;
                }

                pcSkill.Rank += amount;
                
                DataService.Set(player);
                PlayerStatService.ApplyStatChanges(GetPC(), null);

                // Reduce the pool levels. Delete the record if it drops to zero.
                var pool = player.SkillPools[model.SkillCategoryID] -= amount;

                DataService.Set(player);
                if (pool <= 0)
                {
                    EndConversation();
                    return;
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
