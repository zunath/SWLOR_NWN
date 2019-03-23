using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Placeable.MolecularReassembler;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;

namespace SWLOR.Game.Server.Conversation
{
    public class MolecularReassembly: ConversationBase
    {
        
        
        private readonly ICraftService _craft;
        private readonly ISerializationService _serialization;
        

        public MolecularReassembly(
             
            IDialogService dialog,
            
            
            ICraftService craft,
            ISerializationService serialization) 
            : base(dialog)
        {
            
            
            _craft = craft;
            _serialization = serialization;
            
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(); // Dynamically built
            DialogPage salvagePage = new DialogPage("<SET LATER>",
                "Reassemble Component(s)"); 
            
            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("SalvagePage", salvagePage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainPageResponses(responseID);
                    break;
                case "SalvagePage":
                    SalvagePageResponses(responseID);
                    break;
            }
        }

        private void LoadMainPage()
        {
            string header = ColorTokenService.Green("Molecular Reassembler") + "\n\n";
            header += "This device can be used to salvage equipment and reassemble them into components.\n\n";
            header += "Please select the type of item you wish to create. The new item(s) created will have a chance to receive property bonuses from the salvaged item.\n\n";
            header += "Start by selecting a component type now.";
            SetPageHeader("MainPage", header);

            ClearPageResponses("MainPage");
            var componentTypes = DataService.Where<ComponentType>(x => !string.IsNullOrWhiteSpace(x.ReassembledResref));
            foreach (var type in componentTypes)
            {
                AddResponseToPage("MainPage", type.Name, true, type.ID);
            }
        }

        private void MainPageResponses(int responseID)
        {
            var player = GetPC();
            var model = _craft.GetPlayerCraftingData(player);
            DialogResponse response = GetResponseByID("MainPage", responseID);
            model.SalvageComponentTypeID = (int)response.CustomData;

            LoadSalvagePage();
            ChangePage("SalvagePage");
        }
        
        private void LoadSalvagePage()
        {
            var player = GetPC();
            var model = _craft.GetPlayerCraftingData(player);
            NWPlaceable tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = _serialization.DeserializeItem(model.SerializedSalvageItem, tempStorage);
            var componentType = DataService.Get<ComponentType>(model.SalvageComponentTypeID);
            string header = ColorTokenService.Green("Item: ") + item.Name + "\n\n";
            header += "Reassembling this item will create the following " + ColorTokenService.Green(componentType.Name) + " component(s). Chance to create depends on your perks, skills, and harvesting bonus on items.\n\n";

            // Always create one item with zero bonuses.
            header += componentType.Name + " (No Bonuses) [RL: 0] " + GetChanceColor(100) + "\n";

            // Start by checking attack bonus since we're not storing this value as a local variable on the item.
            foreach (var prop in item.ItemProperties)
            {
                int propTypeID = _.GetItemPropertyType(prop);
                if (propTypeID == ITEM_PROPERTY_ATTACK_BONUS)
                {
                    // Get the amount of Attack Bonus
                    int amount = _.GetItemPropertyCostTableValue(prop);
                    header += ProcessPropertyDetails(amount, componentType.Name, "Attack Bonus", 3);
                }
            }

            // Now check specific custom properties which are stored as local variables on the item.
            header += ProcessPropertyDetails(item.CustomAC, componentType.Name, "AC", 3);
            header += ProcessPropertyDetails(item.HarvestingBonus, componentType.Name, "Harvesting Bonus", 3);
            header += ProcessPropertyDetails(item.PilotingBonus, componentType.Name, "Piloting Bonus", 3);
            header += ProcessPropertyDetails(item.ScanningBonus, componentType.Name, "Scanning Bonus", 3);
            header += ProcessPropertyDetails(item.ScavengingBonus, componentType.Name, "Scavenging Bonus", 3);
            header += ProcessPropertyDetails(item.CastingSpeed, componentType.Name, "Activation Speed", 3);
            header += ProcessPropertyDetails(item.CraftBonusArmorsmith, componentType.Name, "Armorsmith", 3);
            header += ProcessPropertyDetails(item.CraftBonusWeaponsmith, componentType.Name, "Weaponsmith", 3);
            header += ProcessPropertyDetails(item.CraftBonusCooking, componentType.Name, "Cooking", 3);
            header += ProcessPropertyDetails(item.CraftBonusEngineering, componentType.Name, "Engineering", 3);
            header += ProcessPropertyDetails(item.CraftBonusFabrication, componentType.Name, "Fabrication", 3);
            header += ProcessPropertyDetails(item.HPBonus, componentType.Name, "HP", 5, 0.5f);
            header += ProcessPropertyDetails(item.FPBonus, componentType.Name, "FP", 5, 0.5f);
            header += ProcessPropertyDetails(item.EnmityRate, componentType.Name, "Enmity", 3);
            header += ProcessPropertyDetails(item.ForcePotencyBonus, componentType.Name, "Force Potency", 3);
            header += ProcessPropertyDetails(item.ForceAccuracyBonus, componentType.Name, "Force Accuracy", 3);
            header += ProcessPropertyDetails(item.ForceDefenseBonus, componentType.Name, "Force Defense", 3);
            header += ProcessPropertyDetails(item.ElectricalPotencyBonus, componentType.Name, "Electrical Potency", 3);
            header += ProcessPropertyDetails(item.MindPotencyBonus, componentType.Name, "Mind Potency", 3);
            header += ProcessPropertyDetails(item.LightPotencyBonus, componentType.Name, "Light Potency", 3);
            header += ProcessPropertyDetails(item.DarkPotencyBonus, componentType.Name, "Dark Potency", 3);
            header += ProcessPropertyDetails(item.ElectricalDefenseBonus, componentType.Name, "Electrical Defense", 3);
            header += ProcessPropertyDetails(item.MindDefenseBonus, componentType.Name, "Mind Defense", 3);
            header += ProcessPropertyDetails(item.LightDefenseBonus, componentType.Name, "Light Defense", 3);
            header += ProcessPropertyDetails(item.DarkDefenseBonus, componentType.Name, "Dark Defense", 3);
            header += ProcessPropertyDetails(item.LuckBonus, componentType.Name, "Luck", 3);
            header += ProcessPropertyDetails(item.MeditateBonus, componentType.Name, "Meditate", 3);
            header += ProcessPropertyDetails(item.RestBonus, componentType.Name, "Rest", 3);
            header += ProcessPropertyDetails(item.MedicineBonus, componentType.Name, "Medicine", 3);
            header += ProcessPropertyDetails(item.HPRegenBonus, componentType.Name, "HP Regen", 3);
            header += ProcessPropertyDetails(item.FPRegenBonus, componentType.Name, "FP Regen", 3);
            header += ProcessPropertyDetails(item.BaseAttackBonus, componentType.Name, "BAB", 3, 6);
            header += ProcessPropertyDetails(item.StructureBonus, componentType.Name, "Structure Bonus", 3);
            header += ProcessPropertyDetails(item.SneakAttackBonus, componentType.Name, "Sneak Attack", 3);
            header += ProcessPropertyDetails(item.DamageBonus, componentType.Name, "Damage", 3);
            header += ProcessPropertyDetails(item.StrengthBonus, componentType.Name, "STR", 3);
            header += ProcessPropertyDetails(item.DexterityBonus, componentType.Name, "DEX", 3);
            header += ProcessPropertyDetails(item.ConstitutionBonus, componentType.Name, "CON", 3);
            header += ProcessPropertyDetails(item.WisdomBonus, componentType.Name, "WIS", 3);
            header += ProcessPropertyDetails(item.IntelligenceBonus, componentType.Name, "INT", 3);
            header += ProcessPropertyDetails(item.CharismaBonus, componentType.Name, "CHA", 3);
            header += ProcessPropertyDetails(item.DurationBonus, componentType.Name, "Duration", 3);


            SetPageHeader("SalvagePage", header);

            // Remove the temporary copy from the game world.
            item.Destroy();
        }

