using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.ContentInjection;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Runs <c>tools/Refactor/SWLOR.ContentInjection</c> over a small repository in a temporary folder, for each case the design lists
/// (MaintainabilityRefactorContentInjection.md, "The master sync"): a static class becomes a content singleton, made content takes its
/// services through its constructor, creation sites become <c>Content.Create</c>, and a master edit merged into a converted file (a
/// new static method that reaches a service, a new service use, a new call to a converted singleton, a new creation of a converted
/// type, a new handler, a new file) is converted in place; a name the class already has becomes <c>&lt;Name&gt;Service</c>; what needs
/// a person is refused; and forward then reverse reproduces a test file byte for byte.
/// </summary>
[TestFixture]
public class ContentInjectionRewriterTests
{
    private static readonly string Services = Lf("""
        { "services": [
          { "name": "Bank", "domain": "Economy" },
          { "name": "Conversation", "domain": "Foundation" },
          { "name": "Currency", "domain": "Economy" },
          { "name": "Faction", "domain": "Quests" },
          { "name": "Item", "domain": "Items" },
          { "name": "Stat", "domain": "Character" }
        ] }
        """);

    private const string ServerProject = "<Project />\n";
    private const string TestProject = "<Project>\n  <ItemGroup>\n    <ProjectReference Include=\"..\\SWLOR.Game.Server\\SWLOR.Game.Server.csproj\" />\n  </ItemGroup>\n</Project>\n";

    private static readonly string PlaceableScripts = Lf("""
        using SWLOR.Game.Server.Core;
        using static SWLOR.Game.Server.Composition.GameServices;

        namespace SWLOR.Game.Server.Feature
        {
            public static class PlaceableScripts
            {
                public const int Cost = 1;

                [NWNEventHandler(ScriptName.OnPlaceableBuyRebuild)]
                public static void PurchaseRebuild()
                {
                    PurchaseRebuild(GetPCSpeaker());
                }

                public static bool PurchaseRebuild(uint player)
                {
                    Currency.TakeCurrency(player, Cost);
                    return Bank.IsFull(player);
                }

                public static int Twice(int value)
                {
                    return value * 2;
                }
            }
        }

        """);

    private static readonly string PlaceableScriptsConverted = Lf("""
        using SWLOR.Game.Server.Core;
        using SWLOR.Game.Server.Domain.Economy;

        namespace SWLOR.Game.Server.Feature
        {
            public class PlaceableScripts
            {
                private IBankService Bank { get; }
                private ICurrencyService Currency { get; }

                public PlaceableScripts(IBankService bank, ICurrencyService currency)
                {
                    Bank = bank;
                    Currency = currency;
                }

                public const int Cost = 1;

                [NWNEventHandler(ScriptName.OnPlaceableBuyRebuild)]
                public void PurchaseRebuild()
                {
                    PurchaseRebuild(GetPCSpeaker());
                }

                public bool PurchaseRebuild(uint player)
                {
                    Currency.TakeCurrency(player, Cost);
                    return Bank.IsFull(player);
                }

                public static int Twice(int value)
                {
                    return value * 2;
                }
            }
        }

        """);

    private static readonly string Payload = Lf("""
        using static SWLOR.Game.Server.Composition.GameServices;

        namespace SWLOR.Game.Server.Feature
        {
            public class ExamineItemPayload
            {
                private readonly uint _item;

                public ExamineItemPayload(uint itemObject, bool trackLiveItem = false)
                {
                    _item = itemObject;
                    Name = Item.GetName(itemObject);
                }

                public string Name { get; }

                public ExamineItemPayload Copy()
                {
                    return new ExamineItemPayload(_item);
                }
            }
        }

        """);

    private static readonly string PayloadConverted = Lf("""
        using SWLOR.Game.Server.Domain.Items;

        namespace SWLOR.Game.Server.Feature
        {
            public class ExamineItemPayload
            {
                private IItemService Item { get; }

                private readonly uint _item;

                public ExamineItemPayload(IItemService item, uint itemObject, bool trackLiveItem = false)
                {
                    Item = item;
                    _item = itemObject;
                    Name = Item.GetName(itemObject);
                }

                public string Name { get; }

                public ExamineItemPayload Copy()
                {
                    return new ExamineItemPayload(Item, _item);
                }
            }
        }

        """);

