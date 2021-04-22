using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using Dialog = SWLOR.Game.Server.Service.Dialog;
using Player = SWLOR.Game.Server.Entity.Player;
using Skill = SWLOR.Game.Server.Service.Skill;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class RestMenuDialog : DialogBase
    {
        /// <summary>
        /// When a player uses the "Open Rest Menu" feat, open the rest menu dialog conversation.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void UseOpenRestMenuFeat()
        {
            var feat = (FeatType)Convert.ToInt32(Events.GetEventData("FEAT_ID"));
            if (feat != FeatType.OpenRestMenu) return;

            Dialog.StartConversation(OBJECT_SELF, OBJECT_SELF, nameof(RestMenuDialog));
        }

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
            header += ColorToken.Green("Unallocated XP: ") + dbPlayer.UnallocatedXP + "\n";

            page.Header = header;

            page.AddResponse("View Skills", () => SwitchConversation(nameof(ViewSkillsDialog)));
            page.AddResponse("View Perks", () => SwitchConversation(nameof(ViewPerksDialog)));
            page.AddResponse("View Achievements", () => SwitchConversation(nameof(ViewAchievementsDialog)));
            page.AddResponse("View Recipes", () =>
            {
                var craftingState = Craft.GetPlayerCraftingState(player);
                craftingState.DeviceSkillType = SkillType.Invalid;
                SwitchConversation(nameof(RecipeDialog));
            });
            page.AddResponse("View Key Items", () => SwitchConversation(nameof(ViewKeyItemsDialog)));
            page.AddResponse("Modify Item Appearance", () => SwitchConversation(nameof(ModifyItemAppearanceDialog)));
            page.AddResponse("Player Settings", () => SwitchConversation(nameof(PlayerSettingsDialog)));
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