        private string GetChanceColor(int chance)
        {
            string message = "-" + chance + "%-";
            if (chance <= 50)
                return ColorTokenService.Red(message);
            else if (chance <= 80)
                return ColorTokenService.Yellow(message);
            else return ColorTokenService.Green(message);
        }

        private string ProcessPropertyDetails(int amount, string componentName, string propertyName, int maxBonuses, float levelsPerBonus = 1.0f)
        {
            var player = GetPC();
            string result = string.Empty;
            int penalty = 0;
            while (amount > 0)
            {
                if (amount >= maxBonuses)
                {
                    int levelIncrease = (int)(maxBonuses * levelsPerBonus);
                    int chanceToTransfer = _craft.CalculateReassemblyChance(player, penalty);
                    result += componentName + " (+" + maxBonuses + " " + propertyName + ") [RL: " + levelIncrease + "] " + GetChanceColor(chanceToTransfer) + "\n";
                    penalty += (maxBonuses * 5);
                    amount -= maxBonuses;
                }
                else
                {
                    int levelIncrease = (int)(amount * levelsPerBonus);
                    int chanceToTransfer = _craft.CalculateReassemblyChance(player, penalty);
                    result += componentName + " (+" + amount + " " + propertyName + ") [RL: " + levelIncrease + "] " + GetChanceColor(chanceToTransfer)+ "\n";
                    break;
                }
            }

            return result;
        }

        private void SalvagePageResponses(int responseID)
        {
            var player = GetPC();
            var model = _craft.GetPlayerCraftingData(player);

            switch (responseID)
            {
                case 1: // Reassemble Component(s)
                    if (model.IsConfirmingReassemble)
                    {
                        // Calculate delay, fire off delayed event, and show timing bar.
                        float delay = _craft.CalculateCraftingDelay(player, (int) SkillType.Harvesting);
                        NWNXPlayer.StartGuiTimingBar(player, delay, string.Empty);
                        player.DelayEvent<ReassembleComplete>(delay, player, model.SerializedSalvageItem, model.SalvageComponentTypeID);

                        // Make the player play an animation.
                        player.AssignCommand(() =>
                        {
                            _.ClearAllActions();
                            _.ActionPlayAnimation(ANIMATION_LOOPING_GET_MID, 1.0f, delay);
                        });

                        // Show sparks halfway through the process.
                        _.DelayCommand(1.0f * (delay / 2.0f), () =>
                        {
                            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_COM_BLOOD_SPARK_MEDIUM), Object.OBJECT_SELF);
                        });
                        
                        // Immobilize the player while crafting.
                        Effect immobilize = _.EffectCutsceneImmobilize();
                        immobilize = _.TagEffect(immobilize, "CRAFTING_IMMOBILIZATION");
                        _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, immobilize, player);

                        // Clear the temporary crafting data and end this conversation.
                        model.SerializedSalvageItem = string.Empty;
                        EndConversation();
                    }
                    else
                    {
                        model.IsConfirmingReassemble = true;
                        SetResponseText("SalvagePage", 1, "CONFIRM REASSEMBLE COMPONENT(S)");
                    }
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var model = _craft.GetPlayerCraftingData(player);
            model.IsConfirmingReassemble = false;
            SetResponseText("SalvagePage", 1, "Reassemble Component(s)");
        }

        public override void EndDialog()
        {
            var player = GetPC();
            _craft.ClearPlayerCraftingData(player);
        }
    }
}
