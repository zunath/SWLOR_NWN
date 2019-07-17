using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class CraftItem : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            ToggleBackButton(false);

            var model = CraftService.GetPlayerCraftingData(GetPC());
            var device = GetDevice();

            // Entering the conversation for the first time from the blueprint selection menu.
            if (!model.IsInitialized)
            {
                model.IsInitialized = true;
                model.Blueprint = CraftService.GetBlueprintByID(model.BlueprintID);
                model.PlayerSkillRank = SkillService.GetPCSkillRank(GetPC(), model.Blueprint.SkillID);

                switch ((SkillType)model.Blueprint.SkillID)
                {
                    case SkillType.Armorsmith:
                        model.PlayerPerkLevel = PerkService.GetCreaturePerkLevel(GetPC(), PerkType.ArmorBlueprints);
                        break;
                    case SkillType.Engineering:
                        model.PlayerPerkLevel = PerkService.GetCreaturePerkLevel(GetPC(), PerkType.EngineeringBlueprints);
                        break;
                    case SkillType.Weaponsmith:
                        model.PlayerPerkLevel = PerkService.GetCreaturePerkLevel(GetPC(), PerkType.WeaponBlueprints);
                        break;
                    case SkillType.Fabrication:
                        model.PlayerPerkLevel = PerkService.GetCreaturePerkLevel(GetPC(), PerkType.FabricationBlueprints);
                        break;
                    case SkillType.Medicine:
                        model.PlayerPerkLevel = PerkService.GetCreaturePerkLevel(GetPC(), PerkType.MedicalBlueprints);
                        break;
                    case SkillType.Lightsaber:
                        model.PlayerPerkLevel = PerkService.GetCreaturePerkLevel(GetPC(), PerkType.LightsaberBlueprints);
						// Lightsabers do not have Optimisation or Efficiency perks. 
                        break;
                    default:
                        model.PlayerPerkLevel = 0;
                        break;

                }
                GetDevice().IsLocked = true;
                model.MainMinimum = model.Blueprint.MainMinimum ;
                model.SecondaryMinimum = model.Blueprint.SecondaryMinimum;
                model.TertiaryMinimum = model.Blueprint.TertiaryMinimum;

                model.MainMaximum = model.Blueprint.MainMaximum;
                model.SecondaryMaximum = model.Blueprint.SecondaryMaximum > 0 ? model.Blueprint.SecondaryMaximum : 0;
                model.TertiaryMaximum = model.Blueprint.TertiaryMaximum > 0 ? model.Blueprint.TertiaryMaximum : 0;

                if (model.MainMinimum <= 0)
                    model.MainMinimum = 1;
                if (model.SecondaryMinimum <= 0 && model.Blueprint.SecondaryMinimum > 0)
                    model.SecondaryMinimum = 1;
                if (model.TertiaryMinimum <= 0 && model.Blueprint.TertiaryMinimum > 0)
                    model.TertiaryMinimum = 1;

            }
            // Otherwise returning from accessing the device's inventory.
            else
            {
                model.Access = CraftingAccessType.None;

                _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_USED, "script_1");
                _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
                _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
                _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
            }


            SetPageHeader("MainPage", CraftService.BuildBlueprintHeader(GetPC(), true));
            BuildMainPageOptions();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponses(responseID);
                    break;
            }

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
            var model = CraftService.GetPlayerCraftingData(GetPC());
            if (model.IsAccessingStorage) return;

            CraftService.ClearPlayerCraftingData(GetPC());
        }

        private NWPlaceable GetDevice()
        {
            return (GetDialogTarget().Object);
        }


        private void BuildMainPageOptions()
        {
            var model = CraftService.GetPlayerCraftingData(GetPC());
            int maxEnhancements = model.PlayerPerkLevel / 2;
            bool canAddEnhancements = model.Blueprint.EnhancementSlots > 0 && maxEnhancements > 0;

            AddResponseToPage("MainPage", "Examine Base Item");
            AddResponseToPage("MainPage", "Create Item", model.CanBuildItem);
            AddResponseToPage("MainPage", "Select Main Components");
            AddResponseToPage("MainPage", "Select Secondary Components", model.Blueprint.SecondaryMinimum > 0);
            AddResponseToPage("MainPage", "Select Tertiary Components", model.Blueprint.TertiaryMinimum > 0);
            AddResponseToPage("MainPage", "Select Enhancement Components", canAddEnhancements);

            AddResponseToPage("MainPage", "Change Blueprint");
        }

        private void HandleMainPageResponses(int responseID)
        {
            var model = CraftService.GetPlayerCraftingData(GetPC());
            NWPlaceable device = GetDevice();

            switch (responseID)
            {
                case 1: // Examine Base Item
                    CraftBlueprint entity = CraftService.GetBlueprintByID(model.BlueprintID);
                    NWPlaceable tempContainer = (_.GetObjectByTag("craft_temp_store"));
                    NWItem examineItem = (_.CreateItemOnObject(entity.ItemResref, tempContainer.Object));
                    GetPC().AssignCommand(() => _.ActionExamine(examineItem.Object));
                    examineItem.Destroy(0.1f);
                    break;
                case 2: // Create item
                    if (!model.CanBuildItem)
                    {
                        GetPC().FloatingText("You are missing some required components.");
                        return;
                    }

                    int effectiveLevel = CraftService.CalculatePCEffectiveLevel(GetPC(), model.PlayerSkillRank, (SkillType)model.Blueprint.SkillID);
                    int difficulty = effectiveLevel - model.AdjustedLevel;

                    if(difficulty <= -5)
                    {
                        GetPC().FloatingText("It's impossible to make this item because its level is too high. Use lower-level components to reduce the level and difficulty.");
                        return;
                    }

                    CraftService.CraftItem(GetPC(), device);
                    model.IsAccessingStorage = true;
                    EndConversation();
                    break;
                case 3: // Select main components
                    model.Access = CraftingAccessType.MainComponent;
                    OpenDeviceInventory();
                    break;
                case 4: // Select secondary components
                    model.Access = CraftingAccessType.SecondaryComponent;
                    OpenDeviceInventory();
                    break;
                case 5: // Select tertiary components
                    model.Access = CraftingAccessType.TertiaryComponent;
                    OpenDeviceInventory();
                    break;
                case 6: // Select enhancement components
                    model.Access = CraftingAccessType.Enhancement;
                    OpenDeviceInventory();
                    break;
                case 7: // Back (return to blueprint selection)
                    CraftService.ClearPlayerCraftingData(GetPC());
                    SwitchConversation("CraftingDevice");
                    break;
            }
        }

        private void OpenDeviceInventory()
        {
            var model = CraftService.GetPlayerCraftingData(GetPC());
            NWPlaceable device = GetDevice();
            device.IsLocked = false;
            model.IsAccessingStorage = true;

            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_USED, string.Empty);
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN, "script_2");
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, "script_3");
            _.SetEventScript(device.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, "script_4");

            device.SetLocalString("SCRIPT_2", "Placeable.CraftingDevice.OnOpened");
            device.SetLocalString("SCRIPT_3", "Placeable.CraftingDevice.OnClosed");
            device.SetLocalString("SCRIPT_4", "Placeable.CraftingDevice.OnDisturbed");

            GetPC().AssignCommand(() => _.ActionInteractObject(device.Object));
            EndConversation();
        }

    }
}
