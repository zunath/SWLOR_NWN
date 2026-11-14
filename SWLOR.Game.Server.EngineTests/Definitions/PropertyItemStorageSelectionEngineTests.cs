using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The property storage window clears its item selection, and disables Retrieve, whenever the item list is
    /// rebuilt (another category, reopening the window), and ignores clicks for rows its lists no longer have.
    /// </summary>
    public static class PropertyItemStorageSelectionEngineTests
    {
        [EngineTest("Changing property storage category or reopening the window clears the selected item", Category = "PropertyStorage")]
        public static async Task CategoryChangeClearsItemSelection(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var property = new WorldProperty
            {
                Id = "engine-storage-select/" + fixture.Id, OwnerPlayerId = fixture.Id, CustomName = "Selection storage",
                PropertyType = PropertyType.Apartment, ItemStorageCount = 10
            };
            var first = new WorldPropertyCategory { ParentPropertyId = property.Id, Name = "First" };
            var second = new WorldPropertyCategory { ParentPropertyId = property.Id, Name = "Second" };
            var permissions = new[] { Permission(fixture.Id, first.Id), Permission(fixture.Id, second.Id) };
            var previousPropertyId = Property.GetPropertyId(ctx.Arena);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    first.Items["engine-select-a"] = StoredItem(ctx);
                    first.Items["engine-select-b"] = StoredItem(ctx);
                    second.Items["engine-select-c"] = StoredItem(ctx);
                    DB.Set(property);
                    DB.Set(first);
                    DB.Set(second);
                    foreach (var permission in permissions)
                        DB.Set(permission);
                    SetLocalString(ctx.Arena, "PROPERTY_ID", property.Id);

                    var model = OpenWindow<PropertyItemStorageViewModel>(fixture, GuiWindowType.PropertyItemStorage, null);
                    var categoryIds = (List<string>)Get(model, "_categoryIds");
                    var firstIndex = categoryIds.IndexOf(first.Id);
                    var secondIndex = categoryIds.IndexOf(second.Id);
                    ctx.Assert(firstIndex >= 0 && secondIndex >= 0, "The window lists both categories");

                    Call(model, "SelectCategory", firstIndex);
                    Call(model, "SelectItem", 0);
                    ctx.Assert(model.IsItemSelected, "Selecting an item enables Retrieve");

                    // The reported crash: another category leaves Retrieve enabled with no item selected.
                    Call(model, "SelectCategory", secondIndex);
                    ctx.Assert(!model.IsItemSelected, "Changing category disables Retrieve");
                    ctx.AssertEqual(-1, (int)Get(model, "SelectedItemIndex"), "Changing category clears the selected item");
                    model.OnRetrieveItem()();
                    ctx.AssertEqual(2, DB.Get<WorldPropertyCategory>(first.Id).Items.Count, "The first category keeps its items");
                    ctx.AssertEqual(1, DB.Get<WorldPropertyCategory>(second.Id).Items.Count, "The second category keeps its item");
                    ctx.AssertEqual(0, CountCarried(player, LargeItemResref), "Nothing is retrieved");

                    // Clicks for rows past the end of the lists are ignored.
                    Call(model, "SelectItem", 5);
                    ctx.AssertEqual(-1, (int)Get(model, "SelectedItemIndex"), "A missing item row is not selected");
                    ctx.Assert(!model.IsItemSelected, "A missing item row leaves Retrieve disabled");
                    Call(model, "SelectCategory", 9);
                    ctx.AssertEqual(secondIndex, (int)Get(model, "SelectedCategoryIndex"), "A missing category row keeps the category");

                    // Reopening the window starts without an item selection.
                    Call(model, "SelectItem", 0);
                    ctx.Assert(model.IsItemSelected, "Selecting an item enables Retrieve again");
                    CloseWindows(fixture);
                    var reopened = OpenWindow<PropertyItemStorageViewModel>(fixture, GuiWindowType.PropertyItemStorage, null);
                    ctx.Assert(!reopened.IsItemSelected, "Reopening the window disables Retrieve");
                    reopened.OnRetrieveItem()();
                    ctx.AssertEqual(0, CountCarried(player, LargeItemResref), "Retrieve does nothing after reopening");
                });
            }
            finally
            {
                CloseWindows(fixture);
                SetLocalString(ctx.Arena, "PROPERTY_ID", previousPropertyId);
                foreach (var permission in permissions)
                    DB.Delete<WorldPropertyPermission>(permission.Id);
                DB.Delete<WorldPropertyCategory>(first.Id);
                DB.Delete<WorldPropertyCategory>(second.Id);
                DB.Delete<WorldProperty>(property.Id);
            }
        }

        private static WorldPropertyItem StoredItem(EngineTestContext ctx) => new()
        {
            Name = "Twin Electroblade", Resref = LargeItemResref, Quantity = 1, Data = SerializeNewItem(ctx, LargeItemResref)
        };

        private static WorldPropertyPermission Permission(string playerId, string categoryId) => new()
        {
            PlayerId = playerId, PropertyId = categoryId,
            Permissions = Property.GetPermissionsByPropertyType(PropertyType.Category).ToDictionary(x => x, _ => true),
            GrantPermissions = Property.GetPermissionsByPropertyType(PropertyType.Category).ToDictionary(x => x, _ => true)
        };
    }
}
