using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="BlueprintTemplateFactory"/>. The load-bearing test is
    /// <see cref="CreateFileContent_CarriesEveryFieldTheCorpusAlwaysHas"/>: it derives each type's
    /// de-facto required field set from the real module corpus (the fields present in every sampled
    /// blueprint of that type) and holds the template to it, because a blueprint missing one of
    /// those is something neither the game nor this toolset's editors should be handed.
    /// </summary>
    public class BlueprintTemplateFactoryTests
    {
        private const int CorpusSampleSize = 200;

        private static readonly ResourceType[] SupportedTypes =
        {
            ResourceType.Utc, ResourceType.Uti, ResourceType.Utp, ResourceType.Utd,
            ResourceType.Utm, ResourceType.Utt, ResourceType.Uts, ResourceType.Utw
        };

        private static readonly ResourceType[] UnsupportedTypes =
        {
            ResourceType.Area, ResourceType.Dlg, ResourceType.Nss
        };

        /// <summary>Per type: the resref field, then the localized-name field the display name lands in.</summary>
        private static readonly Dictionary<ResourceType, (string ResRefField, string NameField)> IdentityFields = new()
        {
            [ResourceType.Utc] = ("TemplateResRef", "FirstName"),
            [ResourceType.Uti] = ("TemplateResRef", "LocalizedName"),
            [ResourceType.Utp] = ("TemplateResRef", "LocName"),
            [ResourceType.Utd] = ("TemplateResRef", "LocName"),
            [ResourceType.Utm] = ("ResRef", "LocName"),
            [ResourceType.Utt] = ("TemplateResRef", "LocalizedName"),
            [ResourceType.Uts] = ("TemplateResRef", "LocName"),
            [ResourceType.Utw] = ("TemplateResRef", "LocalizedName")
        };

        [Test]
        public void Supports_CoversExactlyTheBlueprintTypes()
        {
            foreach (var type in Enum.GetValues<ResourceType>())
            {
                BlueprintTemplateFactory.Supports(type).Should().Be(
                    SupportedTypes.Contains(type),
                    "Supports must agree with the templates the factory actually implements ({0})", type);
            }
        }

        [TestCaseSource(nameof(UnsupportedTypes))]
        public void CreateFileContent_RejectsUnsupportedType(ResourceType type)
        {
            BlueprintTemplateFactory.Supports(type).Should().BeFalse();

            var act = () => BlueprintTemplateFactory.CreateFileContent(type, "probe_resref", "Probe");
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCaseSource(nameof(SupportedTypes))]
        public void CreateFileContent_RoundTripsThroughItsDocumentType(ResourceType type)
        {
            var content = BlueprintTemplateFactory.CreateFileContent(type, "probe_resref", "Probe Blueprint");

            var document = Parse(type, content).Document;
            document.DataType.Should().Be($"{type.Extension().ToUpperInvariant()} ");

            document.ToBytes().Should().Equal(content, "reparsing and reserializing must be lossless");

            // The unpack pipeline's convention: LF body, single CRLF terminator.
            var text = Encoding.ASCII.GetString(content);
            text.Should().EndWith("}\r\n");
            text.Split('\r').Should().HaveCount(2, "only the file terminator is a CRLF");
        }

        [TestCaseSource(nameof(SupportedTypes))]
        public void CreateFileContent_PutsResRefAndDisplayNameInTheRightFields(ResourceType type)
        {
            var (resRefField, nameField) = IdentityFields[type];

            var root = Parse(type, BlueprintTemplateFactory.CreateFileContent(type, "probe_resref", "Probe Blueprint")).Fields;

            root.Get(resRefField).GetString().Should().Be("probe_resref");
            root.Get(resRefField).Type.Should().Be(GffFieldType.ResRef);
            root.Get("Tag").GetString().Should().Be("probe_resref", "the corpus defaults a blueprint's tag to its resref");
            LanguageZeroText(root, nameField).Should().Be("Probe Blueprint");
        }

        [TestCaseSource(nameof(SupportedTypes))]
        public void CreateFileContent_FallsBackToResRefWhenNoDisplayNameGiven(ResourceType type)
        {
            var root = Parse(type, BlueprintTemplateFactory.CreateFileContent(type, "probe_resref", "   ")).Fields;

            LanguageZeroText(root, IdentityFields[type].NameField).Should().Be("probe_resref");
        }

        [TestCaseSource(nameof(SupportedTypes))]
        public void CreateFileContent_CarriesEveryFieldTheCorpusAlwaysHas(ResourceType type)
        {
            var required = RequiredCorpusFields(type);
            required.Should().NotBeEmpty("the corpus sample must actually contribute a required set");

            var generated = Parse(type, BlueprintTemplateFactory.CreateFileContent(type, "probe_resref", "Probe"))
                .Fields.Entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

            var missing = required.Except(generated).OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
            missing.Should().BeEmpty(
                "a new {0} blueprint must carry every field every real one carries", type.Extension());
        }

        [Test]
        public void CreateFileContent_Creature_IsASpawnableLevelOneCreature()
        {
            var root = Parse(ResourceType.Utc,
                BlueprintTemplateFactory.CreateFileContent(ResourceType.Utc, "probe_resref", "Probe")).Fields;

            var classEntry = root.Get("ClassList").Elements!.Single();
            Encoding.ASCII.GetString(classEntry.RawStructId!).Should().Be("2", "ClassList entries use __struct_id 2");
            classEntry.Get("ClassLevel").GetInteger().Should().Be(1);
            root.Get("ClassList").Elements!.Should().NotBeEmpty("a class-less creature has no level");
            root.Get("SkillList").Elements!.Should().HaveCount(28, "the engine expects one rank per skills.2da row");
            root.Get("HitPoints").GetInteger().Should().BeGreaterThan(0, "a 0 HP creature spawns dead");
            root.Get("Str").GetInteger().Should().Be(10, "ability scores start at the neutral baseline");
            root.Get("LastName").LocStringEntries!.Should().BeEmpty("unset localized text carries no language entry");
        }

        [Test]
        public void CreateFileContent_Item_StartsWithSynchronizedEngineDescriptions()
        {
            var root = Parse(
                ResourceType.Uti,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    "probe_item",
                    "Probe Item")).Fields;

            root.GetIntOrNull("Identified").Should().Be(1);
            root.Get("Description").LocStringEntries.Should().BeEmpty();
            root.Get("DescIdentified").LocStringEntries.Should().BeEmpty();
        }

        [Test]
        public void CreateFileContent_Merchant_UsesSwlorDefaultsAndFiveInventoryPanes()
        {
            var root = Parse(
                ResourceType.Utm,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_resref", "Probe Merchant")).Fields;

            root.GetListOrEmpty("StoreList").Should().HaveCount(5);
            root.GetIntOrNull("IdentifyPrice").Should().Be(0);
            root.GetIntOrNull("BlackMarket").Should().Be(1);
            root.GetIntOrNull("MaxBuyPrice").Should().Be(-1);
            root.GetIntOrNull("StoreGold").Should().Be(-1);
            root.GetStringOrNull("OnOpenStore").Should().Be("on_open_store");
            root.GetStringOrNull("OnStoreClosed").Should().Be("on_close_store");
            root.GetIntOrNull("ID").Should().Be(5);
        }

        /// <summary>
        /// The field names present in every blueprint of a corpus sample - the type's de-facto
        /// required set. The sample is spread evenly across the alphabetically sorted corpus rather
        /// than taken from its head, so it is not dominated by one naming prefix.
        /// </summary>
        private static HashSet<string> RequiredCorpusFields(ResourceType type)
        {
            HashSet<string>? required = null;
            foreach (var path in SampleCorpusFiles(type))
            {
                var names = JsonGffDocument.Load(path).Root.Entries
                    .Select(entry => entry.Key)
                    .ToHashSet(StringComparer.Ordinal);

                if (required == null)
                    required = names;
                else
                    required.IntersectWith(names);
            }

            return required ?? new HashSet<string>(StringComparer.Ordinal);
        }

        private static IEnumerable<string> SampleCorpusFiles(ResourceType type)
        {
            var extension = type.Extension();
            var files = Directory.GetFiles(
                Path.Combine(CorpusLocator.ModuleDirectory, extension), $"*.{extension}.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            if (files.Length <= CorpusSampleSize)
                return files;

            var step = files.Length / CorpusSampleSize;
            return Enumerable.Range(0, CorpusSampleSize).Select(index => files[index * step]);
        }

        private static string? LanguageZeroText(JsonGffStruct root, string fieldName)
        {
            var field = root.Get(fieldName);
            field.Type.Should().Be(GffFieldType.CExoLocString);
            return field.LocStringEntries!.Single(entry => entry.LanguageKey == "0").GetText();
        }

        private static GffDocumentBase Parse(ResourceType type, byte[] content)
        {
            return type switch
            {
                ResourceType.Utc => UtcDocument.Parse(content),
                ResourceType.Uti => UtiDocument.Parse(content),
                ResourceType.Utp => UtpDocument.Parse(content),
                ResourceType.Utd => UtdDocument.Parse(content),
                ResourceType.Utm => UtmDocument.Parse(content),
                ResourceType.Utt => UttDocument.Parse(content),
                ResourceType.Uts => UtsDocument.Parse(content),
                ResourceType.Utw => UtwDocument.Parse(content),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Not a blueprint type.")
            };
        }
    }
}
