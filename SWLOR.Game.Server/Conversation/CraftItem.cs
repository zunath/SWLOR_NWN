using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class CraftItem: ConversationBase
    {
        private readonly ICraftService _craft;
        private readonly IColorTokenService _color;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;

        public CraftItem(
            INWScript script, 
            IDialogService dialog,
            ICraftService craft,
            IColorTokenService color,
            IPerkService perk,
            ISkillService skill) 
            : base(script, dialog)
        {
            _craft = craft;
            _color = color;
            _perk = perk;
            _skill = skill;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage();
            
            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            var model = _craft.GetPlayerCraftingData(GetPC());
            var device = GetDevice();

            // Entering the conversation for the first time from the blueprint selection menu.
            if (!model.IsInitialized)
            {
                model.IsInitialized = true;
                model.Blueprint = _craft.GetBlueprintByID(model.BlueprintID);
                model.PlayerSkillRank = _skill.GetPCSkill(GetPC(), model.Blueprint.SkillID).Rank;

                switch ((SkillType) model.Blueprint.SkillID)
                {
                    case SkillType.Armorsmith:
                        model.PlayerPerkLevel = _perk.GetPCPerkLevel(GetPC(), PerkType.ArmorBlueprints);
                        break;
                    case SkillType.Engineering:
                        model.PlayerPerkLevel = _perk.GetPCPerkLevel(GetPC(), PerkType.EngineeringBlueprints);
                        break;
                    case SkillType.Weaponsmith:
                        model.PlayerPerkLevel = _perk.GetPCPerkLevel(GetPC(), PerkType.WeaponBlueprints);
                        break;
                    case SkillType.Fabrication:
                        model.PlayerPerkLevel = _perk.GetPCPerkLevel(GetPC(), PerkType.FabricationBlueprints);
                        break;
                    case SkillType.Medicine:
                        model.PlayerPerkLevel = _perk.GetPCPerkLevel(GetPC(), PerkType.MedicalBlueprints);
                        break;
                    default:
                        model.PlayerPerkLevel = 0;
                        break;

                }
                GetDevice().IsLocked = true;
            }
            // Otherwise returning from accessing the device's inventory.
            else
            {
                model.Access = CraftingAccessType.None;

                _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_USED, "jvm_script_1");
                _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
                _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
                _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
            }


            SetPageHeader("MainPage", _craft.BuildBlueprintHeader(GetPC(), model.BlueprintID, true));
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

        public override void EndDialog()
        {
            var model = _craft.GetPlayerCraftingData(GetPC());
            if (model.IsAccessingStorage) return;

            _craft.ClearPlayerCraftingData(GetPC());
        }
        
        private NWPlaceable GetDevice()
        {
            return NWPlaceable.Wrap(GetDialogTarget().Object);
        }
        

        private void BuildMainPageOptions()
        {
            var model = _craft.GetPlayerCraftingData(GetPC());
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
            var model = _craft.GetPlayerCraftingData(GetPC());
            NWPlaceable device = GetDevice();

            switch(responseID)
            {
                case 1: // Examine Base Item
                    CraftBlueprint entity = _craft.GetBlueprintByID(model.BlueprintID);
                    NWPlaceable tempContainer = NWPlaceable.Wrap(_.GetObjectByTag("craft_temp_store"));
                    NWItem examineItem = NWItem.Wrap(_.CreateItemOnObject(entity.ItemResref, tempContainer.Object));
                    GetPC().AssignCommand(() => _.ActionExamine(examineItem.Object));
                    examineItem.Destroy(0.1f);
                    break;
                case 2: // Create item
                    if(!model.CanBuildItem)
                    {
                        GetPC().FloatingText("You are missing some required components.");
                        return;
                    }

                    _craft.CraftItem(GetPC(), device);
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
                    _craft.ClearPlayerCraftingData(GetPC());
                    SwitchConversation("CraftingDevice");
                    break;
            }
        }

        private void OpenDeviceInventory()
        {
            var model = _craft.GetPlayerCraftingData(GetPC());
            NWPlaceable device = GetDevice();
            device.IsLocked = false;
            model.IsAccessingStorage = true;

            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_USED, string.Empty);
            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_OPEN, "jvm_script_2");
            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_CLOSED, "jvm_script_3");
            _.SetEventScript(device.Object, NWScript.EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, "jvm_script_4");

            device.SetLocalString("JAVA_SCRIPT_2", "Placeable.CraftingDevice.OnOpened");
            device.SetLocalString("JAVA_SCRIPT_3", "Placeable.CraftingDevice.OnClosed");
            device.SetLocalString("JAVA_SCRIPT_4", "Placeable.CraftingDevice.OnDisturbed");

            GetPC().AssignCommand(() => _.ActionInteractObject(device.Object));
            EndConversation();
        }

    }
}
