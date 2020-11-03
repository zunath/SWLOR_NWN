using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ViewKeyItemsDialog: DialogBase
    {
        private class Model
        {
            public KeyItemCategoryType SelectedCategory { get; set; }
            public KeyItemType SelectedKeyItem { get; set; }
        }

        private const string CategoryPageId = "CATEGORY_PAGE";
        private const string KeyItemListPageId = "KEY_ITEM_LIST_PAGE";
        private const string KeyItemDetailPageId = "KEY_ITEM_DETAIL_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(CategoryPageId, MainPageInit)
                .AddPage(KeyItemListPageId, KeyItemListInit)
                .AddPage(KeyItemDetailPageId, KeyItemDetailInit);


            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = "Please select a key item category.";

            var categories = KeyItem.GetActiveCategories();
            foreach (var (type, category) in categories)
            {
                page.AddResponse(category.Name, () =>
                {
                    model.SelectedCategory = type;
                    ChangePage(KeyItemListPageId);
                });
            }
        }

        private void KeyItemListInit(DialogPage page)
        {
            page.Header = "Please select a key item.";

            var player = GetPC();
            var model = GetDataModel<Model>();
            var keyItems = KeyItem.GetActiveKeyItemsByCategory(model.SelectedCategory);
            foreach (var (type, keyItem) in keyItems)
            {
                if (KeyItem.HasKeyItem(player, type))
                {
                    page.AddResponse(keyItem.Name, () =>
                    {
                        model.SelectedKeyItem = type;
                        ChangePage(KeyItemDetailPageId);
                    });
                }
            }

        }

        private void KeyItemDetailInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var detail = KeyItem.GetKeyItem(model.SelectedKeyItem);
            var acquiredDate = dbPlayer.KeyItems[model.SelectedKeyItem];

            page.Header = $"{ColorToken.Green("Name:")} {detail.Name}\n" +
                          $"{ColorToken.Green("Acquired Date:")} {acquiredDate:yyyy-MM-dd hh:mm:ss}\n\n" +
                $"{ColorToken.Green("Description:")} {detail.Description}";
        }
    }
}
