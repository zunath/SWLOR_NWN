using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class CraftItem : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage();

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

                NWScript.SetEventScript(device.Object, EventScript.Placeable_OnUsed, "script_1");
                NWScript.SetEventScript(device.Object, EventScript.Placeable_OnOpen, string.Empty);
                NWScript.SetEventScript(device.Object, EventScript.Placeable_OnClosed, string.Empty);
                NWScript.SetEventScript(device.Object, EventScript.Placeable_OnInventoryDisturbed, string.Empty);
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
            var maxEnhancements = model.PlayerPerkLevel / 2;
            var canAddEnhancements = model.Blueprint.EnhancementSlots > 0 && maxEnhancements > 0;

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
            var device = GetDevice();

            switch (responseID)
            {
                case 1: // Examine Base Item
                    var entity = CraftService.GetBlueprintByID(model.BlueprintID);
                    NWPlaceable tempContainer = (NWScript.GetObjectByTag("craft_temp_store"));
                    NWItem examineItem = (NWScript.CreateItemOnObject(entity.ItemResref, tempContainer.Object));
                    GetPC().AssignCommand(() => NWScript.ActionExamine(examineItem.Object));
                    examineItem.Destroy(0.1f);
                    break;
                case 2: // Create item
                    if (!model.CanBuildItem)
                    {
                        GetPC().FloatingText("You are missing some required components.");
                        return;
                    }

                    var effectiveLevel = model.PlayerSkillRank;
                    var difficulty = effectiveLevel - model.AdjustedLevel;

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
            var device = GetDevice();
            device.IsLocked = false;
            model.IsAccessingStorage = true;

            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnUsed, string.Empty);
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnOpen, "script_2");
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnClosed, "script_3");
            NWScript.SetEventScript(device.Object, EventScript.Placeable_OnInventoryDisturbed, "script_4");

            device.SetLocalString("SCRIPT_2", "Placeable.CraftingDevice.OnOpened");
            device.SetLocalString("SCRIPT_3", "Placeable.CraftingDevice.OnClosed");
            device.SetLocalString("SCRIPT_4", "Placeable.CraftingDevice.OnDisturbed");

            GetPC().AssignCommand(() => NWScript.ActionInteractObject(device.Object));
            EndConversation();
        }

    }
}
