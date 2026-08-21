using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class DestroyItemDialog: ConversationMenuDefinition
    {
        private class Model
        {
            public uint Item { get; set; }
            public string ItemName { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ConfirmPageId = "CONFIRM_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialization)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ConfirmPageId, ConfirmPageInit);

            return builder.Build();
        }

        private void Initialization()
        {
            var player = Player;
            var model = Data<Model>();
            model.Item = GetLocalObject(player, "DESTROY_ITEM");

            if (!GetIsObjectValid(model.Item))
            {
                FloatingTextStringOnCreature("Could not locate item to destroy. Notify an admin.", player, false);
                Close();
                return;
            }

            model.ItemName = GetName(model.Item);
            DeleteLocalObject(player, "DESTROY_ITEM");
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            page.Header = $"Item: {model.ItemName}\n\n" +
                          "Are you sure you want to destroy this item? This action is irreversible!";

            page.AddResponse("Destroy Item", () =>
            {
                GoToPage(ConfirmPageId);
            });
        }

        private void ConfirmPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            page.Header = $"Item: {model.ItemName}\n\n" +
                          "Are you sure you want to destroy this item? This action is irreversible!";

            page.AddResponse(ColorToken.Red("CONFIRM DESTROY ITEM"), () =>
            {
                DestroyObject(model.Item);
                Close();
            });
        }
    }
}
