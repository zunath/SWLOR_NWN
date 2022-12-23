using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class DestroyItemDialog: DialogBase
    {
        private class Model
        {
            public uint Item { get; set; }
            public string ItemName { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ConfirmPageId = "CONFIRM_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialization)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ConfirmPageId, ConfirmPageInit);

            return builder.Build();
        }

        private void Initialization()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            model.Item = GetLocalObject(player, "DESTROY_ITEM");

            if (!GetIsObjectValid(model.Item))
            {
                FloatingTextStringOnCreature("Could not locate item to destroy. Notify an admin.", player, false);
                EndConversation();
                return;
            }

            model.ItemName = GetName(model.Item);
            DeleteLocalObject(player, "DESTROY_ITEM");
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = $"{ColorToken.Green("Item:")} {model.ItemName}\n\n" +
                          "Are you sure you want to destroy this item? This action is irreversible!";

            page.AddResponse("Destroy Item", () =>
            {
                ChangePage(ConfirmPageId);
            });
        }

        private void ConfirmPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = $"{ColorToken.Green("Item:")} {model.ItemName}\n\n" +
                          "Are you sure you want to destroy this item? This action is irreversible!";

            page.AddResponse(ColorToken.Red("CONFIRM DESTROY ITEM"), () =>
            {
                DestroyObject(model.Item);
                EndConversation();
            });
        }
    }
}
