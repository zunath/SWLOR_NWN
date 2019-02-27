using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class AtomicReassembly: ConversationBase
    {
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly ICraftService _craft;
        private readonly ISerializationService _serialization;

        public AtomicReassembly(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IDataService data,
            ICraftService craft,
            ISerializationService serialization) 
            : base(script, dialog)
        {
            _color = color;
            _data = data;
            _craft = craft;
            _serialization = serialization;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(); // Dynamically built
            DialogPage salvagePage = new DialogPage(); // Dynamically built
            
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
            string header = _color.Green("Atomic Reassembler") + "\n\n";
            header += "This device can be used to salvage equipment and reassemble them into components.\n\n";
            header += "Please select the type of item you wish to create. The new item(s) created will have a chance to receive property bonuses from the salvaged item.\n\n";
            header += "Start by selecting a component type now.";
            SetPageHeader("MainPage", header);

            ClearPageResponses("MainPage");
            var componentTypes = _data.Where<ComponentType>(x => !string.IsNullOrWhiteSpace(x.ReassembledResref));
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
            var componentType = _data.Get<ComponentType>(model.SalvageComponentTypeID);
            string header = _color.Green("Item: ") + item.Name + "\n\n";
            header += "Reassembling this item will create the following " + _color.Green(componentType.Name) + " component(s).\n\n";
            
            // Start by checking attack bonus since we're not storing this value as a local variable on the item.
            foreach (var prop in item.ItemProperties)
            {
                int propTypeID = _.GetItemPropertyType(prop);
                if (propTypeID == ITEM_PROPERTY_ATTACK_BONUS)
                {
                    // Get the amount of Attack Bonus
                    int amount = _.GetItemPropertyCostTableValue(prop);
                    header += ProcessPropertyDetails(amount, "Attack Bonus");
                }
            }
            
            SetPageHeader("SalvagePage", header);

            // Remove the temporary copy from the game world.
            item.Destroy();
        }

        private string ProcessPropertyDetails(int amount, string propertyName)
        {
            string result = string.Empty;
            while (amount > 0)
            {
                if (amount >= 3)
                {
                    result += "+3 " + propertyName + "\n";
                    amount -= 3;
                }
                else
                {
                    result += "+" + amount + " " + propertyName + "\n";
                    break;
                }
            }

            return result;
        }

        private void SalvagePageResponses(int responseID)
        {

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
            var player = GetPC();
            _craft.ClearPlayerCraftingData(player);
        }





    }
}
