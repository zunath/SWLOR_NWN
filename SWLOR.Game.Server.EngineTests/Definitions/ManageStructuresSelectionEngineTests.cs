using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The structure window ignores row clicks for rows that no longer exist (a client click that arrives
    /// before the list refresh), and its edit buttons do nothing instead of throwing while the selection is stale.
    /// </summary>
    public static class ManageStructuresSelectionEngineTests
    {
        [EngineTest("Clicking a structure row that no longer exists keeps the window usable", Category = "ManageStructures")]
        public static async Task StaleRowClickKeepsWindowUsable(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var parent = new WorldProperty
            {
                Id = "engine-structure-select/" + fixture.Id, OwnerPlayerId = fixture.Id, CustomName = "Selection apartment",
                PropertyType = PropertyType.Apartment
            };
            var structure = new WorldProperty
            {
                ParentPropertyId = parent.Id, OwnerPlayerId = fixture.Id, CustomName = "Selection obelisk",
                PropertyType = PropertyType.Structure, StructureType = StructureType.ObeliskLarge
            };
            parent.ChildPropertyIds[PropertyChildType.Structure] = new List<string> { structure.Id };
            var permission = new WorldPropertyPermission
            {
                PlayerId = fixture.Id, PropertyId = parent.Id,
                Permissions = Property.GetPermissionsByPropertyType(PropertyType.Apartment).ToDictionary(x => x, _ => true),
                GrantPermissions = Property.GetPermissionsByPropertyType(PropertyType.Apartment).ToDictionary(x => x, _ => true)
            };
            var previousPropertyId = Property.GetPropertyId(ctx.Arena);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    DB.Set(parent);
                    DB.Set(structure);
                    DB.Set(permission);
                    SetLocalString(ctx.Arena, "PROPERTY_ID", parent.Id);

                    var model = OpenWindow<ManageStructuresViewModel>(fixture, GuiWindowType.ManageStructures, null);
                    ctx.AssertEqual(1, model.StructureToggles.Count, "The window lists the one structure");

                    // A click for a row past the end of the list is ignored.
                    Call(model, "SelectStructure", 5);
                    ctx.AssertEqual(-1, (int)Get(model, "SelectedStructureIndex"), "A missing row is not selected");
                    ctx.Assert(!model.StructureToggles.Any(x => x), "No row shows as selected");

                    // A real row still selects.
                    Call(model, "SelectStructure", 0);
                    ctx.AssertEqual(0, (int)Get(model, "SelectedStructureIndex"), "The real row is selected");
                    ctx.Assert(model.StructureToggles[0], "The real row shows as selected");
                    ctx.AssertEqual(structure.CustomName, model.StructureName, "The selected structure's name loads");

                    // A later stale click leaves the current selection alone.
                    Call(model, "SelectStructure", 3);
                    ctx.AssertEqual(0, (int)Get(model, "SelectedStructureIndex"), "A stale click keeps the selection");

                    // Edit buttons do nothing, rather than throw, when the stored selection is out of range.
                    Set(model, "SelectedStructureIndex", 4);
                    model.OnXAxisUp()();
                    model.OnSetFacingEast()();
                    model.OnRotateClockwise()();
                    model.OnZAxisReset()();
                    model.OnDiscardChanges()();
                    model.OnCloseWindow()();
                    ctx.Assert(DB.Get<WorldProperty>(structure.Id) != null, "The structure is untouched");
                });
            }
            finally
            {
                CloseWindows(fixture);
                SetLocalString(ctx.Arena, "PROPERTY_ID", previousPropertyId);
                DB.Delete<WorldPropertyPermission>(permission.Id);
                DB.Delete<WorldProperty>(structure.Id);
                DB.Delete<WorldProperty>(parent.Id);
            }
        }
    }
}
