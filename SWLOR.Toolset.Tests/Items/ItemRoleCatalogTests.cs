using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>RolesFor per family, data-driven Classify, and the Custom-only variables rule.</summary>
    [TestFixture]
    public class ItemRoleCatalogTests
    {
        [Test]
        public void RolesFor_ReturnsTheDeclaredSetPerFamily()
        {
            RoleIds(ItemFamily.Miscellaneous).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.ConsumableId, ItemRoleCatalog.MealId,
                ItemRoleCatalog.DeployedDeviceId, ItemRoleCatalog.DroidPartId,
                ItemRoleCatalog.IncubationSampleId, ItemRoleCatalog.SchematicId,
                ItemRoleCatalog.KeyItemId, ItemRoleCatalog.CustomId
            });

            RoleIds(ItemFamily.Essence).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.ComponentId, ItemRoleCatalog.EnhancementId, ItemRoleCatalog.CustomId
            });

            RoleIds(ItemFamily.CreatureItem).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.CreatureItemId, ItemRoleCatalog.CustomId
            });

            RoleIds(ItemFamily.Tool).Should().BeEquivalentTo(new[] { ItemRoleCatalog.CustomId });

            ItemRoleCatalog.RolesFor(ItemFamily.Armor).Should().BeEmpty();
            ItemRoleCatalog.RolesFor(ItemFamily.MeleeWeapon).Should().BeEmpty();
        }

        [Test]
        public void Classify_ReadsWhatThePropertiesListAlreadySays()
        {
            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem()), ItemFamily.CreatureItem).Id
                .Should().Be(ItemRoleCatalog.CreatureItemId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((123, 0))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.DroidPartId, "DroidInstruction also marks a droid part");

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((122, 4))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.DroidPartId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((127, 0))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.IncubationSampleId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((130, 0))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.SchematicId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((106, 0))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.MealId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((108, 0))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.MealId, "FoodEnhancement also marks a meal");

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((15, 0))), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.ConsumableId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem()), ItemFamily.Essence).Id
                .Should().Be(ItemRoleCatalog.ComponentId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem((101, 0))), ItemFamily.Essence).Id
                .Should().Be(ItemRoleCatalog.EnhancementId);

            ItemRoleCatalog.Classify(new ItemValueStore(BuildItem()), ItemFamily.Miscellaneous).Id
                .Should().Be(ItemRoleCatalog.CustomId);
        }

        [Test]
        public void OnlyCustomAllowsVariables()
        {
            ItemRoleCatalog.All.Where(role => role.AllowsVariables)
                .Should().ContainSingle().Which.Id.Should().Be(ItemRoleCatalog.CustomId);
        }

        [Test]
        public void GroupsUnlockedBy_MatchesTheDeclaredRoleUnlocks()
        {
            ItemRoleCatalog.GroupsUnlockedBy(ItemRoleCatalog.DroidPartId)
                .Should().BeEquivalentTo(new[] { ItemStatGroup.Droid });
            ItemRoleCatalog.GroupsUnlockedBy(ItemRoleCatalog.MealId)
                .Should().BeEquivalentTo(new[] { ItemStatGroup.Bonuses, ItemStatGroup.Enhancements });
            ItemRoleCatalog.GroupsUnlockedBy(ItemRoleCatalog.IncubationSampleId)
                .Should().BeEquivalentTo(new[] { ItemStatGroup.Incubation });
            ItemRoleCatalog.GroupsUnlockedBy(ItemRoleCatalog.SchematicId)
                .Should().BeEquivalentTo(new[] { ItemStatGroup.Crafting });
            ItemRoleCatalog.GroupsUnlockedBy(ItemRoleCatalog.ComponentId).Should().BeEmpty();
        }

        private static IEnumerable<string> RoleIds(ItemFamily family) =>
            ItemRoleCatalog.RolesFor(family).Select(role => role.Id);

        /// <summary>A minimal uti struct carrying only the given PropertyName/Subtype pairs.</summary>
        private static JsonGffStruct BuildItem(params (int PropertyId, int SubtypeId)[] properties)
        {
            var entries = string.Join(",\n", properties.Select(property => $$"""
                {
                  "__struct_id": 0,
                  "ChanceAppear": { "type": "byte", "value": 100 },
                  "CostTable": { "type": "byte", "value": 0 },
                  "CostValue": { "type": "word", "value": 1 },
                  "Param1": { "type": "byte", "value": 255 },
                  "Param1Value": { "type": "byte", "value": 0 },
                  "PropertyName": { "type": "word", "value": {{property.PropertyId}} },
                  "Subtype": { "type": "word", "value": {{property.SubtypeId}} }
                }
                """));

            var json = $$"""
                {
                  "__data_type": "UTI ",
                  "TemplateResRef": { "type": "resref", "value": "test" },
                  "PropertiesList": { "type": "list", "value": [{{entries}}] }
                }
                """;

            return JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json)).Root;
        }
    }
}
