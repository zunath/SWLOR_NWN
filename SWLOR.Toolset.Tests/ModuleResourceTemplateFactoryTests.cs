using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// <see cref="ModuleResourceTemplateFactory"/>: the files Module Contents' "New Conversation..." and
    /// "New Script..." write.
    /// </summary>
    /// <remarks>
    /// A new conversation has to be a conversation the engine can start, which is more than valid GFF:
    /// StartingList has to point at an entry that exists, and the entry has to carry the fields every
    /// .dlg in the corpus carries. That is what these assert, alongside the round trip.
    /// </remarks>
    [TestFixture]
    public class ModuleResourceTemplateFactoryTests
    {
        /// <summary>Root fields every one of the module's 609 conversations carries.</summary>
        private static readonly string[] RequiredRootFields =
        {
            "DelayEntry", "DelayReply", "EndConverAbort", "EndConversation",
            "EntryList", "NumWords", "PreventZoomIn", "ReplyList", "StartingList"
        };

        /// <summary>Fields every entry in the corpus carries.</summary>
        private static readonly string[] RequiredEntryFields =
        {
            "Animation", "AnimLoop", "Comment", "Delay", "Quest",
            "RepliesList", "Script", "Sound", "Speaker", "Text"
        };

        [Test]
        public void Supports_CoversExactlyConversationsAndScripts()
        {
            foreach (var type in Enum.GetValues<ResourceType>())
            {
                ModuleResourceTemplateFactory.Supports(type)
                    .Should().Be(type is ResourceType.Dlg or ResourceType.Nss, because: $"of {type}");
            }
        }

        [Test]
        public void CreateFileContent_RejectsATypeItHasNoTemplateFor()
        {
            var act = () => ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Area, "probe", "Probe");
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void CreateFileContent_RejectsABlankResRef()
        {
            var act = () => ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Dlg, "  ", "Probe");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void CreateFileContent_Conversation_RoundTripsAsADlgDocument()
        {
            var document = ParseConversation();

            document.DataType.Should().Be("DLG ");
            document.Root.Entries.Should().NotBeEmpty();
        }

        [Test]
        public void CreateFileContent_Conversation_CarriesEveryFieldTheCorpusAlwaysHas()
        {
            var root = ParseConversation().Root;

            foreach (var field in RequiredRootFields)
                root.Contains(field).Should().BeTrue(because: $"every .dlg carries '{field}'");

            var entry = root.Get("EntryList").Elements!.Single();
            foreach (var field in RequiredEntryFields)
                entry.Contains(field).Should().BeTrue(because: $"every dialogue entry carries '{field}'");
        }

        /// <summary>
        /// The one structural rule: StartingList indexes into EntryList, so an empty EntryList would
        /// make the conversation unstartable rather than merely blank.
        /// </summary>
        [Test]
        public void CreateFileContent_Conversation_StartsAtAnEntryThatExists()
        {
            var root = ParseConversation().Root;

            var entries = root.Get("EntryList").Elements!;
            var start = root.Get("StartingList").Elements!.Single();

            start.Get("Index").GetInteger().Should().BeLessThan(entries.Count);
        }

        [Test]
        public void CreateFileContent_Conversation_OpensWithThePlaceholderLine()
        {
            var entry = ParseConversation().Root.Get("EntryList").Elements!.Single();

            entry.Get("Text").LocStringEntries!.Single(text => text.LanguageKey == "0").GetText()
                .Should().Be(ModuleResourceTemplateFactory.PlaceholderEntryText);
        }

        /// <summary>
        /// A delay of 0 is a zero-second delay, which the engine skips through; the corpus's "no delay"
        /// is 0xFFFFFFFF. Asserted because a plain 0 would look correct and read wrong in game.
        /// </summary>
        [Test]
        public void CreateFileContent_Conversation_UsesTheCorpusNoDelaySentinel()
        {
            var entry = ParseConversation().Root.Get("EntryList").Elements!.Single();

            entry.Get("Delay").GetUnsignedInteger().Should().Be(uint.MaxValue);
        }

        [Test]
        public void CreateFileContent_Script_IsCompilableAndNamed()
        {
            var source = Encoding.UTF8.GetString(
                ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Nss, "probe_script", "Probe Script"));

            source.Should().Contain("void main()");
            source.Should().Contain("Probe Script");
        }

        [Test]
        public void CreateFileContent_Script_FallsBackToTheResRefWhenUnnamed()
        {
            var source = Encoding.UTF8.GetString(
                ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Nss, "probe_script", string.Empty));

            source.Should().Contain("probe_script");
        }

        private static JsonGffDocument ParseConversation() =>
            JsonGffDocument.Parse(
                ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Dlg, "probe_talk", "Probe Talk"));
    }
}
