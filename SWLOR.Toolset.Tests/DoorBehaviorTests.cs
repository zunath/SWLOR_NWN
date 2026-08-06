using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>The door behavior catalog, its owned values, and its editor-specific rules.</summary>
    [TestFixture]
    public class DoorBehaviorTests
    {
        [Test]
        public void EveryDoorBlueprintAndPlacementClassifiesWithoutThrowing()
        {
            var blueprints = CorpusBlueprints().ToList();
            var placements = CorpusPlacements().ToList();

            blueprints.Should().NotBeEmpty("the behavior catalog was derived from the door blueprints");
            placements.Should().NotBeEmpty("the placement path is the primary use of this editor");

            foreach (var door in blueprints.Concat(placements))
                DoorBehaviorCatalog.Classify(door).Should().NotBeNull();
        }

        [Test]
        public void TheClassifierUsesTheRequiredPrecedence()
        {
            var transition = NewDoor();
            Store(transition).SetString(
                BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "destination");
            DoorBehaviorCatalog.Classify(transition).Id.Should().Be(DoorBehaviorCatalog.AreaTransitionId);

            var locked = NewDoor();
            Store(locked).SetInteger(BehaviorFieldStorage.Field, "Locked", GffFieldType.Byte, 1);
            DoorBehaviorCatalog.Classify(locked).Id.Should().Be(DoorBehaviorCatalog.LockedDoorId);

            var keyItem = NewDoor();
            Store(keyItem).SetRequiredKeyItemIds(new[] { 1 });
            Store(keyItem).SetInteger(BehaviorFieldStorage.Field, "TrapFlag", GffFieldType.Byte, 1);
            DoorBehaviorCatalog.Classify(keyItem).Id.Should().Be(DoorBehaviorCatalog.KeyItemDoorId);

            var trapped = NewDoor();
            Store(trapped).SetInteger(BehaviorFieldStorage.Field, "TrapFlag", GffFieldType.Byte, 1);
            Store(trapped).SetString(
                BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "destination");
            DoorBehaviorCatalog.Classify(trapped).Id.Should().Be(DoorBehaviorCatalog.TrappedDoorId);

            var sealedDoor = NewDoor();
            Store(sealedDoor).SetInteger(BehaviorFieldStorage.Field, "Plot", GffFieldType.Byte, 1);
            DoorBehaviorCatalog.Classify(sealedDoor).Id.Should().Be(DoorBehaviorCatalog.SealedDoorId);

            DoorBehaviorCatalog.Classify(NewDoor()).Id.Should().Be(DoorBehaviorCatalog.CustomId);

            var conversation = NewDoor();
            Store(conversation).SetString(
                BehaviorFieldStorage.Field, "Conversation", GffFieldType.ResRef, "door_dialog");
            DoorBehaviorCatalog.Classify(conversation).Id.Should().Be(DoorBehaviorCatalog.CustomId);

            var scripted = NewDoor();
            Store(scripted).SetString(
                BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, "unknown_open");
            DoorBehaviorCatalog.Classify(scripted).Id.Should().Be(DoorBehaviorCatalog.CustomId);
        }

        [TestCase("OnDeath", DoorBehaviorCatalog.DefaultDeathScript)]
        [TestCase("OnOpen", "dt_refermeporte")]
        [TestCase("OnOpen", "pug_closedoor8s")]
        [TestCase("OnOpen", "gy_2minlockclose")]
        [TestCase("OnOpen", "gy_2minclosedoor")]
        [TestCase("OnOpen", "relock")]
        public void EngineDeathAndKnownCloserScriptsDoNotOverrideARecognizedBehavior(
            string field,
            string script)
        {
            var door = NewDoor();
            Store(door).SetString(BehaviorFieldStorage.Field, field, GffFieldType.ResRef, script);
            Store(door).SetInteger(BehaviorFieldStorage.Field, "Plot", GffFieldType.Byte, 1);

            DoorBehaviorCatalog.Classify(door).Id.Should().Be(DoorBehaviorCatalog.SealedDoorId);
        }

        [Test]
        public void TheSelfClosingFlagUsesAndPreservesTheKnownScripts()
        {
            var blank = Store(NewDoor());
            blank.SetSelfClosing(true);
            blank.GetString(BehaviorFieldStorage.Field, "OnOpen")
                .Should().Be(DoorValueStore.DefaultCloser);

            var existing = Store(NewDoor());
            existing.SetString(
                BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, "pug_closedoor8s");
            existing.SetSelfClosing(true);
            existing.GetString(BehaviorFieldStorage.Field, "OnOpen")
                .Should().Be("pug_closedoor8s");

            existing.SetSelfClosing(false);
            existing.GetString(BehaviorFieldStorage.Field, "OnOpen").Should().BeEmpty();

            var custom = Store(NewDoor());
            custom.SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, "custom_open");
            custom.SetSelfClosing(false);
            custom.GetString(BehaviorFieldStorage.Field, "OnOpen").Should().Be("custom_open");
        }

        [Test]
        public void EveryBehaviorWritesTheFieldsItsRuntimeReads()
        {
            DoorBehaviorCatalog.All.Select(behavior => behavior.Id).Should().BeEquivalentTo(new[]
            {
                DoorBehaviorCatalog.AreaTransitionId,
                DoorBehaviorCatalog.LockedDoorId,
                DoorBehaviorCatalog.KeyItemDoorId,
                DoorBehaviorCatalog.SealedDoorId,
                DoorBehaviorCatalog.TrappedDoorId,
                DoorBehaviorCatalog.CustomId
            });

            var transition = Store(NewDoor());
            transition.Apply(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.AreaTransitionId), isInstance: true);
            transition.GetInteger(BehaviorFieldStorage.Field, "KeyRequired").Should().Be(0);

            var locked = Store(NewDoor());
            locked.SetString(BehaviorFieldStorage.Field, "KeyName", GffFieldType.CExoString, "door_key");
            locked.Apply(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.LockedDoorId), isInstance: true);
            locked.GetInteger(BehaviorFieldStorage.Field, "Locked").Should().Be(1);
            locked.GetInteger(BehaviorFieldStorage.Field, "KeyRequired").Should().Be(1);

            var keyItem = Store(NewDoor());
            keyItem.Apply(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.KeyItemDoorId), isInstance: true);
            keyItem.GetInteger(BehaviorFieldStorage.Field, "Locked").Should().Be(1);
            keyItem.Locals.GetString("CONVERSATION")
                .Should().Be(DoorBehaviorCatalog.LockedDoorConversation);

            var sealedDoor = Store(NewDoor());
            sealedDoor.Apply(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.SealedDoorId), isInstance: true);
            sealedDoor.GetInteger(BehaviorFieldStorage.Field, "Plot").Should().Be(1);
            sealedDoor.GetInteger(BehaviorFieldStorage.Field, "Locked").Should().Be(0);
            sealedDoor.GetInteger(BehaviorFieldStorage.Field, "KeyRequired").Should().Be(0);
            sealedDoor.GetString(BehaviorFieldStorage.Field, "LinkedTo").Should().BeEmpty();

            var trapped = Store(NewDoor());
            trapped.Apply(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.TrappedDoorId), isInstance: true);
            trapped.GetInteger(BehaviorFieldStorage.Field, "TrapFlag").Should().Be(1);

            var custom = Store(NewDoor());
            custom.Apply(DoorBehaviorCatalog.Custom, isInstance: true);
            custom.Door.TryGet("Plot", out _).Should().BeFalse();
            custom.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("test_door");
        }

        [Test]
        public void KeyItemDoorExplainsItsDestinationWaypointField()
        {
            var destination = DoorBehaviorCatalog.Get(DoorBehaviorCatalog.KeyItemDoorId)
                .Fields.Single(field => field.Name == "LOCKED_DOOR_INSIDE_WP");

            destination.Label.Should().Be("Destination waypoint tag");
            destination.Note.Should().Be(
                "After the key-item check succeeds, the player and henchman move to this waypoint.");
            destination.TagScope.Should().Be(BehaviorTagScope.Waypoint);
            destination.IsRequired.Should().BeTrue();
        }

        [Test]
        public void SwappingAwayFromKeyItemDoorClearsOwnedFieldsAndRenumbersLocals()
        {
            var door = NewDoor();
            var store = Store(door);
            store.SetRequiredKeyItemIds(new[] { 7, 11, 15 });
            store.SetRequiredKeyItemIds(new[] { 7, 15 });
            store.Locals.GetInt("REQUIRED_KEY_ITEM_ID_1").Should().Be(7);
            store.Locals.GetInt("REQUIRED_KEY_ITEM_ID_2").Should().Be(15);
            store.Locals.GetInt("REQUIRED_KEY_ITEM_ID_3").Should().BeNull();
            store.SetString(
                BehaviorFieldStorage.Local, "DOOR_DIALOGUE", GffFieldType.CExoString, "Show papers.");
            store.SetString(
                BehaviorFieldStorage.Local, "LOCKED_DOOR_INSIDE_WP", GffFieldType.CExoString, "inside");
            store.Apply(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.KeyItemDoorId), isInstance: true);
            store.SetString(
                BehaviorFieldStorage.Field, "TemplateResRef", GffFieldType.ResRef, "door_template");

            var edits = 0;
            var editor = new DoorEditorViewModel(
                door,
                "test_area",
                isInstance: true,
                (_, mutation) =>
                {
                    edits++;
                    mutation();
                    return true;
                });
            editor.ChooseBehavior(DoorBehaviorCatalog.Custom);

            edits.Should().Be(1, "a behavior swap is one undoable edit");
            store.HasRequiredKeyItemLocals.Should().BeFalse();
            store.Locals.GetString("DOOR_DIALOGUE").Should().BeNull();
            store.Locals.GetString("LOCKED_DOOR_INSIDE_WP").Should().BeNull();
            store.Locals.GetString("CONVERSATION").Should().BeNull();
            store.GetInteger(BehaviorFieldStorage.Field, "Locked").Should().Be(0);
            store.GetString(BehaviorFieldStorage.Field, "TemplateResRef")
                .Should().Be("door_template");
            DoorBehaviorCatalog.Classify(door).Id.Should().Be(DoorBehaviorCatalog.CustomId);
        }

        [Test]
        public void SwappingAwayFromCustomClearsRawBehaviorValuesButKeepsIdentity()
        {
            var door = NewDoor();
            var store = Store(door);
            store.SetString(
                BehaviorFieldStorage.Field, "TemplateResRef", GffFieldType.ResRef, "door_template");
            store.SetString(
                BehaviorFieldStorage.Field, "Conversation", GffFieldType.ResRef, "custom_dialog");
            store.SetString(
                BehaviorFieldStorage.Field, "OnClick", GffFieldType.ResRef, "custom_click");
            store.SetInteger(BehaviorFieldStorage.Field, "TrapFlag", GffFieldType.Byte, 1);
            store.SetString(
                BehaviorFieldStorage.Local, "CUSTOM_VALUE", GffFieldType.CExoString, "leftover");

            var editor = Editor(door, isInstance: false);
            editor.Behavior.Id.Should().Be(DoorBehaviorCatalog.TrappedDoorId,
                "trap classification precedes Custom");
            editor.ChooseBehavior(DoorBehaviorCatalog.Custom);
            editor.ChooseBehavior(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.SealedDoorId));

            store.GetString(BehaviorFieldStorage.Field, "Conversation").Should().BeEmpty();
            store.GetString(BehaviorFieldStorage.Field, "OnClick").Should().BeEmpty();
            store.GetInteger(BehaviorFieldStorage.Field, "TrapFlag").Should().Be(0);
            store.Locals.Any().Should().BeFalse();
            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("test_door");
            store.GetString(BehaviorFieldStorage.Field, "TemplateResRef").Should().Be("door_template");
            DoorBehaviorCatalog.Classify(door).Id.Should().Be(DoorBehaviorCatalog.SealedDoorId);
        }

        /// <summary>
        /// ResRef is editable because rename-on-save owns the blueprint file, category membership,
        /// and every placed instance as one transaction.
        /// </summary>
        [Test]
        public void TheResRefRowSupportsSafeRenameOnSave()
        {
            var resRef = DoorEditorLayout.Basic.Single(row => row.Name == "TemplateResRef");
            resRef.Label.Should().Be("ResRef");
            resRef.IsReadOnly.Should().BeFalse();
            resRef.IsRequired.Should().BeTrue();
        }

        [Test]
        public void VariablesExistOnlyUnderCustom()
        {
            DoorBehaviorCatalog.All.Where(behavior => behavior.AllowsVariables)
                .Should().ContainSingle()
                .Which.Id.Should().Be(DoorBehaviorCatalog.CustomId);

            var door = NewDoor();
            Store(door).SetInteger(BehaviorFieldStorage.Field, "Plot", GffFieldType.Byte, 1);
            var editor = Editor(door, isInstance: false);
            editor.ShowsVariablesTab.Should().BeFalse();
            editor.Variables.Should().BeNull();

            editor.ChooseBehavior(DoorBehaviorCatalog.Custom);
            editor.ShowsVariablesTab.Should().BeTrue();
            editor.Variables.Should().NotBeNull();
        }

        [Test]
        public void PlacementHeadersNameTheBehaviorAndOwningArea()
        {
            var door = NewDoor();
            Store(door).SetString(
                BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "next_area");

            var editor = Editor(door, isInstance: true, owner: "tat_mos_eisley");

            editor.HeaderName.Should().Be("Area Transition");
            editor.HeaderKind.Should().Be("instance");
            editor.HeaderOwner.Should().Be("tat_mos_eisley");
        }

        [Test]
        public void SelectingAnAreaDoorUsesTheBehaviorEditorAndKeepsTransformEditingOutsideIt()
        {
            var gitPath = Path.Combine(CorpusLocator.ModuleDirectory, "git", "area.git.json");
            var gicPath = Path.Combine(CorpusLocator.ModuleDirectory, "gic", "area.gic.json");
            if (!File.Exists(gitPath) || !File.Exists(gicPath))
                Assert.Ignore("The area door fixture is not present in this checkout.");

            using var gitSession = new DocumentSession(gitPath, JsonGffDocument.Load(gitPath));
            using var gicSession = new DocumentSession(gicPath, JsonGffDocument.Load(gicPath));
            using var section = new InstanceListSectionViewModel(
                "Doors",
                "Door List",
                ResourceType.Utd,
                gitSession,
                gicSession,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                (description, mutation) =>
                {
                    using (gitSession.Begin(description))
                        mutation();
                    return true;
                },
                new GameCodeIndex(null),
                new OutputLogService(),
                new StubPrompts(),
                doorEditorServices: new DoorEditorServices(
                    "area",
                    null,
                    null,
                    Array.Empty<DoorAppearanceChoice>(),
                    null,
                    null));

            section.SelectedRow = section.Rows.Single();

            section.UsesDoorEditor.Should().BeTrue();
            section.UsesGenericDetailEditor.Should().BeFalse();
            section.DoorEditor.Should().NotBeNull();
            section.DoorEditor!.HeaderKind.Should().Be("instance");
            section.DoorEditor.HeaderOwner.Should().Be("area");
            section.VarTableSection.Should().BeNull(
                "door locals live only in the behavior editor's Custom Variables tab");

            var originalX = section.DetailX;
            section.DoorEditor.BasicRows.Single(row => row.Definition.Name == "Tag").Text = "edited_tag";
            section.SelectedRow.Tag.Should().Be("edited_tag");
            section.DetailX.Should().Be(originalX,
                "position remains owned by the area detail controls");
        }

        [Test]
        public void TransitionRowsResolveTagsAndWarnWhenTheDestinationTypeIsUnset()
        {
            var door = NewDoor();
            var store = Store(door);
            store.SetString(
                BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "missing_exit");
            store.SetInteger(BehaviorFieldStorage.Field, "LinkedToFlags", GffFieldType.Byte, 0);

            var editor = Editor(
                door,
                isInstance: true,
                resolveTag: (_, _) => null);
            var destination = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");
            var destinationType = editor.BehaviorRows.Single(row =>
                row.Definition.Name == "LinkedToFlags");

            destinationType.Choice.Should().BeNull(
                "an unset runtime value must not be displayed as the first real destination type");
            destination.IsStatusGood.Should().BeFalse();
            destination.Status.Should().Contain("destination type is unset");
            destination.Status.Should().Contain("will do nothing");
        }

        [TestCase(1, BehaviorTagScope.Door, BehaviorTagScope.Waypoint, "no door")]
        [TestCase(2, BehaviorTagScope.Waypoint, BehaviorTagScope.Door, "no waypoint")]
        public void TransitionRowsResolveOnlyAgainstTheSelectedDestinationKind(
            int linkedToFlags,
            BehaviorTagScope selectedScope,
            BehaviorTagScope oppositeScope,
            string expectedWarning)
        {
            var door = NewDoor();
            var store = Store(door);
            store.SetString(
                BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "shared_tag");
            store.SetInteger(
                BehaviorFieldStorage.Field, "LinkedToFlags", GffFieldType.Byte, linkedToFlags);
            var requestedScopes = new List<BehaviorTagScope>();

            var editor = Editor(
                door,
                isInstance: true,
                resolveTag: (scope, _) =>
                {
                    requestedScopes.Add(scope);
                    return scope == oppositeScope ? "opposite-kind destination" : null;
                });
            var destination = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");

            requestedScopes.Should().Equal(selectedScope);
            destination.IsStatusGood.Should().BeFalse();
            destination.Status.Should().Contain(expectedWarning);
        }

        [Test]
        public void LockedAndKeyItemRowsWarnAboutUnresolvableValues()
        {
            var lockedDoor = NewDoor();
            var lockedStore = Store(lockedDoor);
            lockedStore.SetInteger(BehaviorFieldStorage.Field, "Locked", GffFieldType.Byte, 1);
            lockedStore.SetInteger(BehaviorFieldStorage.Field, "KeyRequired", GffFieldType.Byte, 1);
            var lockedEditor = Editor(
                lockedDoor,
                isInstance: false,
                resolveTag: (_, _) => null);
            lockedEditor.BehaviorRows.Single(row => row.Definition.Name == "KeyName")
                .Status.Should().Contain("no item tag is set");

            var keyItemDoor = NewDoor();
            Store(keyItemDoor).SetRequiredKeyItemIds(new[] { 0 });
            Store(keyItemDoor).SetString(
                BehaviorFieldStorage.Local,
                "LOCKED_DOOR_INSIDE_WP",
                GffFieldType.CExoString,
                "WP_TAG");
            var keyItemEditor = new DoorEditorViewModel(
                keyItemDoor,
                "test",
                isInstance: false,
                Run,
                new GameCodeIndex(null),
                (_, _) => null);

            keyItemEditor.BehaviorRows.Single(row => row.IsMultiChoice)
                .Status.Should().Contain("invalid KeyItemType value");
            keyItemEditor.BehaviorRows.Single(row =>
                    row.Definition.Name == "LOCKED_DOOR_INSIDE_WP")
                .Status.Should().Contain("no waypoint");
        }

        [Test]
        public void ItemTagsResolveAgainstModuleItemBlueprints()
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var expected = workspace.EnumerateResRefs(ResourceType.Uti)
                .Select(resRef => new
                {
                    ResRef = resRef,
                    Tag = Store(JsonGffDocument.Load(
                            workspace.GetResourcePath(ResourceType.Uti, resRef)).Root)
                        .GetString(BehaviorFieldStorage.Field, "Tag")
                })
                .First(item => !string.IsNullOrWhiteSpace(item.Tag));

            workspace.TagIndex.FindItemBlueprintDefiningTag(expected.Tag)
                .Should().Be(expected.ResRef);
        }

        [Test]
        public void AppearancePickerWritesExactlyOneOfTheTwoDoorAppearanceFields()
        {
            var store = Store(NewDoor());

            store.SetAppearance(new DoorAppearanceChoice(
                DoorAppearanceKind.Generic, 12, "Generic", "model_generic"));
            store.GetInteger(BehaviorFieldStorage.Field, "GenericType_New").Should().Be(12);
            store.GetInteger(BehaviorFieldStorage.Field, "Appearance").Should().Be(0);

            store.SetAppearance(new DoorAppearanceChoice(
                DoorAppearanceKind.Specific, 27, "Specific", "model_specific"));
            store.GetInteger(BehaviorFieldStorage.Field, "Appearance").Should().Be(27);
            store.GetInteger(BehaviorFieldStorage.Field, "GenericType_New").Should().Be(0);
        }

        [Test]
        public void DescriptionPreservesLineBreaks()
        {
            var door = NewDoor();
            var editor = Editor(door, isInstance: false);
            var description = editor.BasicRows.Single(row => row.Definition.Name == "Description");

            description.Text = "First paragraph.\n\nSecond paragraph.";

            Store(door).GetLocalizedText("Description")
                .Should().Be("First paragraph.\n\nSecond paragraph.");
        }

        [Test]
        public async Task ASmallPictureSetIsShownOnThePageAndPaged()
        {
            using var row = PictureRow(60);
            await row.ActivateChoicesAsync();

            row.IsGallery.Should().BeTrue();
            row.IsInlineGallery.Should().BeTrue("a set this size fits on the page");
            row.IsPopupGallery.Should().BeFalse();

            // On the page, but still paged: the tiles past the first page are not realized and their
            // pictures are not requested until the builder scrolls to them.
            row.GalleryChoices.Should().HaveCount(48);
            row.LoadMoreGalleryCommand.Execute(null);
            row.GalleryChoices.Should().HaveCount(60);
        }

        [Test]
        public async Task ALargePictureSetStaysBehindItsPreviewUntilOpened()
        {
            using var row = PictureRow(400);

            row.IsInlineGallery.Should().BeFalse("four hundred tiles is not a page's worth of row");
            row.IsPopupGallery.Should().BeTrue();
            row.GalleryChoices.Should().BeEmpty("opening the editor must not realize portrait tiles");

            await row.OpenGalleryCommand.ExecuteAsync(null);
            row.IsGalleryOpen.Should().BeTrue();
            row.GalleryChoices.Should().HaveCount(48);

            // Picking closes it: a picker left open after the choice makes you dismiss it yourself
            // to see what you did.
            row.PickChoiceCommand.Execute(row.GalleryChoices[3]);
            row.IsGalleryOpen.Should().BeFalse();
            row.Choice!.Display.Should().Be("Portrait 3");
            row.GalleryChoices[3].IsSelected.Should().BeTrue();
        }

        private static DoorRowViewModel PictureRow(int count) =>
            new(
                new DoorFieldDefinition
                {
                    Label = "Portrait",
                    Name = "PortraitId",
                    Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Word
                },
                Store(NewDoor()),
                Run,
                null,
                _ => { },
                _ => { },
                Enumerable.Range(0, count)
                    .Select(index => new BehaviorChoice(index, $"Portrait {index}", $"portrait_{index}"))
                    .ToList());

        [Test]
        public void TheLayoutKeepsEngineNoiseOutAndRawBehaviorFlagsUnderCustom()
        {
            DoorEditorLayout.Basic.Any(row => row.Name == "Comment" ||
                                              row.Name == "Interruptable" ||
                                              row.Name == "GenericType")
                .Should().BeFalse();
            DoorEditorLayout.Basic.Should().ContainSingle(row =>
                row.Special == DoorFieldSpecial.SelfClosing);
            DoorEditorLayout.Basic.Should().NotContain(row =>
                row.Special == DoorFieldSpecial.Appearance);
            DoorEditorLayout.Basic.Should().Contain(row => row.Name == "PortraitId");
            DoorEditorLayout.Basic.Should().Contain(row => row.Name == "AnimationState");
            DoorEditorLayout.Basic.Should().Contain(row =>
                row.Name == "Description" && row.Kind == BehaviorFieldKind.Paragraph);

            var raw = DoorBehaviorCatalog.Custom.Fields
                .Select(row => row.Name)
                .ToHashSet(StringComparer.Ordinal);
            raw.Should().Contain(new[]
            {
                "Conversation",
                "Plot", "Locked", "KeyRequired", "LinkedTo", "LinkedToFlags",
                "TrapFlag", "TrapType", "TrapDetectable", "TrapDetectDC",
                "TrapDisarmable", "DisarmDC", "TrapOneShot"
            });
        }

        [Test]
        public void TheCorpusReaderUsesDoorListWithItsSpace()
        {
            var path = Directory.EnumerateFiles(
                    Path.Combine(CorpusLocator.ModuleDirectory, "git"),
                    "*.git.json")
                .First(candidate =>
                    new GitDocument(JsonGffDocument.Load(candidate)).Doors.Count > 0);

            using var raw = JsonDocument.Parse(File.ReadAllText(path));
            var expected = raw.RootElement.GetProperty("Door List").GetProperty("value").GetArrayLength();

            new GitDocument(JsonGffDocument.Load(path)).Doors.Count.Should().Be(expected);
        }

        private static DoorEditorViewModel Editor(
            JsonGffStruct door,
            bool isInstance,
            string owner = "test",
            Func<BehaviorTagScope, string, string?>? resolveTag = null) =>
            new(door, owner, isInstance, Run, resolveTag: resolveTag);

        private static bool Run(string _, Action mutation)
        {
            mutation();
            return true;
        }

        private static DoorValueStore Store(JsonGffStruct door) => new(door);

        private static JsonGffStruct NewDoor()
        {
            return JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                "{\n  \"__data_type\": \"UTD \",\n" +
                "  \"Tag\": { \"type\": \"cexostring\", \"value\": \"test_door\" }\n}\n")).Root;
        }

        private static IEnumerable<JsonGffStruct> CorpusBlueprints()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "utd");
            if (!Directory.Exists(directory))
                Assert.Ignore("The module door blueprints are not present in this checkout.");

            foreach (var path in Directory.EnumerateFiles(directory, "*.utd.json"))
                yield return JsonGffDocument.Load(path).Root;
        }

        private static IEnumerable<JsonGffStruct> CorpusPlacements()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "git");
            if (!Directory.Exists(directory))
                Assert.Ignore("The module area corpus is not present in this checkout.");

            foreach (var path in Directory.EnumerateFiles(directory, "*.git.json"))
            {
                foreach (var door in new GitDocument(JsonGffDocument.Load(path)).Doors)
                    yield return door;
            }
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