    private static readonly string ViewModel = Lf("""
        namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
        {
            public class ExamineViewModel
            {
                public object Build(uint item)
                {
                    return new ExamineItemPayload(item);
                }
            }
        }

        """);

    private static readonly string FixtureWithContainer = Lf("""
        using NUnit.Framework;
        using SWLOR.Game.Server.Feature;
        using SWLOR.Game.Server.Tests.Support;

        namespace SWLOR.Game.Server.Tests.Feature
        {
            public class ExamineItemPayloadTests
            {
                private FacadeContainer _container = null!;

                [Test]
                public void Builds()
                {
                    var payload = new ExamineItemPayload(5u);
                    var nested = new Holder(new ExamineItemPayload(6u), 2);
                    PlaceableScripts.PurchaseRebuild(1u);
                }
            }
        }

        """);

    private const string FixtureWithoutContainer = "using NUnit.Framework;\r\nusing SWLOR.Game.Server.Feature;\r\nusing SWLOR.Game.Server.Tests.Support;\r\n\r\n" +
                                                   "namespace SWLOR.Game.Server.Tests.Feature;\r\n\r\npublic class PayloadWithoutContainerTests\r\n{\r\n" +
                                                   "    [Test]\r\n    public void Builds()\r\n    {\r\n        var payload = new ExamineItemPayload(5u, true);\r\n    }\r\n}\r\n";

    private string _root = null!;

