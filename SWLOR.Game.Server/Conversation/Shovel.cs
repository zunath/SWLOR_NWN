using System;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class Shovel: ConversationBase
    {
        private readonly IPlayerService _player;
        private readonly ISkillService _skill;
        private readonly IFarmingService _farming;

        public Shovel(
            INWScript script, 
            IDialogService dialog,
            IPlayerService player,
            ISkillService skill,
            IFarmingService farming) 
            : base(script, dialog)
        {
            _player = player;
            _skill = skill;
            _farming = farming;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "Dig a hole",
                "Retrieve Seed"
            );

            DialogPage harvestPage = new DialogPage(
                "Are you sure you want to harvest this plant? Harvesting will destroy the plant and recover a seed.",
                "Yes, harvest it.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("HarvestPage", harvestPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWItem shovel = (GetPC().GetLocalObject("SHOVEL_ITEM"));
            string header = "This shovel has " + shovel.Charges + " uses remaining. What would you like to do?";
            SetPageHeader("MainPage", header);

            if (!CanHarvest())
            {
                SetResponseVisible("MainPage", 2, false);
            }
        }

        private bool CanHarvest()
        {
            NWObject target = (GetPC().GetLocalObject("SHOVEL_TARGET_OBJECT"));

            return target.IsValid &&
                   target.GetLocalInt("GROWING_PLANT_ID") > 0 &&
                   _.GetDistanceBetween(GetPC().Object, target.Object) <= 2.0f;
        }


        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponse(responseID);
                    break;
                case "HarvestPage":
                    HandleHarvestPageResponse(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Dig a Hole
                    DigAHole();
                    break;
                case 2: // Harvest Plant
                    ChangePage("HarvestPage");
                    break;
            }
        }

        private void DigAHole()
        {
            Location targetLocation = GetPC().GetLocalLocation("SHOVEL_TARGET_LOCATION");

            _.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "farm_small_hole", targetLocation);
            _.FloatingTextStringOnCreature("You dig a hole.", GetPC().Object, NWScript.FALSE);
            _skill.GiveSkillXP(GetPC(), SkillType.Farming, 50);
            EndConversation();
        }

        private void HandleHarvestPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Harvest Seed
                    HarvestSeed();
                    break;
            }
        }

        private void HarvestSeed()
        {
            if (!CanHarvest())
            {
                _.SendMessageToPC(GetPC().Object, "You cannot harvest that plant from here. Move closer and try again.");
                return;
            }

            NWItem shovel = (GetPC().GetLocalObject("SHOVEL_ITEM"));
            NWObject plant = (GetPC().GetLocalObject("SHOVEL_TARGET_OBJECT"));
            _farming.HarvestPlant(GetPC(), shovel, (NWPlaceable)plant);
            EndConversation();
        }

        public override void EndDialog()
        {
            GetPC().DeleteLocalObject("SHOVEL_ITEM");
            GetPC().DeleteLocalLocation("SHOVEL_TARGET_LOCATION");
        }
    }
}
