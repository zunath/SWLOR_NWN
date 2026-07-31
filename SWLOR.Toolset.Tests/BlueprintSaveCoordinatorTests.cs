using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class BlueprintSaveCoordinatorTests
    {
        private static readonly ResourceType[] BlueprintTypes =
        {
            ResourceType.Utc, ResourceType.Uti, ResourceType.Utp, ResourceType.Utd,
            ResourceType.Utm, ResourceType.Utt, ResourceType.Uts, ResourceType.Utw
        };

        private string _testRoot = string.Empty;
        private string _moduleRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-blueprint-save-" + Guid.NewGuid().ToString("N"));
            _moduleRoot = Path.Combine(_testRoot, "Module");
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "are"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "utc"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "git"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, recursive: true);
        }

        [TestCaseSource(nameof(BlueprintTypes))]
        public void RenameMovesTheBlueprintAndSynchronizesEveryPlacedType(ResourceType type)
        {
            const string oldResRef = "probe_old";
            const string newResRef = "probe_new";
            const string areaResRef = "probe_area";
            var typeFolder = Path.Combine(_moduleRoot, type.Extension());
            Directory.CreateDirectory(typeFolder);
            var oldPath = Path.Combine(typeFolder, $"{oldResRef}.{type.Extension()}.json");
            var newPath = Path.Combine(typeFolder, $"{newResRef}.{type.Extension()}.json");
            var blueprintBytes = BlueprintTemplateFactory.CreateFileContent(
                type, oldResRef, "Probe Blueprint");
            File.WriteAllBytes(oldPath, blueprintBytes);
            File.WriteAllText(
                Path.Combine(_moduleRoot, "are", $"{areaResRef}.are.json"),
                "{}");

            var blueprint = JsonGffDocument.Parse(blueprintBytes);
            var gitRoot = new JsonGffStruct();
            var list = JsonGffField.CreateList();
            using (EditScope.EnterConstruction())
            {
                list.InsertElement(
                    0,
                    InstanceFieldMap.CreateInstance(
                        type, blueprint, oldResRef, 12.5, 24.25, 1.5, 0, 1));
                gitRoot.Add(BlueprintInstanceSynchronizer.ListFieldName(type), list);
            }

            var git = new JsonGffDocument("GIT ", gitRoot);
            var gitPath = Path.Combine(_moduleRoot, "git", $"{areaResRef}.git.json");
            File.WriteAllBytes(gitPath, git.ToBytes());

            using var session = DocumentSession.Open(oldPath);
            var identityField = type == ResourceType.Utm ? "ResRef" : "TemplateResRef";
            session.Execute(
                "Change identity",
                () =>
                {
                    session.Document.Root.SetString(
                        identityField, GffFieldType.ResRef, newResRef);
                    session.Document.Root.SetString(
                        "Tag", GffFieldType.CExoString, "updated_tag");
                });

            var outcome = new BlueprintSaveCoordinator(new OutputLogService())
                .Save(session, type, oldResRef, newResRef);

            outcome.Saved.Should().BeTrue();
            outcome.Renamed.Should().BeTrue();
            outcome.UpdatedInstances.Should().Be(1);
            outcome.UpdatedAreas.Should().Equal(areaResRef);
            File.Exists(oldPath).Should().BeFalse();
            File.Exists(newPath).Should().BeTrue();
            session.FilePath.Should().Be(newPath);
            JsonGffDocument.Load(newPath).Root.GetStringOrNull(identityField)
                .Should().Be(newResRef);

            var savedGit = JsonGffDocument.Load(gitPath);
            var placed = savedGit.Root
                .Get(BlueprintInstanceSynchronizer.ListFieldName(type))
                .Elements!.Single();
            InstanceFieldMap.GetTemplateResRef(type, placed).Should().Be(newResRef);
            InstanceFieldMap.GetTag(placed).Should().Be("updated_tag");
            InstanceFieldMap.GetPosition(type, placed)
                .Should().Be((12.5f, 24.25f, 1.5f));
        }

        [Test]
        public void RenameRefusesToOverwriteUnsavedOpenAreaInstances()
        {
            const ResourceType type = ResourceType.Utp;
            const string oldResRef = "probe_old";
            const string newResRef = "probe_new";
            const string areaResRef = "probe_area";
            var folder = Path.Combine(_moduleRoot, type.Extension());
            Directory.CreateDirectory(folder);
            var oldPath = Path.Combine(folder, $"{oldResRef}.utp.json");
            var blueprintBytes = BlueprintTemplateFactory.CreateFileContent(type, oldResRef, "Probe");
            File.WriteAllBytes(oldPath, blueprintBytes);
            File.WriteAllText(Path.Combine(_moduleRoot, "are", $"{areaResRef}.are.json"), "{}");

            var root = new JsonGffStruct();
            var list = JsonGffField.CreateList();
            using (EditScope.EnterConstruction())
            {
                list.InsertElement(
                    0,
                    InstanceFieldMap.CreateInstance(
                        type, JsonGffDocument.Parse(blueprintBytes), oldResRef, 1, 2, 3));
                root.Add(BlueprintInstanceSynchronizer.ListFieldName(type), list);
            }
            File.WriteAllBytes(
                Path.Combine(_moduleRoot, "git", $"{areaResRef}.git.json"),
                new JsonGffDocument("GIT ", root).ToBytes());

            using var session = DocumentSession.Open(oldPath);
            session.Execute(
                "Change ResRef",
                () => session.Document.Root.SetString(
                    "TemplateResRef", GffFieldType.ResRef, newResRef));
            var coordinator = new BlueprintSaveCoordinator(
                new OutputLogService(),
                hasUnsavedAreaInstances: value => value == areaResRef);

            var outcome = coordinator.Save(session, type, oldResRef, newResRef);

            outcome.Saved.Should().BeFalse();
            File.Exists(oldPath).Should().BeTrue();
            File.Exists(Path.Combine(folder, $"{newResRef}.utp.json")).Should().BeFalse();
            var placed = JsonGffDocument.Load(
                    Path.Combine(_moduleRoot, "git", $"{areaResRef}.git.json"))
                .Root.Get(BlueprintInstanceSynchronizer.ListFieldName(type))
                .Elements!.Single();
            InstanceFieldMap.GetTemplateResRef(type, placed).Should().Be(oldResRef);
        }

        [Test]
        public void ItemRenameUpdatesEmbeddedEquipmentAndStoreInstancesWithoutRenamingOtherBlueprintTypes()
        {
            const string oldResRef = "shared_old";
            const string newResRef = "shared_new";
            var root = new JsonGffStruct();
            JsonGffStruct creature;
            JsonGffStruct equipped;
            JsonGffStruct item;
            using (EditScope.EnterConstruction())
            {
                var creatures = JsonGffField.CreateList();
                creature = JsonGffField.CreateStruct(4).Struct!;
                creature.SetString("TemplateResRef", GffFieldType.ResRef, oldResRef);
                var equipment = JsonGffField.CreateList();
                equipped = JsonGffField.CreateStruct(1).Struct!;
                equipped.SetString("TemplateResRef", GffFieldType.ResRef, oldResRef);
                equipment.InsertElement(0, equipped);
                creature.Add("Equip_ItemList", equipment);
                creatures.InsertElement(0, creature);
                root.Add("Creature List", creatures);

                var stores = JsonGffField.CreateList();
                var store = JsonGffField.CreateStruct(11).Struct!;
                var panes = JsonGffField.CreateList();
                var pane = JsonGffField.CreateStruct(0).Struct!;
                var items = JsonGffField.CreateList();
                item = JsonGffField.CreateStruct(0).Struct!;
                item.SetString("TemplateResRef", GffFieldType.ResRef, oldResRef);
                items.InsertElement(0, item);
                pane.Add("ItemList", items);
                panes.InsertElement(0, pane);
                store.Add("StoreList", panes);
                stores.InsertElement(0, store);
                root.Add("StoreList", stores);
            }

            var itemBlueprint = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, newResRef, "Renamed Item"));
            var git = new JsonGffDocument("GIT ", root);
            int updated;
            using (EditScope.EnterConstruction())
            {
                updated = BlueprintInstanceSynchronizer.Synchronize(
                    ResourceType.Uti,
                    itemBlueprint,
                    git,
                    oldResRef,
                    newResRef);
            }

            updated.Should().Be(2);
            creature.GetStringOrNull("TemplateResRef").Should().Be(oldResRef);
            equipped.GetStringOrNull("TemplateResRef").Should().Be(newResRef);
            item.GetStringOrNull("TemplateResRef").Should().Be(newResRef);
        }
    }
}