    [SetUp]
    public void CreateRepository()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-content-injection-" + Guid.NewGuid().ToString("N")[..8]);
        Write("SWLOR.Game.Server.sln", string.Empty);
        Write("SWLOR.Game.Server/SWLOR.Game.Server.csproj", ServerProject);
        Write(ContentInjectionRunner.MarkerPath, "namespace SWLOR.Game.Server.Core { public interface IContentFactory { } }\n");
        Write(NameTable.ConvertedServicesPath, Services);
        Write("SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj", TestProject);
        Converted();
    }

    [TearDown]
    public void DeleteRepository()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void A_static_class_that_reaches_services_becomes_a_registered_singleton_and_a_second_run_changes_nothing()
    {
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);
        Converted("SWLOR.Game.Server/Feature/PlaceableScripts.cs");

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        Text("SWLOR.Game.Server/Feature/PlaceableScripts.cs").Should().Be(PlaceableScriptsConverted);
        Text(NameTable.RegistrationsPath).Should().Contain("            services.AddSingleton<SWLOR.Game.Server.Feature.PlaceableScripts>();\n");
        RunTwiceChangesNothing();
    }

    [Test]
    public void Made_content_takes_its_services_first_copies_itself_with_them_and_its_creators_use_Content()
    {
        Write("SWLOR.Game.Server/Feature/ExamineItemPayload.cs", Payload);
        Write("SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/ExamineViewModel.cs", ViewModel);
        Converted("SWLOR.Game.Server/Feature/ExamineItemPayload.cs");

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        Text("SWLOR.Game.Server/Feature/ExamineItemPayload.cs").Should().Be(PayloadConverted);
        Text("SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/ExamineViewModel.cs").Should().Be(
            "using static SWLOR.Game.Server.Composition.GameServices;\n\n" +
            ViewModel.Replace("new ExamineItemPayload(item)", "Content.Create<ExamineItemPayload, uint>(item)"),
            "code not converted yet creates converted content through the temporary GameServices.Content");
        Text(NameTable.RegistrationsPath).Should().Contain("yield return new ContentSignature(typeof(SWLOR.Game.Server.Feature.ExamineItemPayload), typeof(uint));");
        RunTwiceChangesNothing();
    }

    [Test]
    public void A_new_static_method_a_new_handler_and_a_new_service_use_in_a_converted_singleton_are_converted_in_place()
    {
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);
        Converted("SWLOR.Game.Server/Feature/PlaceableScripts.cs");
        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        // Master's edit merged into the converted file: a new handler and a new helper that reach a service the class did not use,
        // after the facade removal added its import back.
        var merged = PlaceableScriptsConverted
            .Replace("using SWLOR.Game.Server.Domain.Economy;\n", "using SWLOR.Game.Server.Domain.Economy;\nusing static SWLOR.Game.Server.Composition.GameServices;\n")
            .Replace("        public static int Twice(int value)", Lf("""
                    [NWNEventHandler(ScriptName.OnPlaceableUsed)]
                    public static void Used()
                    {
                        Refresh(OBJECT_SELF);
                    }

                    public static void Refresh(uint creature)
                    {
                        Stat.Refresh(creature);
                    }

                    public static int Twice(int value)
            """));
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", merged);

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        var text = Text("SWLOR.Game.Server/Feature/PlaceableScripts.cs");
        text.Should().NotContain("using static", "the import goes once every lookup is a property");
        text.Should().Contain("using SWLOR.Game.Server.Domain.Character;\n");
        text.Should().Contain("        private ICurrencyService Currency { get; }\n        private IStatService Stat { get; }\n\n");
        text.Should().Contain("public PlaceableScripts(IBankService bank, ICurrencyService currency, IStatService stat)");
        text.Should().Contain("            Currency = currency;\n            Stat = stat;\n        }");
        text.Should().Contain("        public void Used()").And.Contain("        public void Refresh(uint creature)");
        text.Should().Contain("        public static int Twice(int value)", "a member that reaches no service stays static");
        RunTwiceChangesNothing();
    }

    [Test]
    public void A_new_call_to_a_converted_singleton_injects_it_and_a_new_file_in_a_converted_folder_is_converted_whole()
    {
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);
        Write("SWLOR.Game.Server/Feature/DialogDefinition/RebuildDialog.cs", Lf("""
            using static SWLOR.Game.Server.Composition.GameServices;

            namespace SWLOR.Game.Server.Feature.DialogDefinition
            {
                public class RebuildDialog
                {
                    public void Confirm(uint player)
                    {
                        if (PlaceableScripts.PurchaseRebuild(player))
                            Conversation.End(player);
                    }
                }
            }

            """));
        Converted("SWLOR.Game.Server/Feature/DialogDefinition/", "SWLOR.Game.Server/Feature/PlaceableScripts.cs");

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        Text("SWLOR.Game.Server/Feature/DialogDefinition/RebuildDialog.cs").Should().Be(Lf("""
            using SWLOR.Game.Server.Domain.Foundation;

            namespace SWLOR.Game.Server.Feature.DialogDefinition
            {
                public class RebuildDialog
                {
                    private IConversationService Conversation { get; }
                    private PlaceableScripts PlaceableScripts { get; }

                    public RebuildDialog(IConversationService conversation, PlaceableScripts placeableScripts)
                    {
                        Conversation = conversation;
                        PlaceableScripts = placeableScripts;
                    }

                    public void Confirm(uint player)
                    {
                        if (PlaceableScripts.PurchaseRebuild(player))
                            Conversation.End(player);
                    }
                }
            }

            """), "a converted singleton is injected under its class name, so the call keeps its text");
        RunTwiceChangesNothing();
    }

    [Test]
    public void A_new_creation_of_a_converted_type_in_a_converted_class_gets_Content_injected()
    {
        Write("SWLOR.Game.Server/Feature/ExamineItemPayload.cs", Payload);
        Write("SWLOR.Game.Server/Feature/ItemExamine.cs", Lf("""
            namespace SWLOR.Game.Server.Feature
            {
                public static class ItemExamine
                {
                    public static object Open(uint item)
                    {
                        Stat.Refresh(item);
                        return new ExamineItemPayload(item, true);
                    }
                }
            }

            """));
        Converted("SWLOR.Game.Server/Feature/ExamineItemPayload.cs", "SWLOR.Game.Server/Feature/ItemExamine.cs");

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        var text = Text("SWLOR.Game.Server/Feature/ItemExamine.cs");
        text.Should().StartWith("using SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Domain.Character;\n\n");
        text.Should().Contain("        private IContentFactory Content { get; }\n        private IStatService Stat { get; }\n");
        text.Should().Contain("public ItemExamine(IContentFactory content, IStatService stat)");
        text.Should().Contain("return Content.Create<ExamineItemPayload, uint, bool>(item, true);");
        text.Should().Contain("public object Open(uint item)");
        RunTwiceChangesNothing();
    }

    [Test]
    public void A_name_the_class_already_has_becomes_its_Service_property_and_only_the_explicit_sites_change()
    {
        Write("SWLOR.Game.Server/Feature/QuestReward/FactionReward.cs", Lf("""
            using SWLOR.Game.Server.Composition;

            namespace SWLOR.Game.Server.Feature.QuestReward
            {
                public class FactionReward
                {
                    public FactionReward(int faction)
                    {
                        Faction = faction;
                    }

                    public int Faction { get; }

                    public string Describe() => GameServices.Faction.GetName(Faction);
                }
            }

            """));
        Converted("SWLOR.Game.Server/Feature/QuestReward/");

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        var text = Text("SWLOR.Game.Server/Feature/QuestReward/FactionReward.cs");
        text.Should().Contain("        private IFactionService FactionService { get; }\n");
        text.Should().Contain("public FactionReward(IFactionService factionService, int faction)");
        text.Should().Contain("            FactionService = factionService;\n            Faction = faction;");
        text.Should().Contain("public string Describe() => FactionService.GetName(Faction);");
        RunTwiceChangesNothing();
    }

    [Test]
    public void A_test_gets_fixture_properties_from_its_container_and_forward_then_reverse_reproduces_it_byte_for_byte()
    {
        Write("SWLOR.Game.Server/Feature/ExamineItemPayload.cs", Payload);
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);
        Write("SWLOR.Game.Server/Feature/Holder.cs", "namespace SWLOR.Game.Server.Feature\n{\n    public class Holder\n    {\n        public Holder(ExamineItemPayload payload, int count)\n        {\n        }\n    }\n}\n");
        var withBom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(FixtureWithContainer.Replace("\n", "\r\n"))).ToArray();
        Write("SWLOR.Game.Server.Tests/Feature/ExamineItemPayloadTests.cs", withBom);
        Write("SWLOR.Game.Server.Tests/Feature/PayloadWithoutContainerTests.cs", FixtureWithoutContainer);
        Converted("SWLOR.Game.Server/Feature/ExamineItemPayload.cs", "SWLOR.Game.Server/Feature/PlaceableScripts.cs");

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        var withContainer = Text("SWLOR.Game.Server.Tests/Feature/ExamineItemPayloadTests.cs");
        withContainer.Should().Contain("    {\r\n        private SWLOR.Game.Server.Core.IContentFactory Content => _container.Content;\r\n\r\n" +
                                       "        private PlaceableScripts PlaceableScripts => _container.Get<PlaceableScripts>();\r\n\r\n");
        withContainer.Should().Contain("var payload = Content.Create<ExamineItemPayload, uint>(5u);");
        withContainer.Should().Contain("var nested = new Holder(Content.Create<ExamineItemPayload, uint>(6u), 2);");
        Text("SWLOR.Game.Server.Tests/Feature/PayloadWithoutContainerTests.cs").Should().Contain(
            "{\r\n    private SWLOR.Game.Server.Core.IContentFactory Content { get; } = TestContent.WithoutServices;\r\n\r\n    [Test]");
        Text("SWLOR.Game.Server.Tests/Feature/PayloadWithoutContainerTests.cs").Should().Contain("Content.Create<ExamineItemPayload, uint, bool>(5u, true)");

        // The parity copy reverses the tests on master's tree, which has no IContentFactory.
        File.Delete(Path.Combine(_root, ContentInjectionRunner.MarkerPath));
        ContentInjectionRunner.Run(new RunOptions(RunMode.Reverse, Path.Combine(_root, "SWLOR.Game.Server.Tests"), string.Empty), TextWriter.Null)
            .Should().Be(ContentInjectionRunner.Ok);

        Bytes("SWLOR.Game.Server.Tests/Feature/ExamineItemPayloadTests.cs").Should().Equal(withBom);
        Text("SWLOR.Game.Server.Tests/Feature/PayloadWithoutContainerTests.cs").Should().Be(FixtureWithoutContainer);
    }

    [Test]
    public void Singletons_that_need_each_other_are_refused_and_left_as_they_were()
    {
        const string first = "namespace SWLOR.Game.Server.Feature\n{\n    public static class First\n    {\n        public static void Do() { Second.Work(); Bank.IsFull(1); }\n    }\n}\n";
        const string second = "namespace SWLOR.Game.Server.Feature\n{\n    public static class Second\n    {\n        public static void Work() { First.Do(); Stat.Refresh(1); }\n    }\n}\n";
        Write("SWLOR.Game.Server/Feature/First.cs", first);
        Write("SWLOR.Game.Server/Feature/Second.cs", second);
        Converted("SWLOR.Game.Server/Feature/First.cs", "SWLOR.Game.Server/Feature/Second.cs");

        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(ContentInjectionRunner.Refused);

        log.ToString().Should().Contain("The content singletons First, Second need each other");
        Text("SWLOR.Game.Server/Feature/First.cs").Should().Be(first);
        Text("SWLOR.Game.Server/Feature/Second.cs").Should().Be(second);
        Text(NameTable.RegistrationsPath).Should().NotContain("AddSingleton<", "a refused singleton is not registered");
    }

    [Test]
    public void What_needs_a_person_is_listed_and_its_file_left_alone()
    {
        Write("SWLOR.Game.Server/Feature/ExamineItemPayload.cs", Payload);
        const string named = "namespace SWLOR.Game.Server.Feature\n{\n    public class Named\n    {\n        public object A() => new ExamineItemPayload(itemObject: 5u);\n    }\n}\n";
        const string initialized = "namespace SWLOR.Game.Server.Feature\n{\n    public class Initialized\n    {\n        public object A() => new ExamineItemPayload(5u) { };\n    }\n}\n";
        const string state = "namespace SWLOR.Game.Server.Feature\n{\n    public static class Stateful\n    {\n        private static int _count;\n        public static void Count() { _count++; Bank.IsFull(1); }\n    }\n}\n";
        const string madeStatic = "namespace SWLOR.Game.Server.Feature\n{\n    public class MadeWithStatic\n    {\n        public int Value { get; set; }\n        public static void Handler() { Bank.IsFull(1); }\n    }\n}\n";
        const string caller = "namespace SWLOR.Game.Server.Feature\n{\n    public static class Caller\n    {\n        public static void Call() { PlaceableScripts.PurchaseRebuild(1); }\n    }\n}\n";
        const string clash = "namespace SWLOR.Game.Server.Feature\n{\n    public class Clash\n    {\n        public Clash(uint item) { }\n        public void Use() { Item.GetName(1); }\n    }\n}\n";
        Write("SWLOR.Game.Server/Feature/Named.cs", named);
        Write("SWLOR.Game.Server/Feature/Initialized.cs", initialized);
        Write("SWLOR.Game.Server/Feature/Stateful.cs", state);
        Write("SWLOR.Game.Server/Feature/MadeWithStatic.cs", madeStatic);
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);
        Write("SWLOR.Game.Server/Domain/Caller.cs", caller);
        Write("SWLOR.Game.Server/Feature/Clash.cs", clash);
        Converted("SWLOR.Game.Server/Feature/Clash.cs", "SWLOR.Game.Server/Feature/ExamineItemPayload.cs", "SWLOR.Game.Server/Feature/MadeWithStatic.cs",
            "SWLOR.Game.Server/Feature/PlaceableScripts.cs", "SWLOR.Game.Server/Feature/Stateful.cs");

        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(ContentInjectionRunner.Refused);

        var output = log.ToString();
        output.Should().Contain("SWLOR.Game.Server/Feature/Named.cs:5: new ExamineItemPayload(...) with named arguments");
        output.Should().Contain("SWLOR.Game.Server/Feature/Initialized.cs:5: new ExamineItemPayload with an object initializer");
        output.Should().Contain("Stateful._count is runtime state of a content singleton");
        output.Should().Contain("The static member MadeWithStatic.Handler of made content reaches a service");
        output.Should().Contain("SWLOR.Game.Server/Domain/Caller.cs:5: PlaceableScripts.PurchaseRebuild becomes an instance member");
        output.Should().Contain("already has a parameter named item");
        Text("SWLOR.Game.Server/Feature/Named.cs").Should().Be(named);
        Text("SWLOR.Game.Server/Feature/Initialized.cs").Should().Be(initialized);
        Text("SWLOR.Game.Server/Feature/Stateful.cs").Should().Be(state);
        Text("SWLOR.Game.Server/Feature/MadeWithStatic.cs").Should().Be(madeStatic);
        Text("SWLOR.Game.Server/Feature/Clash.cs").Should().Be(clash);
        Text("SWLOR.Game.Server/Feature/PlaceableScripts.cs").Should().Be(PlaceableScriptsConverted, "a file without a refusal still converts");
    }

    [Test]
    public void More_than_ten_services_are_refused()
    {
        var names = Enumerable.Range(1, 11).Select(index => $"S{index:00}").ToArray();
        Write(NameTable.ConvertedServicesPath, "{ \"services\": [" + string.Join(",", names.Select(name => $"{{ \"name\": \"{name}\", \"domain\": \"World\" }}")) + "] }");
        var calls = string.Concat(names.Select(name => $" {name}.Do();"));
        Write("SWLOR.Game.Server/Feature/Wide.cs", "namespace SWLOR.Game.Server.Feature\n{\n    public static class Wide\n    {\n        public static void All() {" + calls + " }\n    }\n}\n");
        Converted("SWLOR.Game.Server/Feature/Wide.cs");

        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(ContentInjectionRunner.Refused);
        log.ToString().Should().Contain("Wide would take 11 services, more than 10");
    }

    [Test]
    public void A_tree_without_the_content_factory_is_master_and_is_left_alone()
    {
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);
        Converted("SWLOR.Game.Server/Feature/PlaceableScripts.cs");
        File.Delete(Path.Combine(_root, ContentInjectionRunner.MarkerPath));
        var before = Snapshot();

        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);
        Run(RunMode.Check).Should().Be(ContentInjectionRunner.Ok);

        Snapshot().Should().BeEquivalentTo(before);
    }

    [Test]
    public void With_nothing_converted_only_the_registrations_are_written_and_the_check_reports_a_stale_file()
    {
        Write("SWLOR.Game.Server/Feature/PlaceableScripts.cs", PlaceableScripts);

        Run(RunMode.Check).Should().Be(ContentInjectionRunner.Differs, "the registrations file does not exist yet");
        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);

        Text("SWLOR.Game.Server/Feature/PlaceableScripts.cs").Should().Be(PlaceableScripts);
        Text(NameTable.RegistrationsPath).Should().Contain("internal static void Register(IServiceCollection services)\n        {\n        }")
            .And.Contain("yield break;");
        Run(RunMode.Check).Should().Be(ContentInjectionRunner.Ok);
    }

    [Test]
    public void The_branch_is_in_the_tools_shape()
    {
        var repository = RepositoryLocator.Find(TestContext.CurrentContext.TestDirectory)!;
        var log = new StringWriter();

        var result = ContentInjectionRunner.Run(new RunOptions(RunMode.Check, repository, Path.Combine(repository, "tools", "Refactor", "ContentInjection.json")), log);

        result.Should().Be(ContentInjectionRunner.Ok, log.ToString());
    }

    private void RunTwiceChangesNothing()
    {
        var after = Snapshot();
        Run(RunMode.Apply).Should().Be(ContentInjectionRunner.Ok);
        Snapshot().Should().BeEquivalentTo(after, "a second run changes nothing");
        Run(RunMode.Check).Should().Be(ContentInjectionRunner.Ok, "the check is clean after an apply");
    }

    private int Run(RunMode mode, TextWriter? log = null) =>
        ContentInjectionRunner.Run(new RunOptions(mode, _root, Path.Combine(_root, "tools", "Refactor", "ContentInjection.json")), log ?? TextWriter.Null);

    private void Converted(params string[] paths) =>
        Write("tools/Refactor/ContentInjection.json", "{ \"converted\": [" + string.Join(", ", paths.OrderBy(path => path, StringComparer.Ordinal).Select(path => $"\"{path}\"")) + "] }");

    private void Write(string relative, string text) => Write(relative, Encoding.Latin1.GetBytes(text));

    /// <summary>A raw string literal with LF line endings, whatever line endings the checkout gave this file.</summary>
    private static string Lf(string text) => text.Replace("\r\n", "\n");

    private void Write(string relative, byte[] bytes)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    private string Text(string relative) => SourceFile.Read(Path.Combine(_root, relative)).Text;

    private byte[] Bytes(string relative) => File.ReadAllBytes(Path.Combine(_root, relative));

    private Dictionary<string, string> Snapshot() =>
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(_root, path), path => Convert.ToBase64String(File.ReadAllBytes(path)));
}
