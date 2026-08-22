using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests
{
    public class ScriptUsageIndexTests
    {
        [Test]
        public async Task InvalidatingTheCacheBuildsANewLazyGeneration()
        {
            var builds = 0;
            var cache = new ScriptUsageIndexCache(() =>
            {
                Interlocked.Increment(ref builds);
                return null;
            });

            await cache.GetAsync();
            await cache.GetAsync();
            builds.Should().Be(1);

            cache.Invalidate();
            await cache.GetAsync();

            builds.Should().Be(2);
        }

        [Test]
        public void BuildFindsNestedDialogScriptsAndPlacedInstanceOverrides()
        {
            using var module = SyntheticModule.Create();

            var dialogRoot = new JsonGffStruct();
            dialogRoot.Add("EndConversation", ResRef("dlg_end"));
            dialogRoot.Add("EndConverAbort", ResRef("dlg_abort"));
            var entries = JsonGffField.CreateList();
            var entry = new JsonGffStruct();
            entry.Add("Script", ResRef("dlg_action"));
            var replies = JsonGffField.CreateList();
            var reply = new JsonGffStruct();
            reply.Add("Active", ResRef("dlg_condition"));
            replies.Elements!.Add(reply);
            entry.Add("RepliesList", replies);
            entries.Elements!.Add(entry);
            dialogRoot.Add("EntryList", entries);
            Write(
                module,
                "dlg",
                "conversation.dlg.json",
                new JsonGffDocument("DLG ", dialogRoot));

            module.WriteAreaStub("test_area");
            var gitRoot = new JsonGffStruct();
            var placeables = JsonGffField.CreateList();
            var placed = new JsonGffStruct();
            placed.Add("OnUsed", ResRef("placed_override"));
            placeables.Elements!.Add(placed);
            gitRoot.Add("Placeable List", placeables);
            Write(
                module,
                "git",
                "test_area.git.json",
                new JsonGffDocument("GIT ", gitRoot));

            var index = ScriptUsageIndex.Build(module.Workspace);

            index.UsagesOf("dlg_action").Should().ContainSingle(usage =>
                usage.ResourceType == ResourceType.Dlg &&
                usage.ResRef == "conversation" &&
                usage.FieldName == "EntryList[0].Script");
            index.UsagesOf("dlg_condition").Should().ContainSingle(usage =>
                usage.ResourceType == ResourceType.Dlg &&
                usage.FieldName == "EntryList[0].RepliesList[0].Active");
            index.UsagesOf("dlg_end").Should().ContainSingle(usage =>
                usage.ResourceType == ResourceType.Dlg &&
                usage.FieldName == "EndConversation");
            index.UsagesOf("dlg_abort").Should().ContainSingle(usage =>
                usage.ResourceType == ResourceType.Dlg &&
                usage.FieldName == "EndConverAbort");
            index.UsagesOf("placed_override").Should().ContainSingle(usage =>
                usage.ResourceType == ResourceType.Area &&
                usage.ResRef == "test_area" &&
                usage.FieldName == "Placeable List[0].OnUsed");
        }

        private static JsonGffField ResRef(string value)
        {
            var field = JsonGffField.CreateScalar(GffFieldType.ResRef, Array.Empty<byte>());
            field.SetString(value);
            return field;
        }

        private static void Write(
            SyntheticModule module,
            string folder,
            string fileName,
            JsonGffDocument document)
        {
            var directory = Path.Combine(module.Path, folder);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), document.ToBytes());
        }
    }
}
