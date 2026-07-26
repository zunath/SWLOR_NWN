using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Placeables;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Covers the behavior layer: that it recognises what the module already stores, that switching
    /// writes and clears exactly what it claims to, and that none of it disturbs a file's bytes.
    /// </summary>
    public class PlaceableBehaviorTests
    {
        private static string UtpDirectory => Path.Combine(CorpusLocator.ModuleDirectory, "utp");

        [Test]
        public void Catalog_HasNoDuplicateIdsAndDeclaresBothSentinels()
        {
            var behaviors = PlaceableBehaviorCatalog.Behaviors;

            behaviors.Select(behavior => behavior.Id).Should().OnlyHaveUniqueItems();
            PlaceableBehaviorCatalog.None.IsSentinel.Should().BeTrue();
            PlaceableBehaviorCatalog.None.Name.Should().Be("Decor");
            PlaceableBehaviorCatalog.Custom.AllowsRawEditing.Should().BeTrue();
            behaviors.Where(behavior => !behavior.IsSentinel)
                .Should().OnlyContain(behavior => behavior.Scripts.Count > 0 || behavior.Fields.Count > 0,
                    "a named behavior has to be recognisable from something stored");
        }

        [Test]
        public void BehaviorSelection_GivesCustomItsOwnSection()
        {
            var section = BuildBehaviorSection();
            var customIndex = section.Items
                .Select((item, index) => (item, index))
                .Single(entry => entry.item.Behavior?.Id == PlaceableBehaviorCatalog.CustomId)
                .index;

            customIndex.Should().BeGreaterThan(0);
            section.Items[customIndex - 1].IsHeader.Should().BeTrue();
            section.Items[customIndex - 1].Text.Should().Be("Custom");
        }

        [Test]
        public void Catalog_DecorOffersStaticAndEveryUsableBehaviorIsDynamic()
        {
            PlaceableBehaviorCatalog.None.EditableFlags.Should().ContainSingle()
                .Which.Should().Be(new PlaceableBehaviorEditableFlag(
                    "Static",
                    "Static",
                    "Treat this decor as part of the area geometry instead of an interactive object."));

            PlaceableBehaviorCatalog.Behaviors
                .Where(behavior => behavior.Flags.Any(flag =>
                    flag.FieldName == "Useable" && flag.Value))
                .Should().OnlyContain(behavior => behavior.Flags.Any(flag =>
                        flag.FieldName == "Static" && !flag.Value),
                    "usable placeables must be non-static for interaction events to work");
        }

        [Test]
        public void PlaceableSchema_ShowsRawFlagsOnlyOnCustomAndMakesDescriptionMultiline()
        {
            var schema = UtpSchema.Build();
            var schemaFields = schema.AllFields.ToList();
            var customFlagNames = UtpSchema.CustomBehaviorFlagFields
                .Select(field => field.FieldName)
                .ToList();

            schemaFields.Should().NotContain(field => customFlagNames.Contains(field.FieldName),
                "raw flags belong to Custom on the Behavior tab");
            customFlagNames.Should().BeEquivalentTo("Useable", "HasInventory", "Static", "Plot");

            schemaFields.Single(field => field.FieldName == "Description")
                .IsMultiline.Should().BeTrue();
            schemaFields.Single(field => field.FieldName == "LocName")
                .IsMultiline.Should().BeFalse();

            UtpSchema.CustomBehaviorScriptFields
                .Select(field => field.FieldName)
                .Should().BeEquivalentTo(PlaceableBehaviorDetector.ScriptSlots);
        }

        [Test]
        public void Catalog_ConstrainsEngineTiersAndUsesPreviewPickersAndRuntimeDefaults()
        {
            Field("scavenge_point", "SCAVENGE_POINT_LEVEL").Maximum.Should().Be(5);
            Field("harvest_node", "HARVESTER_REQUIRED_LEVEL").Maximum.Should().Be(5);
            Field("asteroid", "ASTEROID_TIER").Maximum.Should().Be(5);
            Field("slicing_terminal", "SLICING_TIER").Maximum.Should().Be(5);

            var integrity = Field("slicing_terminal", "SLICING_INTEGRITY");
            integrity.Minimum.Should().Be(1);
            integrity.Maximum.Should().Be(100);
            integrity.DefaultIntValue.Should().Be(100);
            integrity.Description.Should().Contain("Failed slicing attempts");

            Field("harvest_node", "RESOURCE_COUNT").Label.Should().Be("Charges");
            Field("resource_node", "RESOURCE_SPAWN_COUNT").Label.Should().Be("Charges");
            Field("harvest_node", "RESOURCE_COUNT").DefaultIntValue.Should().Be(4);
            Field("resource_node", "RESOURCE_SPAWN_COUNT").DefaultIntValue.Should().Be(4);

            Field("resource_node", "RESOURCE_PROP").Source
                .Should().Be(PlaceableValueSource.PlaceableBlueprints);
            Field("quest_activator", "QUEST_ENCOUNTER_RESREF").Source
                .Should().Be(PlaceableValueSource.CreatureBlueprints);
            Field("teleporter", "VISUAL_EFFECT").Source
                .Should().Be(PlaceableValueSource.VisualEffects);
            Field("permanent_vfx", "PERMANENT_VFX_ID").Source
                .Should().Be(PlaceableValueSource.VisualEffects);

            var marketDialog = Field("market_terminal", "CONVERSATION");
            marketDialog.IsVisible.Should().BeFalse();
            marketDialog.DefaultStringValue.Should().Be("MarketDialog");
            Field("market_terminal", "MARKET_ID").Source.Should().Be(PlaceableValueSource.MarketRegions);
            Field("market_terminal", "MARKET_ID").DefaultIntValue.Should().Be(1);

            Field("quest_activator", "QUEST_ENCOUNTER_COOLDOWN_MINUTES").DefaultIntValue.Should().Be(60);
            Field("quest_activator", "QUEST_ENCOUNTER_IDLE_MINUTES").DefaultIntValue.Should().Be(10);
        }

        [Test]
        public void Apply_WritesSensibleDefaultsWithoutReplacingExistingValues()
        {
            var slicingDocument = BuildDocument();
            var slicing = PlaceableBehaviorCatalog.FindById("slicing_terminal")!;
            PlaceableBehaviorApplier.Apply(
                slicingDocument.Root,
                PlaceableBehaviorCatalog.None,
                slicing);

            var slicingVariables = new VarTable(slicingDocument.Root);
            slicingVariables.Single(entry => entry.Name == "SLICING_TIER").IntValue.Should().Be(1);
            slicingVariables.Single(entry => entry.Name == "SLICING_INTEGRITY").IntValue.Should().Be(100);

            var marketDocument = BuildDocument();
            var market = PlaceableBehaviorCatalog.FindById("market_terminal")!;
            var conversation = PlaceableBehaviorCatalog.FindById("conversation")!;
            new VarTable(marketDocument.Root).SetString("CONVERSATION", "SomeOtherDialog");
            PlaceableBehaviorApplier
                .ValuesLostBySwitching(marketDocument.Root, conversation, market)
                .Should().Contain("CONVERSATION");
            PlaceableBehaviorApplier.Apply(
                marketDocument.Root,
                conversation,
                market);

            var marketVariables = new VarTable(marketDocument.Root);
            marketVariables.Single(entry => entry.Name == "CONVERSATION").StringValue.Should().Be("MarketDialog");
            marketVariables.Single(entry => entry.Name == "MARKET_ID").IntValue.Should().Be(1);

            var questDocument = BuildDocument();
            var quest = PlaceableBehaviorCatalog.FindById("quest_activator")!;
            var questVariables = new VarTable(questDocument.Root);
            questVariables.SetInt("QUEST_ENCOUNTER_COOLDOWN_MINUTES", 15);
            PlaceableBehaviorApplier.Apply(
                questDocument.Root,
                PlaceableBehaviorCatalog.None,
                quest);

            questVariables.Single(entry => entry.Name == "QUEST_ENCOUNTER_COOLDOWN_MINUTES")
                .IntValue.Should().Be(15, "an authored value must win over the default");
            questVariables.Single(entry => entry.Name == "QUEST_ENCOUNTER_IDLE_MINUTES")
                .IntValue.Should().Be(10);
        }

        [Test]
        public void EnsureExpectedValues_MaterializesEveryDeclaredScriptFlagAndDefault()
        {
            foreach (var behavior in PlaceableBehaviorCatalog.Behaviors
                         .Where(candidate => !candidate.IsSentinel))
            {
                var document = BuildDocument();
                PlaceableBehaviorApplier.EnsureExpectedValues(document.Root, behavior);
                var saved = JsonGffDocument.Parse(document.ToBytes());

                foreach (var script in behavior.Scripts)
                    saved.Root.GetOrNull(script.Key)!.GetString().Should().Be(
                        script.Value,
                        $"{behavior.Name} owns the {script.Key} script");

                foreach (var flag in behavior.Flags)
                    saved.Root.GetOrNull(flag.FieldName)!.GetInteger().Should().Be(
                        flag.Value ? 1 : 0,
                        $"{behavior.Name} requires {flag.FieldName} = {flag.Value}");

                var variables = new VarTable(saved.Root);
                foreach (var field in behavior.Fields)
                {
                    if (field.DefaultIntValue is { } intValue)
                    {
                        variables.Single(entry => entry.Name == field.VariableName)
                            .IntValue.Should().Be(
                                intValue,
                                $"{behavior.Name} supplies the normal {field.Label} value");
                    }
                    else if (!string.IsNullOrWhiteSpace(field.DefaultStringValue))
                    {
                        variables.Single(entry => entry.Name == field.VariableName)
                            .StringValue.Should().Be(
                                field.DefaultStringValue,
                                $"{behavior.Name} supplies the normal {field.Label} value");
                    }
                }
            }
        }

        [Test]
        public void Decor_StaticIsEditableAndSurvivesSerialization()
        {
            var document = BuildDocument();
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            var section = new PlaceableBehaviorSectionViewModel(
                context,
                new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                new AcceptingPrompts(),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            section.Current.Should().BeSameAs(PlaceableBehaviorCatalog.None);
            section.HasEditableFlags.Should().BeTrue();
            section.HasSettings.Should().BeTrue();
            var staticField = section.EditableFlagFields.Should().ContainSingle().Subject;
            staticField.Label.Should().Be("Static");

            staticField.IsChecked = true;

            document.Root.GetOrNull("Static")!.GetInteger().Should().Be(1);
            JsonGffDocument.Parse(document.ToBytes()).Root
                .GetOrNull("Static")!.GetInteger().Should().Be(1);

            PlaceableBehaviorApplier.EnsureExpectedValues(
                document.Root,
                PlaceableBehaviorCatalog.None);
            document.Root.GetOrNull("Static")!.GetInteger().Should().Be(
                1,
                "Decor's Static value is a builder choice, not a behavior default");
        }

        [Test]
        public async Task BlueprintSave_CompletesNamedBehaviorWiring()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "swlor-placeable-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "behavior_save.utp.json");

            try
            {
                var modelCatalog = CreateFixturePlaceableModelCatalog(directory);
                _ = modelCatalog.GetAll();
                var initial = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utp,
                    "behavior_save",
                    "Behavior Save"));
                initial.Root.GetOrNull("OnUsed")!.SetString("zep_torch");
                initial.Root.GetOrNull("OnHeartbeat")!.SetString(string.Empty);
                initial.Root.GetOrNull("Useable")!.SetInteger(0);
                initial.Root.GetOrNull("Static")!.SetInteger(1);
                File.WriteAllBytes(path, initial.ToBytes());

                var log = new OutputLogService();
                var workspace = new WorkspaceContext(_ => throw new NotSupportedException(), log);
                var editor = new BlueprintEditorViewModel(
                    path,
                    "behavior_save",
                    ResourceType.Utp,
                    UtpSchema.Build(),
                    new LookupOptionProvider(workspace),
                    gameCodeIndex: null,
                    log,
                    new AcceptingPrompts(),
                    placeableSections: (context, runEdit, scriptHost) =>
                    {
                        var appearance = new AppearanceSectionViewModel(
                            context,
                            modelCatalog,
                            thumbnails: null,
                            () => PlaceableAppearanceUsageIndex.Empty,
                            runEdit);
                        var behavior = new PlaceableBehaviorSectionViewModel(
                            context,
                            new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                            new AcceptingPrompts(),
                            runEdit,
                            scriptHost);
                        return new PlaceableEditorSections(appearance, behavior);
                    });

                editor.PlaceableSections!.Behavior.Current.Id.Should().Be("light_torch");
                editor.Groups
                    .SelectMany(group => group.Fields)
                    .OfType<LocStringFieldViewModel>()
                    .Single(field => field.Descriptor.FieldName == "LocName")
                    .Text = "Saved Behavior";

                (await editor.TrySaveAsync()).Should().BeTrue();
                editor.IsDirty.Should().BeFalse();
                editor.OnClose().Should().BeTrue();

                var saved = JsonGffDocument.Load(path);
                saved.Root.GetOrNull("OnUsed")!.GetString().Should().Be("zep_torch");
                saved.Root.GetOrNull("OnHeartbeat")!.GetString().Should().Be("zep_torchspawn");
                saved.Root.GetOrNull("Useable")!.GetInteger().Should().Be(1);
                saved.Root.GetOrNull("Static")!.GetInteger().Should().Be(0);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void GameCodePickerData_UsesOnlyCraftMenuSkillsAndDocumentsEveryVfxGroup()
        {
            var sourceRoot = FindGameServerSource();
            if (sourceRoot == null)
                Assert.Ignore("SWLOR.Game.Server source not found from the test context.");

            var index = new GameCodeIndex(sourceRoot);
            index.SkillTypes.Keys.Should().BeEquivalentTo(new[] { 9, 10, 31, 32, 49 });
            index.MarketRegions.Should().ContainSingle(entry =>
                entry.Key == 1 && entry.Value == "Global");
            index.VisualEffectReferences.Values
                .Select(reference => reference.Group)
                .Distinct()
                .Should().BeEquivalentTo("BEAM", "COM", "DUR", "EYES", "FNF", "IMP");
            index.VisualEffectReferences.Should().HaveCountGreaterThan(500);
            index.VisualEffectReferences.Values
                .Where(reference => !string.IsNullOrWhiteSpace(reference.ImageUrl))
                .Should().OnlyContain(reference => Uri.IsWellFormedUriString(
                    reference.ImageUrl,
                    UriKind.Absolute));

            var sources = new BehaviorValueSourceProvider(index, tags: () => null);
            sources.GetOptions(PlaceableValueSource.SkillTypes)
                .Should().OnlyContain(option => option.Display == index.SkillTypes[int.Parse(option.Value)],
                    "skill names do not need their internal ids in the workbench list");
            sources.GetOptions(PlaceableValueSource.KeyItems)
                .Should().OnlyContain(option => option.Display == index.KeyItems[int.Parse(option.Value)]);
        }

        [Test]
        public void BehaviorView_UsesInlineInfiniteGalleriesAndSearchableKeyItems()
        {
            var viewPath = Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset",
                "Editors",
                "Views",
                "BlueprintEditorView.axaml");
            var view = File.ReadAllText(viewPath);

            view.Should().NotContain("WHAT THIS BEHAVIOR MANAGES");
            view.Should().Contain("ItemsSource=\"{Binding CustomScriptFields}\"");
            view.Should().Contain("ItemsSource=\"{Binding EditableFlagFields}\"");

            var appPath = Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset",
                "App.axaml");
            var app = File.ReadAllText(appPath);
            var behaviorTemplate = app[
                app.IndexOf("<DataTemplate DataType=\"placeables:BehaviorFieldViewModel\">",
                    StringComparison.Ordinal)..app.IndexOf("<local:ViewLocator", StringComparison.Ordinal)];

            behaviorTemplate.Should().Contain("IsSearchableIdChoice");
            behaviorTemplate.Should().Contain("Watermark=\"Search key items\"");
            behaviorTemplate.Should().Contain("ScrollChanged=\"OnBehaviorGalleryScrollChanged\"");
            behaviorTemplate.Should().NotContain("<Popup");
            behaviorTemplate.Should().NotContain("Load more");
            behaviorTemplate.Should().NotContain("OpenGalleryCommand");
        }

        [Test]
        public void BehaviorFields_ClampNumbersAndGalleryChoicesWriteAndClearResrefs()
        {
            var document = BuildDocument();
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            var number = new BehaviorFieldViewModel(
                new PlaceableBehaviorField
                {
                    VariableName = "LEVEL",
                    Label = "Level",
                    Kind = PlaceableFieldKind.Integer,
                    Minimum = 1,
                    Maximum = 5
                },
                context,
                new BehaviorValueSourceProvider(gameCode: null, tags: () => null));
            number.Number = 12;
            number.Number.Should().Be(5);
            new VarTable(document.Root).Single(entry => entry.Name == "LEVEL").IntValue.Should().Be(5);

            var gallerySource = new BehaviorValueSourceProvider(
                gameCode: null,
                tags: () => null,
                blueprints: type => type == Domain.Workspace.ResourceType.Utp
                    ? new[]
                    {
                        new Domain.Workspace.CatalogEntry(
                            type,
                            "visible_prop",
                            "Visible Prop",
                            null,
                            "visible_prop.utp.json")
                    }
                    : Array.Empty<Domain.Workspace.CatalogEntry>());
            var gallery = new BehaviorFieldViewModel(
                new PlaceableBehaviorField
                {
                    VariableName = "RESOURCE_PROP",
                    Label = "Visible placeable",
                    Kind = PlaceableFieldKind.Choice,
                    Source = PlaceableValueSource.PlaceableBlueprints
                },
                context,
                gallerySource);

            gallery.IsGalleryChoice.Should().BeTrue();
            gallery.GalleryTiles.Should().ContainSingle();
            gallery.PickChoiceCommand.Execute(gallery.GalleryTiles.Single());
            new VarTable(document.Root).Single(entry => entry.Name == "RESOURCE_PROP")
                .StringValue.Should().Be("visible_prop");

            gallery.CanClearChoice.Should().BeTrue();
            gallery.ClearChoiceCommand.Execute(null);
            new VarTable(document.Root).Should().NotContain(entry => entry.Name == "RESOURCE_PROP");
        }

        [Test]
        public void KeyItemField_IsSearchableAndWritesOnlyASelectedId()
        {
            var sourceRoot = FindGameServerSource();
            if (sourceRoot == null)
                Assert.Ignore("SWLOR.Game.Server source not found from the test context.");

            var document = BuildDocument();
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            var sources = new BehaviorValueSourceProvider(
                new GameCodeIndex(sourceRoot),
                tags: () => null);
            var keyItem = new BehaviorFieldViewModel(
                Field("slicing_terminal", "KEY_ITEM_ID"),
                context,
                sources);

            keyItem.IsSearchableIdChoice.Should().BeTrue();
            keyItem.IsIdChoice.Should().BeFalse();
            keyItem.Options.Should().NotBeEmpty();

            var selected = keyItem.Options.First();
            keyItem.SelectedOption = selected;

            keyItem.ChoiceSearchText.Should().Be(selected.Display);
            new VarTable(document.Root).Single(entry => entry.Name == "KEY_ITEM_ID")
                .IntValue.Should().Be(int.Parse(selected.Value));
        }

        [Test]
        public void Catalog_EveryRequiredFieldIsNamedByRealGameCode()
        {
            // Guards the half of a behavior that cannot be checked against the module: a variable
            // the game code no longer reads would leave the field silently inert.
            var sourceRoot = FindGameServerSource();
            if (sourceRoot == null)
                Assert.Ignore("SWLOR.Game.Server source not found from the test context.");

            var source = string.Join('\n', Directory
                .EnumerateFiles(sourceRoot!, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

            var missing = PlaceableBehaviorCatalog.Behaviors
                .SelectMany(behavior => behavior.Fields.Where(field => field.IsRequired)
                    .Select(field => (behavior.Name, field.VariableName)))
                .Where(entry => !source.Contains($"\"{entry.VariableName}\"", StringComparison.Ordinal))
                .ToList();

            missing.Should().BeEmpty("every required behavior variable must be read by the game code");
        }

        [Test]
        public void Detect_ReadsScavengePointFromItsScriptsAndVariables()
        {
            var root = LoadFirstPlaceableWith("SCAVENGE_POINT_LOOT_TABLE_NAME");
            if (root == null)
                Assert.Ignore("No scavenge point blueprint in the corpus.");

            PlaceableBehaviorDetector.Detect(root!).Id.Should().Be("scavenge_point");
        }

        [Test]
        public void Detect_RecognisesABaseGameChairAsAChair()
        {
            // 2,181 instances still run zep_use_chair and 1,550 run x0_o2_use_chair. Neither is the
            // script SWLOR writes, and both are unmistakably chairs.
            var root = BuildPlaceable(("OnUsed", "zep_use_chair"));

            PlaceableBehaviorDetector.Detect(root).Id.Should().Be("chair");
        }

        [Test]
        public void Detect_PlainDecorIsNoneAndUnknownWiringIsCustom()
        {
            PlaceableBehaviorDetector.Detect(BuildPlaceable()).Id
                .Should().Be(PlaceableBehaviorCatalog.NoneId);

            PlaceableBehaviorDetector.Detect(BuildPlaceable(("OnUsed", "some_unknown_script"))).Id
                .Should().Be(PlaceableBehaviorCatalog.CustomId);
        }

        [Test]
        public void Detect_ToleratesAnExtraScriptTheBehaviorDoesNotOwn()
        {
            // A quarter of the module's scavenge points also run plc_death. Calling those Custom
            // would bury their loot table in a raw grid for no gain.
            var root = BuildPlaceable(
                ("OnOpen", "scav_opened"),
                ("OnClosed", "scav_closed"),
                ("OnInvDisturbed", "scav_disturbed"),
                ("OnDeath", "plc_death"));

            PlaceableBehaviorDetector.Detect(root).Id.Should().Be("scavenge_point");
        }

        [Test]
        public void Apply_WritesScriptsAndFlagsAndClearsWhatTheOldBehaviorOwned()
        {
            var document = BuildDocument();
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            PlaceableBehaviorApplier.Apply(document.Root, PlaceableBehaviorCatalog.None, scavenge);

            document.Root.GetOrNull("OnOpen")!.GetString().Should().Be("scav_opened");
            document.Root.GetOrNull("HasInventory")!.GetInteger().Should().Be(1);
            document.Root.GetOrNull("Useable")!.GetInteger().Should().Be(1);

            new VarTable(document.Root).SetString("SCAVENGE_POINT_LOOT_TABLE_NAME", "SOME_TABLE");

            PlaceableBehaviorApplier.Apply(document.Root, scavenge, teleporter);

            document.Root.GetOrNull("OnOpen")!.GetString().Should().BeEmpty("the old behavior owned that slot");
            document.Root.GetOrNull("OnUsed")!.GetString().Should().Be("teleport");
            document.Root.GetOrNull("HasInventory")!.GetInteger().Should().Be(0,
                "a flag required only by the old behavior must not leak into the new one");
            new VarTable(document.Root).Any(entry => entry.Name == "SCAVENGE_POINT_LOOT_TABLE_NAME")
                .Should().BeFalse("switching clears the variables only the old behavior used");
        }

        [Test]
        public void Apply_CustomToDecorClearsRawWiringAndReportsWhatWillBeLost()
        {
            var document = BuildDocument(
                ("OnUsed", "my_custom_script"),
                ("OnDeath", "my_custom_death"));
            var variables = new VarTable(document.Root);
            variables.SetString("CUSTOM_SETTING", "value");

            PlaceableBehaviorApplier
                .ValuesLostBySwitching(
                    document.Root,
                    PlaceableBehaviorCatalog.Custom,
                    PlaceableBehaviorCatalog.None)
                .Should().BeEquivalentTo(
                    "OnUsed script",
                    "OnDeath script",
                    "CUSTOM_SETTING");

            PlaceableBehaviorApplier.Apply(
                document.Root,
                PlaceableBehaviorCatalog.Custom,
                PlaceableBehaviorCatalog.None);

            PlaceableBehaviorDetector.ReadScripts(document.Root).Should().BeEmpty();
            new VarTable(document.Root).Should().BeEmpty();
            PlaceableBehaviorDetector.Detect(document.Root).Should().BeSameAs(PlaceableBehaviorCatalog.None);
        }

        [Test]
        public void BehaviorSelection_DoesNotSnapAmbiguousChoicesBackToDecor()
        {
            var document = BuildDocument();
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            var section = new PlaceableBehaviorSectionViewModel(
                context,
                new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                new AcceptingPrompts(),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            // This mirrors the blueprint editor's refresh after a behavior switch. Custom and
            // variable-only behaviors have no stored signature until their first field is filled.
            section.BehaviorChanged += () => section.RefreshFromDocument();

            foreach (var id in new[]
                     {
                         PlaceableBehaviorCatalog.CustomId,
                         "harvest_node",
                         "asteroid",
                         "visibility_gated"
                     })
            {
                section.SelectedItem = section.Items.Single(item => item.Behavior?.Id == id);
                section.Current.Id.Should().Be(id);
            }

            section.SelectedItem = section.Items.Single(
                item => item.Behavior?.Id == PlaceableBehaviorCatalog.CustomId);
            section.ShowsCustomFlags.Should().BeTrue();
            section.CustomFlagFields
                .Select(field => field.Descriptor.FieldName)
                .Should().BeEquivalentTo("Useable", "HasInventory", "Static", "Plot");

            section.SelectedItem = section.Items.Single(
                item => item.Behavior?.Id == PlaceableBehaviorCatalog.NoneId);
            section.Current.Should().BeSameAs(PlaceableBehaviorCatalog.None);
            section.ShowsCustomFlags.Should().BeFalse();
        }

        [Test]
        public void BehaviorSelection_EveryCatalogBehaviorCanBeSelectedAndSurviveRefresh()
        {
            foreach (var behavior in PlaceableBehaviorCatalog.Behaviors)
            {
                var document = BuildDocument();
                var context = new EditorFieldContext(
                    document,
                    (_, mutation) =>
                    {
                        mutation();
                        return true;
                    });
                var section = new PlaceableBehaviorSectionViewModel(
                    context,
                    new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                    new AcceptingPrompts(),
                    (_, mutation) =>
                    {
                        mutation();
                        return true;
                    });

                // This is the same refresh the blueprint editor performs after a switch.
                section.BehaviorChanged += () => section.RefreshFromDocument();

                var item = section.Items.Single(candidate => candidate.Behavior?.Id == behavior.Id);
                section.SelectedItem = item;

                section.Current.Should().BeSameAs(behavior, $"{behavior.Name} must be selectable");
                section.SelectedItem.Should().BeSameAs(item);
            }
        }

        [Test]
        public void BehaviorSelection_CategoryHeadingsAreDisabledInTheList()
        {
            var section = BuildBehaviorSection();
            var headings = section.Items.Where(item => item.IsHeader).ToList();

            headings.Should().NotBeEmpty();
            headings.Should().OnlyContain(item => !item.IsSelectable && item.Behavior == null);

            var viewPath = Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset",
                "Editors",
                "Views",
                "BlueprintEditorView.axaml");
            var view = File.ReadAllText(viewPath);

            view.Should().Contain(
                "<Setter Property=\"IsEnabled\" Value=\"{ReflectionBinding IsSelectable}\" />",
                "the ListBox container must reject both mouse and keyboard selection of headings");
        }

        [Test]
        public void BehaviorSelection_ReclassifiesWhenTheSelectedBehaviorNoLongerMatchesTheDocument()
        {
            var document = BuildDocument();
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            var section = new PlaceableBehaviorSectionViewModel(
                context,
                new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                new AcceptingPrompts(),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            section.SelectedItem = section.Items.Single(item => ReferenceEquals(item.Behavior, scavenge));
            PlaceableBehaviorApplier.Apply(document.Root, scavenge, teleporter);
            section.RefreshFromDocument();

            section.Current.Should().BeSameAs(teleporter,
                "undo or changed wiring must not leave a stale explicit selection");
        }

        [Test]
        public void Apply_LeavesAHandEditedSlotAlone()
        {
            var document = BuildDocument(("OnOpen", "my_own_script"));
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;

            PlaceableBehaviorApplier.Apply(document.Root, scavenge, PlaceableBehaviorCatalog.None);

            document.Root.GetOrNull("OnOpen")!.GetString().Should().Be("my_own_script",
                "a slot holding something the behavior did not write belongs to whoever wrote it");
        }

        [Test]
        public void ValuesLostBySwitching_NamesOnlyVariablesThatActuallyHoldSomething()
        {
            var document = BuildDocument();
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;

            new VarTable(document.Root).SetString("SCAVENGE_POINT_LOOT_TABLE_NAME", "SOME_TABLE");
            new VarTable(document.Root).SetInt("SCAVENGE_POINT_LEVEL", 0);

            PlaceableBehaviorApplier
                .ValuesLostBySwitching(document.Root, scavenge, PlaceableBehaviorCatalog.None)
                .Should().BeEquivalentTo(new[] { "SCAVENGE_POINT_LOOT_TABLE_NAME" });
        }

        [Test]
        public void ValuesLostBySwitching_IgnoresDefaultsUntilTheBuilderChangesThem()
        {
            foreach (var behavior in PlaceableBehaviorCatalog.Behaviors
                         .Where(candidate => !candidate.IsSentinel))
            {
                var document = BuildDocument();
                PlaceableBehaviorApplier.Apply(
                    document.Root,
                    PlaceableBehaviorCatalog.None,
                    behavior);

                PlaceableBehaviorApplier
                    .ValuesLostBySwitching(
                        document.Root,
                        behavior,
                        PlaceableBehaviorCatalog.None)
                    .Should().BeEmpty(
                        $"{behavior.Name}'s automatic defaults are not user-authored changes");
            }

            var questDocument = BuildDocument();
            var quest = PlaceableBehaviorCatalog.FindById("quest_activator")!;
            PlaceableBehaviorApplier.Apply(
                questDocument.Root,
                PlaceableBehaviorCatalog.None,
                quest);
            new VarTable(questDocument.Root)
                .SetInt("QUEST_ENCOUNTER_COOLDOWN_MINUTES", 30);

            PlaceableBehaviorApplier
                .ValuesLostBySwitching(
                    questDocument.Root,
                    quest,
                    PlaceableBehaviorCatalog.None)
                .Should().BeEquivalentTo("QUEST_ENCOUNTER_COOLDOWN_MINUTES");
        }

        [Test]
        public void BehaviorSelection_WarnsOnlyForChangedValuesAndUsesPlainLabels()
        {
            var document = BuildDocument();
            var quest = PlaceableBehaviorCatalog.FindById("quest_activator")!;
            PlaceableBehaviorApplier.Apply(
                document.Root,
                PlaceableBehaviorCatalog.None,
                quest);
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            var prompts = new RecordingPrompts();
            var section = new PlaceableBehaviorSectionViewModel(
                context,
                new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                prompts,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            section.SelectedItem = section.Items.Single(item => item.Behavior?.Id == "teleporter");
            prompts.DestructivePrompts.Should().BeEmpty(
                "selecting Quest Activator only wrote its normal defaults");

            PlaceableBehaviorApplier.Apply(document.Root, section.Current, quest);
            new VarTable(document.Root).SetInt("QUEST_ENCOUNTER_IDLE_MINUTES", 25);
            section.RefreshFromDocument(reclassifyAmbiguousSelection: true);
            section.SelectedItem = section.Items.Single(item => item.Behavior?.Id == "teleporter");

            prompts.DestructivePrompts.Should().ContainSingle();
            prompts.DestructivePrompts[0].Headline.Should().Be("Change behavior to Teleporter?");
            prompts.DestructivePrompts[0].Message.Should().Contain("Idle despawn (minutes)");
            prompts.DestructivePrompts[0].Message.Should().NotContain("QUEST_ENCOUNTER_IDLE_MINUTES");
            prompts.DestructivePrompts[0].ConfirmLabel.Should().Be("Change behavior");
        }

        [Test]
        public void UnmanagedVariables_ReportsWhatTheBehaviorDoesNotOwn()
        {
            var document = BuildDocument();
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            new VarTable(document.Root).SetString("DESTINATION", "SOMEWHERE");
            new VarTable(document.Root).SetString("SCRIPT_1", "Placeable.WarpDevice.OnUsed");

            PlaceableBehaviorDetector.UnmanagedVariables(document.Root, teleporter)
                .Should().BeEquivalentTo(new[] { "SCRIPT_1" });
        }

        [Test]
        public void Detect_RunsOverTheWholeCorpusWithoutThrowing()
        {
            var counts = new Dictionary<string, int>();

            foreach (var path in Directory.EnumerateFiles(UtpDirectory, "*.utp.json"))
            {
                var behavior = PlaceableBehaviorDetector.Detect(JsonGffDocument.Load(path).Root);
                counts[behavior.Id] = counts.TryGetValue(behavior.Id, out var existing) ? existing + 1 : 1;
            }

            // Counted from the corpus rather than pinned to a number: the module gains and loses
            // blueprints, and a stale constant would fail for a reason that is not about detection.
            var blueprintCount = Directory.EnumerateFiles(UtpDirectory, "*.utp.json").Count();
            counts.Values.Sum().Should().Be(blueprintCount, "every blueprint gets exactly one behavior");

            // 94% of blueprints set no script at all, so decor has to dominate - a detector that
            // classified most of the module as something else would be finding patterns that are
            // not there.
            counts[PlaceableBehaviorCatalog.NoneId].Should().BeGreaterThan(counts.Values.Sum() * 3 / 4);

            counts.Should().ContainKey("scavenge_point");
            counts.Should().ContainKey("chair");
        }

        [Test]
        public void ApplyThenUndo_RestoresACorpusFileByteForByte()
        {
            // The permanent gate for the whole feature: a behavior is a view, so applying one and
            // taking it back must leave the file exactly as it was found.
            var path = Directory.EnumerateFiles(UtpDirectory, "*.utp.json").First();
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var detected = PlaceableBehaviorDetector.Detect(document.Root);
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            using (session.Begin("switch behavior"))
                PlaceableBehaviorApplier.Apply(document.Root, detected, teleporter);

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse(
                "the switch has to have changed something");

            session.UndoStack.Undo();

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing a behavior switch must restore the original bytes exactly");
        }

        private static JsonGffStruct? LoadFirstPlaceableWith(string variableName)
        {
            foreach (var path in Directory.EnumerateFiles(UtpDirectory, "*.utp.json"))
            {
                var root = JsonGffDocument.Load(path).Root;
                if (new VarTable(root).Any(entry => entry.Name == variableName))
                    return root;
            }

            return null;
        }

        /// <summary>A minimal placeable document with the given script slots set.</summary>
        private static JsonGffDocument BuildDocument(params (string Slot, string Script)[] scripts)
        {
            var json = "{\n  \"__data_type\": \"UTP \",\n  \"TemplateResRef\": { \"type\": \"resref\", \"value\": \"test\" }" +
                       string.Concat(scripts.Select(entry =>
                           $",\n  \"{entry.Slot}\": {{ \"type\": \"resref\", \"value\": \"{entry.Script}\" }}")) +
                       "\n}\n";

            return JsonGffDocument.Parse(System.Text.Encoding.UTF8.GetBytes(json));
        }

        private static JsonGffStruct BuildPlaceable(params (string Slot, string Script)[] scripts) =>
            BuildDocument(scripts).Root;

        private static PlaceableBehaviorField Field(string behaviorId, string variableName) =>
            PlaceableBehaviorCatalog.FindById(behaviorId)!.Fields
                .Single(field => field.VariableName == variableName);

        private static PlaceableBehaviorSectionViewModel BuildBehaviorSection()
        {
            var document = BuildDocument();
            var context = new EditorFieldContext(
                document,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            return new PlaceableBehaviorSectionViewModel(
                context,
                new BehaviorValueSourceProvider(gameCode: null, tags: () => null),
                new AcceptingPrompts(),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
        }

        private static PlaceableModelCatalog CreateFixturePlaceableModelCatalog(string directory)
        {
            var twoDaPath = Path.Combine(directory, "sw_2da");
            Directory.CreateDirectory(twoDaPath);
            File.WriteAllText(
                Path.Combine(twoDaPath, "placeables.2da"),
                "2DA V2.0\r\n\r\nLabel StrRef ModelName\r\n" +
                "0 Fixture **** plc_fixture\r\n");

            return new PlaceableModelCatalog(
                new TwoDaService(twoDaPath),
                new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
        }

        private static string? FindGameServerSource()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                if (Directory.Exists(Path.Combine(candidate, "Feature")))
                    return candidate;

                current = current.Parent;
            }

            return null;
        }

        private sealed class AcceptingPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Overwrite);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Discard);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(true);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }

        private sealed class RecordingPrompts : IEditorPromptService
        {
            public List<(string Headline, string Message, string ConfirmLabel)> DestructivePrompts
            {
                get;
            } = new();

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Overwrite);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Discard);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel)
            {
                DestructivePrompts.Add((headline, message, confirmLabel));
                return Task.FromResult(true);
            }

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
