﻿using System;
using System.Diagnostics;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Conversation
{
    internal class RestMenu : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                BuildMainPageHeader(player),
                ColorTokenService.Green("Open Overflow Inventory"),
                "View Skills",
                "View Perks",
                "View Blueprints",
                "View Key Items",
                "Modify Item Appearance",
                "Character Management",
                "Open Trash Can (Destroy Items)",
                "HoloCom");

            dialog.AddPage("MainPage", mainPage);

            return dialog;
        }

        public override void Initialize()
        {
            Guid playerID = GetPC().GlobalID;
            var dbPlayer = DataService.Player.GetByID(playerID);
            long overflowCount = dbPlayer.OverflowItems.Count;

            if (overflowCount <= 0)
            {
                SetResponseVisible("MainPage", 1, false);
            }

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    switch (responseID)
                    {
                        // Open Overflow Inventory
                        case 1:
                            NWObject storage = (_.CreateObject(ObjectType.Placeable, "overflow_storage", player.Location));
                            player.AssignCommand(() => _.ActionInteractObject(storage.Object));
                            break;
                        // View Skills
                        case 2:
                            SwitchConversation("ViewSkills");
                            break;
                        // View Perks
                        case 3:
                            SwitchConversation("ViewPerks");
                            break;
                        // View Blueprints
                        case 4:
                            SwitchConversation("ViewBlueprints");
                            break;
                        // Key Item Categories Page
                        case 5:
                            SwitchConversation("KeyItems");
                            break;
                        // Modify Item Appearance
                        case 6:
                            SwitchConversation("ModifyItemAppearance");
                            break;
                        // Character Management
                        case 7:
                            SwitchConversation("CharacterManagement");
                            break;
                        // Open Trash Can (Destroy Items)
                        case 8:
                            EndConversation();
                            NWPlaceable trashCan = (_.CreateObject(ObjectType.Placeable, "reo_trash_can", player.Location));

                            player.AssignCommand(() => _.ActionInteractObject(trashCan.Object));
                            _.DelayCommand(0.2f, () => trashCan.IsUseable = false);
                            break;
                        // HoloCom
                        case 9:
                            SwitchConversation("HoloCom");
                            break;
                    }
                    break;

            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }

        private string BuildMainPageHeader(NWPlayer player)
        {
            Player playerEntity = DataService.Player.GetByID(player.GlobalID);

            // Get all player skills and then sum them up by the rank.
            int totalSkillCount = playerEntity.Skills
                .Where(x => SkillService.GetSkill(x.Key).ContributesToSkillCap)
                .Sum(s => s.Value.Rank);

            string header = ColorTokenService.Green("Name: ") + player.Name + "\n";
            header += ColorTokenService.Green("Association: ") + playerEntity.AssociationID.GetDescriptionAttribute() + "\n\n";
            header += ColorTokenService.Green("Skill Points: ") + totalSkillCount + " / " + SkillService.SkillCap + "\n";
            header += ColorTokenService.Green("Unallocated SP: ") + playerEntity.UnallocatedSP + "\n";
            header += ColorTokenService.Green("Roleplay XP: ") + playerEntity.RoleplayXP + "\n";
            header += ColorTokenService.Green("FP: ")  + (playerEntity.MaxFP > 0 ? MenuService.BuildBar(playerEntity.CurrentFP, playerEntity.MaxFP, 100, ColorTokenService.TokenStart(32, 223, 219)) : "N/A") + "\n";

            return header;
        }


    }
}
