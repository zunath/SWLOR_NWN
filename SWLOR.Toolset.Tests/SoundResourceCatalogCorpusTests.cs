using System.Text;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Formats.Gff;
using Nwn.Formats.Resources;
using Nwn.Toolset.Avalonia.Sounds;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

[TestFixture]
[Category("Corpus")]
public sealed class SoundResourceCatalogCorpusTests
{
    [Test]
    public void RealIndexedWavCatalogOpensAndRefusesInvalidNewPlaylistEntries()
    {
        var repositoryRoot = Support.ToolsetCorpusPaths.RepositoryRoot
            ?? throw new DirectoryNotFoundException("Set SWLOR_TEST_REPOSITORY_ROOT to the isolated SWLOR checkout.");
        var haksRoot = Support.ToolsetCorpusPaths.HaksRoot
            ?? throw new DirectoryNotFoundException("Set SWLOR_TEST_HAKS_ROOT to the read-only SWLOR HAK corpus.");
        var installRoot = NwnInstallLocator.Locate()
            ?? throw new DirectoryNotFoundException("A licensed NWN:EE install is required for the real base resource index.");
        var resourceIndex = ResourceIndex.FromHakBuilderConfig(
            Path.Combine(repositoryRoot, "Build", "hakbuilder.json"),
            haksRoot,
            KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
        resourceIndex.EnsureInitialized();

        var soundResRefs = SoundResourceCatalog.Read(resourceIndex);
        var invalidResRefs = soundResRefs
            .Where(resRef => !ResourceReferenceRules.IsValid(resRef))
            .ToArray();

        const string legacyInvalidResRef = "as_cv_ta-da1";
        Assert.That(invalidResRefs, Does.Contain(legacyInvalidResRef), "The indexed corpus contains this legacy UTS resource name.");

        TestContext.Progress.WriteLine(
            $"Indexed WAV catalog contains {soundResRefs.Count} unique ResRefs; invalid identities: " +
            (invalidResRefs.Length == 0 ? "none" : string.Join(", ", invalidResRefs)));
        Assert.That(soundResRefs, Is.Not.Empty, "The configured module, HAK, and base-game index must expose WAV candidates.");
        var validResRefs = soundResRefs.Where(ResourceReferenceRules.IsValid).Take(2).ToArray();
        Assert.That(validResRefs, Has.Length.EqualTo(2), "The catalog must include valid candidates for the editor flow.");
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes($$$"""
            {"__data_type":"UTS ","Sounds":{"type":"list","value":[
              {"Sound":{"type":"resref","value":"{{{validResRefs[0]}}}"},"Future":{"type":"cexostring","value":"retained-first"}},
              {"__struct_id":17,"Sound":{"type":"resref","value":"{{{legacyInvalidResRef}}}"},"Future":{"type":"cexostring","value":"retained-invalid"}},
              {"Sound":{"type":"resref","value":"{{{validResRefs[1]}}}"},"Future":{"type":"cexostring","value":"retained-second"}}
            ]}}
            """));
        using var session = new DocumentSession("wav-catalog.uts.json", document);
        var store = new BehaviorValueStore(document.Root);
        var editCalls = 0;
        var changed = 0;
        var editor = new SoundListEditorViewModel(
            store,
            new SoundListSchema("Sounds", "Sound"),
            soundResRefs,
            0,
            (description, mutation) =>
            {
                editCalls++;
                session.Execute(description, mutation);
                return true;
            },
            () => changed++);
        Assert.That(editor.AvailableSounds, Has.Count.EqualTo(soundResRefs.Count));

        var original = document.ToBytes();
        foreach (var invalidResRef in invalidResRefs)
        {
            editor.Candidate = null;
            editor.Search = invalidResRef;
            Assert.That(editor.FilteredSounds, Does.Contain(invalidResRef));
            editor.Candidate = invalidResRef;
            Assert.That(editor.AddCommand.CanExecute(null), Is.False);
            editor.AddCommand.Execute(null);
            Assert.That(editCalls, Is.Zero);
            Assert.That(document.ToBytes(), Is.EqualTo(original));
        }

        editor.SelectedEntry = editor.Rows[2];
        editor.MoveUpCommand.Execute(null);
        Assert.That(editCalls, Is.EqualTo(1));
        Assert.That(changed, Is.EqualTo(1));
        Assert.That(store.GetResRefList("Sounds", "Sound"), Is.EqualTo(new[]
            { validResRefs[0], validResRefs[1], legacyInvalidResRef }));
        Assert.That(document.Root.Get("Sounds").Elements![0].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-first"));
        Assert.That(document.Root.Get("Sounds").Elements![1].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-second"));
        Assert.That(document.Root.Get("Sounds").Elements![2].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-invalid"));

        editor.SelectedEntry = editor.Rows[0];
        editor.RemoveCommand.Execute(null);
        Assert.That(editCalls, Is.EqualTo(2));
        Assert.That(changed, Is.EqualTo(2));
        Assert.That(store.GetResRefList("Sounds", "Sound"), Is.EqualTo(new[] { validResRefs[1], legacyInvalidResRef }));
        Assert.That(document.Root.Get("Sounds").Elements![0].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-second"));
        Assert.That(document.Root.Get("Sounds").Elements![1].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-invalid"));

        editor.Search = validResRefs[1];
        editor.Candidate = validResRefs[1];
        editor.AddCommand.Execute(null);
        Assert.That(editCalls, Is.EqualTo(3));
        Assert.That(changed, Is.EqualTo(3));
        Assert.That(store.GetResRefList("Sounds", "Sound"), Is.EqualTo(new[]
            { validResRefs[1], legacyInvalidResRef, validResRefs[1] }));
        Assert.That(document.Root.Get("Sounds").Elements![0].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-second"));
        Assert.That(document.Root.Get("Sounds").Elements![1].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-invalid"));

        editor.SelectedEntry = editor.Rows[1];
        editor.MoveDownCommand.Execute(null);
        Assert.That(store.GetResRefList("Sounds", "Sound"), Is.EqualTo(new[]
            { validResRefs[1], validResRefs[1], legacyInvalidResRef }));
        Assert.That(document.Root.Get("Sounds").Elements![2].StructId, Is.EqualTo(17u));
        Assert.That(document.Root.Get("Sounds").Elements![2].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-invalid"));
        Assert.That(document.Root.Get("Sounds").Elements![2].GetOrNull("Sound")?.Type,
            Is.EqualTo(Nwn.Authoring.Documents.NimGff.GffFieldType.ResRef));
        editor.SelectedEntry = editor.Rows[2];
        editor.RemoveCommand.Execute(null);
        Assert.That(store.GetResRefList("Sounds", "Sound"), Is.EqualTo(new[] { validResRefs[1], validResRefs[1] }));
        session.Undo();
        Assert.That(store.GetResRefList("Sounds", "Sound"), Is.EqualTo(new[]
            { validResRefs[1], validResRefs[1], legacyInvalidResRef }));
        var reopened = NativeGffBridge.ToJsonDocument(
            GffReader.Read(GffWriter.Write(NativeGffBridge.ToNativeDocument(document))), true, true);
        Assert.That(reopened.Root.Get("Sounds").Elements![2].GetOrNull("Sound")?.GetString(), Is.EqualTo(legacyInvalidResRef));
        Assert.That(reopened.Root.Get("Sounds").Elements![2].StructId, Is.EqualTo(17u));
        Assert.That(reopened.Root.Get("Sounds").Elements![2].GetOrNull("Future")?.GetString(), Is.EqualTo("retained-invalid"));
        Assert.That(reopened.Root.Get("Sounds").Elements![2].GetOrNull("Sound")?.Type,
            Is.EqualTo(Nwn.Authoring.Documents.NimGff.GffFieldType.ResRef));
    }
}