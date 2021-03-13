using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ViewSkillsDialog: DialogBase
    {
        private class Model
        {
            public SkillCategoryType SelectedCategory { get; set; }
            public SkillType SelectedSkill { get; set; }
            public int XPDistributing { get; set; }
            public bool IsConfirming { get; set; }
        }

        private const string CategorySelectionId = "CATEGORY_SELECTION";
        private const string SkillSelectionId = "SKILL_SELECTION";
        private const string SkillDetailsId = "SKILL_DETAILS";
        private const string DistributeXPId = "DISTRIBUTE_XP";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddBackAction((oldPage, newPage) =>
                {
                    var model = GetDataModel<Model>();
                    model.IsConfirming = false;
                    model.XPDistributing = 0;
                })
                .AddPage(CategorySelectionId, SkillCategorySelectionInit)
                .AddPage(SkillSelectionId, SkillSelectionInit)
                .AddPage(SkillDetailsId, SkillDetailsInit)
                .AddPage(DistributeXPId, DistributeXPAmountInit);

            return builder.Build();
        }

        /// <summary>
        /// Builds the Skill Category Selection page.
        /// </summary>
        /// <param name="page">The page to build.</param>
        private void SkillCategorySelectionInit(DialogPage page)
        {
            void SelectCategory(SkillCategoryType category)
            {
                var model = GetDataModel<Model>();
                model.SelectedCategory = category;
                ChangePage(SkillSelectionId);
            }

            page.Header = "Please select a skill category.";

            var categories = Skill.GetAllActiveSkillCategories();
            foreach (var (type, details) in categories)
            {
                page.AddResponse(details.Name, () =>
                {
                    SelectCategory(type);
                });
            }
        }

        /// <summary>
        /// Builds the Skill Selection page.
        /// </summary>
        /// <param name="page">The page to build.</param>
        private void SkillSelectionInit(DialogPage page)
        {
            var model = GetDataModel<Model>();

            void SelectSkill(SkillType skill)
            {
                model.SelectedSkill = skill;
                ChangePage(SkillDetailsId);
            }

            var categoryDetails = Skill.GetSkillCategoryDetails(model.SelectedCategory);
            page.Header = $"{ColorToken.Green("Selected Category: ")}{categoryDetails.Name}\n\n Please select a skill.";

            var skills = Skill.GetActiveSkillsByCategory(model.SelectedCategory);
            foreach (var (type, details) in skills)
            {
                page.AddResponse(details.Name, () =>
                {
                    SelectSkill(type);
                });
            }
        }

        /// <summary>
        /// Builds the Skill Details page.
        /// </summary>
        /// <param name="page">The page to build.</param>
        private void SkillDetailsInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);

            void BuildHeader()
            {
                var dbPlayer = DB.Get<Player>(playerId);
                var skill = Skill.GetSkillDetails(model.SelectedSkill);
                var pcSkill = dbPlayer.Skills[model.SelectedSkill];
                var requiredXP = Skill.GetRequiredXP(pcSkill.Rank);

                string title;
                if (pcSkill.Rank <= 3) title = "Untrained";
                else if (pcSkill.Rank <= 7) title = "Neophyte";
                else if (pcSkill.Rank <= 13) title = "Novice";
                else if (pcSkill.Rank <= 20) title = "Apprentice";
                else if (pcSkill.Rank <= 35) title = "Journeyman";
                else if (pcSkill.Rank <= 50) title = "Expert";
                else if (pcSkill.Rank <= 65) title = "Adept";
                else if (pcSkill.Rank <= 80) title = "Master";
                else if (pcSkill.Rank <= 100) title = "Grandmaster";
                else title = "Unknown";

                title += " (" + pcSkill.Rank + ")";

                string decayLock = ColorToken.Green("Decay Lock: ") + ColorToken.White("Unlocked");
                if (pcSkill.IsLocked)
                {
                    decayLock = ColorToken.Green("Decay Lock: ") + ColorToken.Red("Locked");
                }

                // Skills which don't contribute to the cap cannot be locked (there's no reason for it.)
                // Display a message explaining this to the player instead.
                string noContributeMessage = string.Empty;
                if (!skill.ContributesToSkillCap)
                {
                    decayLock = string.Empty;
                    noContributeMessage = ColorToken.Green("This skill does not contribute to your cumulative skill cap.") + "\n\n";
                }

                string unallocatedXP = ColorToken.Green("Unallocated XP: ") + dbPlayer.UnallocatedXP + "\n";

                page.Header =
                        ColorToken.Green("Skill: ") + skill.Name + "\n" +
                        ColorToken.Green("Rank: ") + title + "\n" +
                        ColorToken.Green("Exp: ") + Menu.BuildBar(pcSkill.XP, requiredXP, 100, ColorToken.TokenStart(255, 127, 0)) + "\n" +
                        unallocatedXP +
                        noContributeMessage +
                        decayLock + "\n\n" +
                        ColorToken.Green("Description: ") + skill.Description + "\n";
            }

            void DistributeXP()
            {
                ChangePage(DistributeXPId);
            }

            void ToggleDecayLock()
            {
                var dbPlayer = DB.Get<Player>(playerId);
                dbPlayer.Skills[model.SelectedSkill].IsLocked = !dbPlayer.Skills[model.SelectedSkill].IsLocked;
                DB.Set(playerId, dbPlayer);
            }

            BuildHeader();

            page.AddResponse("Distribute XP", DistributeXP);
            page.AddResponse("Toggle Decay Lock", ToggleDecayLock);
        }

        /// <summary>
        /// Builds the Distribute XP Amount page.
        /// </summary>
        /// <param name="page">The page to build.</param>
        private void DistributeXPAmountInit(DialogPage page)
        {
            var model = GetDataModel<Model>();

            string BuildHeader()
            {
                var player = GetPC();
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                var skill = Skill.GetSkillDetails(model.SelectedSkill);

                string header = ColorToken.Green("Unallocated XP Distribution") + "\n\n";
                header += ColorToken.Green("Skill: ") + skill.Name + "\n";
                header += ColorToken.Green("Available XP: ") + dbPlayer.UnallocatedXP + "\n";
                header += ColorToken.Green("Currently Distributing: ") + model.XPDistributing + " XP\n";

                return header;
            }

            void ModifyAmount(int amount)
            {
                var player = GetPC();
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (amount == 0)
                    amount = dbPlayer.UnallocatedXP;

                model.XPDistributing += amount;

                if (model.XPDistributing > dbPlayer.UnallocatedXP)
                    model.XPDistributing = dbPlayer.UnallocatedXP;
                else if (model.XPDistributing < 0)
                    model.XPDistributing = 0;
            }

            void DistributeXP()
            {
                var player = GetPC();
                if (model.XPDistributing <= 0)
                {
                    SendMessageToPC(player, "Please specify how much XP you'd like to distribute into this skill.");
                    model.IsConfirming = false;
                }
                else if (model.IsConfirming)
                {
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    dbPlayer.UnallocatedXP -= model.XPDistributing;
                    DB.Set(playerId, dbPlayer);

                    Skill.GiveSkillXP(player, model.SelectedSkill, model.XPDistributing);
                    model.IsConfirming = false;
                    model.XPDistributing = 0;
                }
                else
                {
                    model.IsConfirming = true;
                }
            }

            var confirmText = model.IsConfirming ? $"CONFIRM DISTRIBUTE XP ({model.XPDistributing})" : $"Distribute XP ({model.XPDistributing})";
            page.Header = BuildHeader();

            page.AddResponse("Select All XP", () => ModifyAmount(0));
            page.AddResponse("Increase by 1000", () => ModifyAmount(1000));
            page.AddResponse("Increase by 100", () => ModifyAmount(100));
            page.AddResponse("Increase by 10", () => ModifyAmount(10));
            page.AddResponse("Increase by 1", () => ModifyAmount(1));
            page.AddResponse("Decrease by 1000", () => ModifyAmount(-1000));
            page.AddResponse("Decrease by 100", () => ModifyAmount(-100));
            page.AddResponse("Decrease by 10", () => ModifyAmount(-10));
            page.AddResponse("Decrease by 1", () => ModifyAmount(-1));
            page.AddResponse(confirmText, DistributeXP);
        }

    }
}
