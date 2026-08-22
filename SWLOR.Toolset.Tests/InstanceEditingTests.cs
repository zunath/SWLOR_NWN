using System.Numerics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Editors.Waypoints;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP3.3: InstanceFieldMap.CreateInstance against real corpus data. Uses
    /// Module\git\ar_scor_kacademy.git.json (a real placed "vnpcsofficer" creature instance) and
    /// Module\utc\vnpcsofficer.utc.json (its blueprint) - verified while designing the field map
    /// to differ only by the blueprint-only "Comment"/"PaletteID" fields and the instance-only
    /// position/orientation/__struct_id fields.
    /// </summary>
    public class InstanceEditingTests
    {
        private static string GitPath => Path.Combine(CorpusLocator.ModuleDirectory, "git", "ar_scor_kacademy.git.json");
        private static string BlueprintPath => Path.Combine(CorpusLocator.ModuleDirectory, "utc", "vnpcsofficer.utc.json");

        /// <summary>Fields a real corpus creature instance may carry that a freshly placed
        /// instance will not (e.g. "ItemList", carrying loose inventory dropped in the toolset
        /// after placement) - documented allowlist for the field-set comparison below.</summary>
        private static readonly HashSet<string> OptionalInstanceOnlyFields = new(StringComparer.Ordinal)
        {
            "ItemList"
        };

        [Test]
        public void CreateInstance_Creature_InsertThenUndo_RestoresOriginalBytesExactly()
        {
            var original = File.ReadAllBytes(GitPath);
            var gitDocument = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(GitPath, gitDocument);

            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var listField = gitDocument.Root.Get("Creature List");
            var startCount = listField.Elements!.Count;

            using (session.Begin("add creature instance"))
            {
                var instance = InstanceFieldMap.CreateInstance(
                    ResourceType.Utc, blueprint, "vnpcsofficer", 12.5, 3.0, 15.0);
                listField.InsertElement(listField.Elements!.Count, instance);
            }

            listField.Elements!.Count.Should().Be(startCount + 1);
            gitDocument.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();

            listField.Elements!.Count.Should().Be(startCount);
            gitDocument.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the whole add-instance transaction must restore the exact original bytes");
        }

        [Test]
        public void CreateInstance_Creature_SerializedBytes_RoundTripThroughReparse()
        {
            var gitDocument = JsonGffDocument.Parse(File.ReadAllBytes(GitPath));
            using var session = new DocumentSession(GitPath, gitDocument);

            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var listField = gitDocument.Root.Get("Creature List");

            using (session.Begin("add creature instance"))
            {
                var instance = InstanceFieldMap.CreateInstance(
                    ResourceType.Utc, blueprint, "vnpcsofficer", 12.5, 3.0, 15.0);
                listField.InsertElement(listField.Elements!.Count, instance);
            }

            var firstWrite = gitDocument.ToBytes();

            // Re-parsing the written bytes and writing them again must produce byte-identical
            // output: proof the new element's fields serialized in nwn_gff's sorted order (an
            // out-of-order field would still parse, but a naive re-serialization would then not
            // match nwn_gff's own sort convention on the second write).
            var reparsed = JsonGffDocument.Parse(firstWrite);
            var secondWrite = reparsed.ToBytes();

            secondWrite.AsSpan().SequenceEqual(firstWrite).Should().BeTrue(
                "the newly inserted instance must serialize in nwn_gff's sorted field order so re-parsing and re-writing is idempotent");
        }

        [Test]
        public void CreateInstance_Creature_FieldSet_MatchesRealCorpusInstance()
        {
            var gitDocument = JsonGffDocument.Parse(File.ReadAllBytes(GitPath));
            var git = new GitDocument(gitDocument);

            // The real placed "vnpcsofficer" instance already in this corpus file - our
            // ground truth for what fields a creature instance actually carries.
            var realInstance = git.Creatures.Single(c =>
                c.GetOrNull("TemplateResRef")?.GetString() == "vnpcsofficer");
            var realFieldNames = realInstance.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var newInstance = InstanceFieldMap.CreateInstance(
                ResourceType.Utc, blueprint, "vnpcsofficer", 0, 0, 0);
            var newFieldNames = newInstance.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

            // The new instance's fields must be a subset of the real instance's fields modulo the
            // documented optional allowlist (fields the real instance happens to carry that a
            // freshly-placed instance would not yet have, e.g. loose inventory).
            var missingFromNew = realFieldNames.Except(newFieldNames).Except(OptionalInstanceOnlyFields).ToList();
            missingFromNew.Should().BeEmpty("every non-optional field the real corpus instance carries should also appear on a freshly created instance");

            var extraOnNew = newFieldNames.Except(realFieldNames).ToList();
            extraOnNew.Should().BeEmpty("a freshly created instance should not invent fields the real corpus instance does not have");
        }

        [Test]
        public void CreateInstance_Creature_PositionFloats_FormatNimStyle()
        {
            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utc, blueprint, "vnpcsofficer", 12.5, 3.0, 15.0);

            var xPosition = instance.Get("XPosition");
            xPosition.Type.Should().Be(GffFieldType.Float);
            xPosition.GetSingle().Should().Be(12.5f);
            Encoding.ASCII.GetString(xPosition.RawValue!).Should().Be("12.5");
        }

        [Test]
        public void CreateInstance_Trigger_GetsUsableDefaultSquareGeometry()
        {
            var blueprintPath = Directory.EnumerateFiles(
                Path.Combine(CorpusLocator.ModuleDirectory, "utt"), "*.utt.json").First();
            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(blueprintPath));

            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utt, blueprint, "test_trigger", 10, 20, 3);
            var geometry = instance.Get("Geometry");

            geometry.Type.Should().Be(GffFieldType.List);
            geometry.Elements.Should().HaveCount(4, "a trigger needs a polygon to receive events");
            geometry.Elements!.Select(point => point.Get("PointX").GetSingle())
                .Should().Equal(-1f, 1f, 1f, -1f);
            geometry.Elements.Select(point => point.Get("PointY").GetSingle())
                .Should().Equal(-1f, -1f, 1f, 1f);
            geometry.Elements.Should().OnlyContain(point =>
                Encoding.ASCII.GetString(point.RawStructId!) == "3" &&
                Math.Abs(point.Get("PointZ").GetSingle() - 0.025f) < 0.0001f);
            InstanceFieldMap.GetTriggerGeometrySize(instance).Should().Be((2f, 2f));
        }

        [Test]
        public void SetTriggerGeometrySize_ResizesThePolygonAroundItsCentre()
        {
            var blueprintPath = Directory.EnumerateFiles(
                Path.Combine(CorpusLocator.ModuleDirectory, "utt"), "*.utt.json").First();
            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(blueprintPath));
            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utt, blueprint, "test_trigger", 10, 20, 3);

            InstanceFieldMap.SetTriggerGeometrySize(instance, 6f, 4f);

            InstanceFieldMap.GetTriggerGeometrySize(instance).Should().Be((6f, 4f));
            var geometry = instance.Get("Geometry").Elements!;
            geometry.Select(point => point.Get("PointX").GetSingle()).Should().Equal(-3f, 3f, 3f, -3f);
            geometry.Select(point => point.Get("PointY").GetSingle()).Should().Equal(-2f, -2f, 2f, 2f);
        }

        [Test]
        public void CreateInstance_Waypoint_UsesSelectedFileResRefInsteadOfStaleEmbeddedValue()
        {
            const string selectedResRef = "fp_danmount";
            var blueprintPath = Path.Combine(
                CorpusLocator.ModuleDirectory, "utw", selectedResRef + ".utw.json");
            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(blueprintPath));
            blueprint.Root.Get("TemplateResRef").GetString().Should().Be(
                "fp_35", "the regression fixture intentionally has a stale embedded resref");

            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utw, blueprint, selectedResRef, 10, 20, 3);

            InstanceFieldMap.GetTemplateResRef(ResourceType.Utw, instance)
                .Should().Be(selectedResRef);
            instance.Get("TemplateResRef").Type.Should().Be(GffFieldType.ResRef);
        }

        [Test]
        public void SelectingAPlacedWaypointUsesTheWaypointBehaviorEditor()
        {
            const string area = "apartment_2";
            var gitPath = Path.Combine(CorpusLocator.ModuleDirectory, "git", area + ".git.json");
            var gicPath = Path.Combine(CorpusLocator.ModuleDirectory, "gic", area + ".gic.json");
            using var gitSession = DocumentSession.Open(gitPath);
            using var gicSession = DocumentSession.Open(gicPath);
            using var section = new InstanceListSectionViewModel(
                "Waypoints",
                "WaypointList",
                ResourceType.Utw,
                gitSession,
                gicSession,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                (description, edit) =>
                {
                    using (gitSession.Begin(description))
                        edit();
                    return true;
                },
                null,
                new OutputLogService(),
                new StubPrompts(),
                waypointEditorServices: new WaypointEditorServices(
                    area,
                    new WaypointBehaviorCatalog(null, null)));

            section.SelectedRow = section.Rows.First();

            section.HasWaypointBehaviorEditor.Should().BeTrue();
            section.UsesGenericDetailEditor.Should().BeFalse();
            section.WaypointEditor.Should().NotBeNull();
            section.WaypointEditor!.HeaderKind.Should().Be("instance");
            section.WaypointEditor.HeaderOwner.Should().Be(area);
            section.VarTableSection.Should().BeNull(
                "waypoint variables are owned by the behavior editor's Custom tab");
        }

        [Test]
        public void RefreshingAnAreaWaypointCatalogReclassifiesTheSelectionAndFutureSelections()
        {
            const string area = "anchor_entreenor";
            var gitPath = Path.Combine(CorpusLocator.ModuleDirectory, "git", area + ".git.json");
            var gicPath = Path.Combine(CorpusLocator.ModuleDirectory, "gic", area + ".gic.json");
            using var gitSession = DocumentSession.Open(gitPath);
            using var gicSession = DocumentSession.Open(gicPath);
            using var section = new InstanceListSectionViewModel(
                "Waypoints",
                "WaypointList",
                ResourceType.Utw,
                gitSession,
                gicSession,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                (description, edit) =>
                {
                    using (gitSession.Begin(description))
                        edit();
                    return true;
                },
                null,
                new OutputLogService(),
                new StubPrompts(),
                waypointEditorServices: new WaypointEditorServices(
                    area,
                    new WaypointBehaviorCatalog(null, Array.Empty<string>())));
            var selected = section.Rows.First(row => !string.IsNullOrWhiteSpace(row.Tag));
            section.SelectedRow = selected;
            section.WaypointEditor!.Behavior.Id.Should().Be(WaypointBehaviorCatalog.CustomId);

            section.RefreshWaypointCatalog(
                new WaypointBehaviorCatalog(null, new[] { selected.Tag }));
            section.WaypointEditor.Behavior.Id
                .Should().Be(WaypointBehaviorCatalog.TransitionDestinationId);

            section.SelectedRow = null;
            section.SelectedRow = selected;
            section.WaypointEditor!.Behavior.Id.Should().Be(
                WaypointBehaviorCatalog.TransitionDestinationId,
                "future selections must use the refreshed catalog too");
            gitSession.UndoStack.IsDirty.Should().BeFalse(
                "refreshing derived classification must not edit the area");
        }

        [Test]
        public void GetVisualTransform_ReadsScaleDegreeRotationAndTranslation()
        {
            var instance = new JsonGffStruct();
            var visual = JsonGffField.CreateStruct(6);
            var visualStruct = visual.Struct!;
            AddSingle(visualStruct, "ScaleX", 2f);
            AddSingle(visualStruct, "ScaleY", 2f);
            AddSingle(visualStruct, "ScaleZ", 2f);
            AddSingle(visualStruct, "RotateZ", 90f);
            AddSingle(visualStruct, "TranslateX", 3f);
            AddSingle(visualStruct, "TranslateY", 4f);
            AddSingle(visualStruct, "TranslateZ", 5f);
            instance.Add("VisualTransform", visual);

            var transformed = Vector3.Transform(new Vector3(1f, 0f, 0f),
                InstanceFieldMap.GetVisualTransform(instance));

            transformed.X.Should().BeApproximately(3f, 0.0001f);
            transformed.Y.Should().BeApproximately(6f, 0.0001f);
            transformed.Z.Should().BeApproximately(5f, 0.0001f);
        }

        private static void AddSingle(JsonGffStruct target, string name, float value)
        {
            var field = JsonGffField.CreateScalar(GffFieldType.Float, Array.Empty<byte>());
            field.SetSingle(value);
            target.Add(name, field);
        }

        /// <summary>
        /// WP5.2 acceptance test: the 3D-view move gizmo commits through
        /// InstanceListSectionViewModel.SetInstancePosition, which is exactly
        /// InstanceFieldMap.SetPosition wrapped in one DocumentSession transaction - the same
        /// setter (and therefore the same diff shape) the instance-list detail form's X/Y editors
        /// already use. Proves that programmatic path directly against a real area's .git
        /// document: moving an instance changes exactly its X/Y value lines (Z untouched, matching
        /// the gizmo's contract of "final X/Y, Z unchanged"), and undoing the transaction restores
        /// the exact original bytes.
        /// </summary>
        [Test]
        public void SetPosition_ProgrammaticGizmoMovePath_ChangesOnlyXyValueLines_AndUndoRestoresBytesExactly()
        {
            var original = File.ReadAllBytes(GitPath);
            var gitDocument = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(GitPath, gitDocument);

            var git = new GitDocument(gitDocument);
            var creature = git.Creatures.Single(c => c.GetOrNull("TemplateResRef")?.GetString() == "vnpcsofficer");
            var (startX, startY, startZ) = InstanceFieldMap.GetPosition(ResourceType.Utc, creature);

            using (session.Begin("Move Creature \"vnpcsofficer\""))
                InstanceFieldMap.SetPosition(ResourceType.Utc, creature, startX + 5f, startY - 2f, startZ);

            var moved = gitDocument.ToBytes();
            moved.AsSpan().SequenceEqual(original).Should().BeFalse();

            var originalLines = Encoding.UTF8.GetString(original).Split('\n');
            var movedLines = Encoding.UTF8.GetString(moved).Split('\n');

            movedLines.Length.Should().Be(originalLines.Length, "an in-place move must not add or remove lines");

            var changedLines = Enumerable.Range(0, originalLines.Length)
                .Where(i => originalLines[i] != movedLines[i])
                .ToList();

            changedLines.Should().HaveCount(2,
                "moving an instance must change exactly its X and Y value lines - Z is left untouched - the " +
                "same diff shape the instance-list detail form's X/Y editors produce. Changed lines: " +
                string.Join(", ", changedLines.Select(i => $"{i}: '{originalLines[i]}' -> '{movedLines[i]}'")));

            foreach (var line in changedLines)
                movedLines[line].Should().Contain("\"value\":");

            session.UndoStack.Undo();

            gitDocument.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the gizmo move transaction must restore the exact original bytes, the same guarantee the instance-list detail form's Undo provides");
        }

        [Test]
        public void SetInstanceTransform_SnappedDoorStyleMove_UndoesPositionAndHeadingTogether()
        {
            var original = File.ReadAllBytes(GitPath);
            var gitDocument = JsonGffDocument.Parse(original);
            using var gitSession = new DocumentSession(GitPath, gitDocument);
            using var gicSession = new DocumentSession(
                "unused.gic.json",
                new JsonGffDocument("GIC ", new JsonGffStruct()));
            var section = new InstanceListSectionViewModel(
                "Creatures",
                "Creature List",
                ResourceType.Utc,
                gitSession,
                gicSession,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                (description, edit) =>
                {
                    using (gitSession.Begin(description))
                        edit();
                    return true;
                },
                null,
                new OutputLogService(),
                new StubPrompts());
            var row = section.Rows.Single(candidate =>
                candidate.TemplateResRef == "vnpcsofficer");

            section.SetInstanceTransform(
                    row.Index,
                    row.X + 3,
                    row.Y - 4,
                    row.Z + 1,
                    xOrientation: 0,
                    yOrientation: 1,
                    "Snap to doorway")
                .Should().BeTrue();

            gitSession.UndoStack.Undo();

            gitDocument.ToBytes().Should().Equal(
                original,
                "one Undo must restore both halves of the snapped transform");
        }

        /// <summary>Mirrors <see cref="SetPosition_ProgrammaticGizmoMovePath_ChangesOnlyXyValueLines_AndUndoRestoresBytesExactly"/> for the rotate gizmo's InstanceFieldMap.SetOrientation path.</summary>
        [Test]
        public void SetOrientation_ProgrammaticGizmoRotatePath_ChangesOnlyOrientationValueLines_AndUndoRestoresBytesExactly()
        {
            var original = File.ReadAllBytes(GitPath);
            var gitDocument = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(GitPath, gitDocument);

            var git = new GitDocument(gitDocument);
            var creature = git.Creatures.Single(c => c.GetOrNull("TemplateResRef")?.GetString() == "vnpcsofficer");
            var (startXOrientation, startYOrientation) = InstanceFieldMap.GetOrientation(ResourceType.Utc, creature);

            using (session.Begin("Rotate Creature \"vnpcsofficer\""))
                InstanceFieldMap.SetOrientation(ResourceType.Utc, creature, -startYOrientation, startXOrientation);

            var rotated = gitDocument.ToBytes();
            rotated.AsSpan().SequenceEqual(original).Should().BeFalse();

            var originalLines = Encoding.UTF8.GetString(original).Split('\n');
            var rotatedLines = Encoding.UTF8.GetString(rotated).Split('\n');

            rotatedLines.Length.Should().Be(originalLines.Length, "an in-place rotate must not add or remove lines");

            var changedLines = Enumerable.Range(0, originalLines.Length)
                .Where(i => originalLines[i] != rotatedLines[i])
                .ToList();

            changedLines.Should().HaveCount(2,
                "rotating an instance must change exactly its XOrientation and YOrientation value lines. Changed lines: " +
                string.Join(", ", changedLines.Select(i => $"{i}: '{originalLines[i]}' -> '{rotatedLines[i]}'")));

            foreach (var line in changedLines)
                rotatedLines[line].Should().Contain("\"value\":");

            session.UndoStack.Undo();

            gitDocument.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the gizmo rotate transaction must restore the exact original bytes");
        }

        [Test]
        public void GetInstanceTemplateField_UsesResRefForStores_TemplateResRefOtherwise()
        {
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utm).Should().Be("ResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utc).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utp).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utd).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utw).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Uts).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utt).Should().Be("TemplateResRef");
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(false);
        }
    }
}
