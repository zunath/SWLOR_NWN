using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// The Behavior tab's role rail/card: rail shape per family, the Consumable/Grenade spell picker,
    /// and the switch-with-confirmation rule that clears only what the outgoing role actually owns.
    /// </summary>
    [TestFixture]
    public class ItemRoleSectionTests
    {
        private static readonly IReadOnlyList<BehaviorChoice> FakeSpells = new[]
        {
            new BehaviorChoice(10, "Fireball"),
            new BehaviorChoice(20, "Heal"),
            new BehaviorChoice(30, "Shock")
        };

        private static bool Accept(string description, Action mutation)
        {
            mutation();
            return true;
        }

        private static IReadOnlyList<BehaviorChoice> ResolveChoices(string key) =>
            key == ItemChoiceKeys.Spells ? FakeSpells : Array.Empty<BehaviorChoice>();

        private static ItemValueStore NewStore() =>
            new(JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                """{"__data_type":"UTI ","TemplateResRef":{"type":"resref","value":"test"}}""")).Root);

        private static ItemRoleSectionViewModel NewSection(
            ItemValueStore store,
            IEditorPromptService? prompts = null,
            Action<ItemRole>? roleChanged = null) =>
            new(store, Accept, ResolveChoices, prompts, roleChanged);

        // ----- rail shape -----

        [Test]
        public void Rebuild_MiscellaneousBuildsTheFullRoleRailWithCustomLast()
        {
            var section = NewSection(NewStore());

            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.Custom, "Misc Medium");

            section.HasRoles.Should().BeTrue();
            section.RoleList[0].IsHeader.Should().BeTrue();
            section.RoleList[0].Text.Should().Be("Misc Medium behaviors");

            var behaviorRows = section.RoleList.Where(row => row.IsSelectable).ToList();
            behaviorRows.Should().HaveCount(8, "7 non-Custom misc behaviors plus Custom");
            behaviorRows.Select(row => ((ItemRole)row.Behavior!).Id).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.ConsumableId, ItemRoleCatalog.MealId,
                ItemRoleCatalog.DeployedDeviceId, ItemRoleCatalog.DroidPartId,
                ItemRoleCatalog.IncubationSampleId, ItemRoleCatalog.SchematicId,
                ItemRoleCatalog.KeyItemId, ItemRoleCatalog.CustomId
            });

            section.RoleList.Should().Contain(row => row.IsRule);
            behaviorRows.Last().Behavior.Should().Be(ItemRoleCatalog.Custom);
            behaviorRows.Last().IsSelected.Should().BeTrue("Custom is the current role");
        }

        [Test]
        public void Rebuild_ArmorHasNoRolesAtAllAndTheRailStaysEmpty()
        {
            var section = NewSection(NewStore());

            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.Custom, "Armor");

            section.HasRoles.Should().BeFalse();
            section.RoleList.Should().BeEmpty();
        }

        // ----- spell picker card -----

        [Test]
        public void ConsumableCardShowsTheSpellPickerWithTheStoredSubtypeSelected()
        {
            var store = NewStore();
            store.SetPropertyValue(15, 20, 3, 1);
            var role = ItemRoleCatalog.Classify(store, ItemFamily.Miscellaneous);
            role.Id.Should().Be(ItemRoleCatalog.ConsumableId);

            var section = NewSection(store);
            section.Rebuild(ItemFamily.Miscellaneous, role, "Misc Medium");

            var card = section.Card!;
            card.ShowsSpellPicker.Should().BeTrue();
            card.SpellChoices.Should().HaveCount(3);
            card.SpellChoices.Single(choice => choice.Value == 20).IsSelected.Should().BeTrue();
            card.Statements.Should().ContainSingle(statement => statement.Contains("Heal"));
        }

        [Test]
        public void PickingAnotherSpellWritesTheNewSubtypeAndPreservesCostValue()
        {
            var store = NewStore();
            store.SetPropertyValue(15, 20, 3, 1);
            var role = ItemRoleCatalog.Classify(store, ItemFamily.Miscellaneous);

            var section = NewSection(store);
            section.Rebuild(ItemFamily.Miscellaneous, role, "Misc Medium");

            var card = section.Card!;
            var target = card.SpellChoices.Single(choice => choice.Value == 30);
            card.SelectSpellCommand.Execute(target);

            store.GetPropertyValue(15, 20).Should().BeNull("the old subtype entry is gone");
            store.GetPropertyValue(15, 30).Should().Be(1, "the CostValue of 1 carried over");
            card.SpellChoices.Single(choice => choice.Value == 30).IsSelected.Should().BeTrue();
            card.Statements.Should().ContainSingle(statement => statement.Contains("Shock"));
        }

        // ----- non-spell cards -----

        [Test]
        public void MealCardNamesTheGroupsItUnlocks()
        {
            var section = NewSection(NewStore());
            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.Get(ItemRoleCatalog.MealId), "Misc Medium");

            section.Card!.ShowsSpellPicker.Should().BeFalse();
            section.Card!.Statements.Should().ContainSingle(statement =>
                statement.Contains("Bonuses") && statement.Contains("Enhancement"));
        }

        [Test]
        public void CreatureItemCardNamesItsFixedStatements()
        {
            var section = NewSection(NewStore());
            section.Rebuild(ItemFamily.CreatureItem, ItemRoleCatalog.Get(ItemRoleCatalog.CreatureItemId), "Creature Item");

            section.Card!.Statements.Should().Contain(statement => statement.Contains("NPC group"));
            section.Card!.Statements.Should().Contain(statement => statement.Contains("restricted automatically"));
        }

        [Test]
        public void CustomCardNamesTheVariablesTab()
        {
            var section = NewSection(NewStore());
            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.Custom, "Misc Medium");

            section.Card!.Statements.Should().Contain(statement => statement.Contains("Variables"));
        }

        // ----- switching roles -----

        [Test]
        public async Task SwitchingAwayFromConsumableAsksAndClearsPropertyFifteenOnApproval()
        {
            var store = NewStore();
            store.SetPropertyValue(15, 20, 3, 1);
            var prompts = new StubPromptService(answer: true);
            ItemRole? changedTo = null;

            var section = NewSection(store, prompts, role => changedTo = role);
            section.Rebuild(
                ItemFamily.Miscellaneous, ItemRoleCatalog.Get(ItemRoleCatalog.ConsumableId), "Misc Medium");

            await section.ChooseRoleAsync(ItemRoleCatalog.Get(ItemRoleCatalog.MealId));

            prompts.Calls.Should().Be(1);
            prompts.Messages.Single().Should().Contain("Cast Spell");
            store.HasProperty(15).Should().BeFalse();
            section.Role.Id.Should().Be(ItemRoleCatalog.MealId);
            changedTo.Should().NotBeNull();
            changedTo!.Id.Should().Be(ItemRoleCatalog.MealId);
            section.RoleList.Single(row => row.IsSelected).Behavior.Should().Be(ItemRoleCatalog.Get(ItemRoleCatalog.MealId));
        }

        [Test]
        public async Task DecliningTheSwitchKeepsThePropertyAndTheOriginalSelection()
        {
            var store = NewStore();
            store.SetPropertyValue(15, 20, 3, 1);
            var prompts = new StubPromptService(answer: false);
            ItemRole? changedTo = null;

            var section = NewSection(store, prompts, role => changedTo = role);
            var consumable = ItemRoleCatalog.Get(ItemRoleCatalog.ConsumableId);
            section.Rebuild(ItemFamily.Miscellaneous, consumable, "Misc Medium");

            await section.ChooseRoleAsync(ItemRoleCatalog.Get(ItemRoleCatalog.MealId));

            prompts.Calls.Should().Be(1);
            store.HasProperty(15).Should().BeTrue("the switch was declined");
            section.Role.Id.Should().Be(ItemRoleCatalog.ConsumableId);
            changedTo.Should().BeNull();
            section.RoleList.Single(row => row.IsSelected).Behavior.Should().Be(consumable);
        }

        [Test]
        public async Task SwitchingWhenThePropertyIsAlreadyAbsentNeverPrompts()
        {
            var store = NewStore();
            var prompts = new StubPromptService(answer: true);

            var section = NewSection(store, prompts);
            section.Rebuild(
                ItemFamily.Miscellaneous, ItemRoleCatalog.Get(ItemRoleCatalog.ConsumableId), "Misc Medium");

            await section.ChooseRoleAsync(ItemRoleCatalog.Get(ItemRoleCatalog.MealId));

            prompts.Calls.Should().Be(0);
            section.Role.Id.Should().Be(ItemRoleCatalog.MealId);
        }

        [Test]
        public async Task SwitchingKeepsAPropertyTheTargetRoleAlsoOwns()
        {
            // Meal and Enhancement both own property 108 (HP bonus territory): switching between
            // them must neither prompt for it nor delete it, or the incoming card arrives empty.
            var store = NewStore();
            store.SetPropertyValue(108, -1, 1, 5);
            var prompts = new StubPromptService(answer: true);

            var section = NewSection(store, prompts);
            section.Rebuild(
                ItemFamily.Miscellaneous, ItemRoleCatalog.Get(ItemRoleCatalog.MealId), "Misc Medium");

            await section.ChooseRoleAsync(ItemRoleCatalog.Get(ItemRoleCatalog.EnhancementId));

            prompts.Calls.Should().Be(0, "nothing exclusive to Meal is being lost");
            store.GetPropertyValue(108, -1).Should().Be(5, "the shared property survives the switch");
            section.Role.Id.Should().Be(ItemRoleCatalog.EnhancementId);
        }

        [Test]
        public async Task SwitchingToTheAlreadyCurrentRoleIsANoOp()
        {
            var store = NewStore();
            store.SetPropertyValue(15, 20, 3, 1);
            var prompts = new StubPromptService(answer: true);

            var section = NewSection(store, prompts);
            var consumable = ItemRoleCatalog.Get(ItemRoleCatalog.ConsumableId);
            section.Rebuild(ItemFamily.Miscellaneous, consumable, "Misc Medium");

            await section.ChooseRoleAsync(consumable);

            prompts.Calls.Should().Be(0);
            store.HasProperty(15).Should().BeTrue();
        }

        // ----- ItemRoleOwnership -----

        [Test]
        public void OwnedProperties_MatchesTheSpecPerRole()
        {
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.ConsumableId).Should().BeEquivalentTo(new[] { 15 });
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.MealId).Should().BeEquivalentTo(new[] { 106, 108 });
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.DroidPartId)
                .Should().BeEquivalentTo(new[] { 121, 122, 123, 124 });
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.IncubationSampleId)
                .Should().BeEquivalentTo(new[] { 127, 128, 129 });
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.SchematicId).Should().BeEquivalentTo(new[] { 130 });
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.EnhancementId)
                .Should().BeEquivalentTo(new[] { 101, 102, 107, 108, 109, 110, 116 });
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.KeyItemId).Should().BeEmpty();
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.ComponentId).Should().BeEmpty();
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.CreatureItemId).Should().BeEmpty();
            ItemRoleOwnership.OwnedProperties(ItemRoleCatalog.CustomId).Should().BeEmpty();
        }

        [Test]
        public void LabelFor_UsesTheExplicitMapForCastSpellAndTheStatCatalogOtherwise()
        {
            ItemRoleOwnership.LabelFor(15).Should().Be("Cast Spell");
            ItemRoleOwnership.LabelFor(94).Should().Be(
                ItemStatCatalog.All.First(definition => definition.PropertyId == 94).Label);
        }

        // ----- ItemSpellChoiceCatalog -----

        [Test]
        public void Read_WithNoTwoDaServiceReturnsEmptyWithoutThrowing()
        {
            ItemSpellChoiceCatalog.Read(null, null).Should().BeEmpty();
        }

        [Test]
        public void Read_RejectsSentinelAndIncompleteSpellRows()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"item-spell-policy-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "iprp_spells.2da"),
                    "2DA V2.0\r\n\r\n" +
                    "Label Name CasterLvl InnateLvl SpellIndex PotionUse WandUse GeneralUse Icon\r\n" +
                    "0 Valid_Spell 100 5 3 42 0 0 1 iss_valid\r\n" +
                    "1 Calm_Emotions **** **** **** **** **** **** **** ****\r\n" +
                    "2 Bio_reserved 101 5 3 43 0 0 1 iss_reserved\r\n" +
                    "3 Missing_Spell_Index 102 5 3 **** 0 0 1 iss_missing\r\n" +
                    "4 Malformed_StrRef not-a-number 5 3 44 0 0 1 iss_malformed\r\n");

                var choice = ItemSpellChoiceCatalog.Read(new TwoDaService(scratch), _ => null)
                    .Should().ContainSingle().Which;
                choice.Value.Should().Be(0);
                choice.Display.Should().Be("Valid_Spell");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void Read_FailsClosedWhenRequiredSpellMetadataIsUnavailable()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"item-spell-metadata-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "iprp_spells.2da"),
                    "2DA V2.0\r\n\r\nLabel Name SpellIndex\r\n0 Incomplete_Table 100 42\r\n");

                ItemSpellChoiceCatalog.Read(new TwoDaService(scratch), _ => null)
                    .Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        // ----- IEditorPromptService test double -----

        private sealed class StubPromptService(bool answer) : IEditorPromptService
        {
            public int Calls { get; private set; }

            public List<string> Messages { get; } = new();

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel)
            {
                Calls++;
                Messages.Add(message);
                return Task.FromResult(answer);
            }

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
