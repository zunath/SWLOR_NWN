using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.SkillService;
using Player = SWLOR.Game.Server.Entity.Player;
using Skill = SWLOR.Game.Server.Service.Skill;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class RestMenuDialog : DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        /// <summary>
        /// Builds the Main Page.
        /// </summary>
        /// <param name="page">The page to build.</param>
        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Get all player skills and then sum them up by the rank.
            var totalSkillCount = dbPlayer.TotalSPAcquired;

            var playerName = GetName(player);
            var header = ColorToken.Green("Name: ") + playerName + "\n";
            header += ColorToken.Green("Skill Points: ") + totalSkillCount + " / " + Skill.SkillCap + "\n";
            header += ColorToken.Green("Unallocated SP: ") + dbPlayer.UnallocatedSP + "\n";
            header += ColorToken.Green("Unallocated AP: ") + dbPlayer.UnallocatedAP + "\n";
            header += ColorToken.Green("Unallocated XP: ") + dbPlayer.UnallocatedXP + "\n";

            page.Header = header;

            if (dbPlayer.UnallocatedAP > 0)
            {
                page.AddResponse("Distribute Attribute Points", () => SwitchConversation(nameof(DistributeAbilityPointsDialog)));
            }

            page.AddResponse("View Recipes", () =>
            {
                var craftingState = Craft.GetPlayerCraftingState(player);
                craftingState.DeviceSkillType = SkillType.Invalid;
                SwitchConversation(nameof(RecipeDialog));
            });
            page.AddResponse("Modify Item Appearance", () => SwitchConversation(nameof(ModifyItemAppearanceDialog)));
            page.AddResponse("Open Trash Can (Destroy Items)", () =>
            {
                EndConversation();
                var location = GetLocation(player);
                var trashCan = CreateObject(ObjectType.Placeable, "reo_trash_can", location);

                AssignCommand(player, () => ActionInteractObject(trashCan));
                DelayCommand(0.2f, () => SetUseableFlag(trashCan, false));
            });
        }
    }
}
