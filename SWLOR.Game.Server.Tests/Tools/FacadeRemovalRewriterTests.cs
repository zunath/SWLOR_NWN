using FluentAssertions;
using NUnit.Framework;
using SWLOR.FacadeRemoval;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Proves what <c>tools/Refactor/SWLOR.FacadeRemoval</c> does to one file: a call into a removed facade keeps its text
/// and gains the <c>GameServices</c> import, a qualified call loses its qualifier, an explicit name and a moved constant
/// are respelled, nothing else changes, a second run changes nothing, and the reversal gives back the original text.
/// </summary>
[TestFixture]
public class FacadeRemovalRewriterTests
{
    private static readonly RemovedFacade Taxi = new("Taxi", false, []);
    private static readonly RemovedFacade Combat = new("Combat", true, [new RemovedConstant("MaximumHitRate", "SWLOR.Game.Server.Domain.Combat.CombatRules")]);
    private static readonly RemovedFacade Faction = new("Faction", false, [new RemovedConstant("MinimumFaction", "SWLOR.Game.Server.Domain.Quests.FactionService")]);
    private static readonly RemovedFacade Encounter = new("QuestService.QuestEncounter", false, []);
    private static readonly RemovedFacade[] All = [Combat, Faction, Encounter, Taxi];

    private const string MasterShaped = """
        using System.Collections.Generic;
        using SWLOR.Game.Server.Service;

        namespace SWLOR.Game.Server.Feature
        {
            // Taxi.Register in a comment stays, and so does "Taxi.Register" in a string.
            public static class Sample
            {
                public static int Run(uint player)
                {
                    Taxi.RegisterTaxiDestination(player, 3);
                    var destinations = Taxi.GetDestinationsByRegionId(1).Count;
                    var hit = Combat.CalculateHitRate(1, 2) + Combat.MaximumHitRate;
                    return destinations + hit + Faction.MinimumFaction + nameof(Taxi.RegisterTaxiDestination).Length;
                }
            }
        }
        """;

    private const string BranchShaped = """
        using System.Collections.Generic;
        using SWLOR.Game.Server.Service;
        using SWLOR.Game.Server.Composition;
        using SWLOR.Game.Server.Domain.Combat;
        using SWLOR.Game.Server.Domain.Quests;
        using static SWLOR.Game.Server.Composition.GameServices;

        namespace SWLOR.Game.Server.Feature
        {
            // Taxi.Register in a comment stays, and so does "Taxi.Register" in a string.
            public static class Sample
            {
                public static int Run(uint player)
                {
                    Taxi.RegisterTaxiDestination(player, 3);
                    var destinations = Taxi.GetDestinationsByRegionId(1).Count;
                    var hit = GameServices.Combat.CalculateHitRate(1, 2) + CombatRules.MaximumHitRate;
                    return destinations + hit + FactionService.MinimumFaction + nameof(Taxi.RegisterTaxiDestination).Length;
                }
            }
        }
        """;

    [Test]
    public void A_master_shaped_file_gets_the_imports_and_the_respelled_names_and_nothing_else()
    {
        var result = CallerRewriter.Apply(MasterShaped, All, importedByProject: false);

        result.Text.Should().Be(BranchShaped);
        result.UsingsAdded.Should().Be(4);
        result.Rewrites.Should().Be(3, "the explicit Combat call and the two moved constants change; the Taxi calls keep their text");
        result.NeedsAccessorImport.Should().BeTrue();
    }

    [Test]
    public void A_second_run_changes_nothing()
    {
        var once = CallerRewriter.Apply(MasterShaped, All, false).Text;

        var twice = CallerRewriter.Apply(once, All, false);

        twice.Text.Should().Be(once);
        twice.UsingsAdded.Should().Be(0);
        twice.Rewrites.Should().Be(0);
    }

    [Test]
    public void The_reversal_gives_back_the_master_shaped_text_byte_for_byte()
    {
        CallerRewriter.Reverse(BranchShaped, All).Text.Should().Be(MasterShaped);
        CallerRewriter.Reverse(CallerRewriter.Apply(MasterShaped.Replace("\n", "\r\n"), All, false).Text, All).Text
            .Should().Be(MasterShaped.Replace("\n", "\r\n"), "the line endings the file uses are kept both ways");
    }

    [Test]
    public void The_reversal_of_a_file_with_nothing_to_reverse_returns_it_unchanged()
    {
        var result = CallerRewriter.Reverse(MasterShaped, All);

        result.Text.Should().BeSameAs(MasterShaped);
        result.Changed(MasterShaped).Should().BeFalse();
    }

    [TestCase("Service.Taxi.RegisterTaxiDestination(1, 2);")]
    [TestCase("SWLOR.Game.Server.Service.Taxi.RegisterTaxiDestination(1, 2);")]
    [TestCase("global::SWLOR.Game.Server.Service.Taxi.RegisterTaxiDestination(1, 2);")]
    [TestCase("Server.Service.Taxi.RegisterTaxiDestination(1, 2);")]
    public void A_qualified_call_loses_its_qualifier(string call)
    {
        var text = $"namespace SWLOR.Game.Server.Domain.World\n{{\n    class C\n    {{\n        void M()\n        {{\n            {call}\n        }}\n    }}\n}}\n";

        var result = CallerRewriter.Apply(text, All, false);

        result.Text.Should().Be("using static SWLOR.Game.Server.Composition.GameServices;\n" + text.Replace(call, "Taxi.RegisterTaxiDestination(1, 2);"));
        result.Rewrites.Should().Be(1);
    }

    [Test]
    public void A_further_facade_is_reached_by_its_simple_name()
    {
        const string text = "using SWLOR.Game.Server.Service.QuestService;\nclass C { void M() { QuestEncounter.Start(1); QuestService.QuestEncounter.Stop(2); } }\n";

        CallerRewriter.Apply(text, All, false).Text.Should().Be(
            "using SWLOR.Game.Server.Service.QuestService;\nusing static SWLOR.Game.Server.Composition.GameServices;\nclass C { void M() { QuestEncounter.Start(1); QuestEncounter.Stop(2); } }\n");
    }

    [TestCase("class C { void M(Holder x) { x.Taxi.Register(1); } }\n")]
    [TestCase("class C { void M() { TaxiService.Register(1); var t = typeof(Taxi); } }\n")]
    [TestCase("class C { string M() => \"Taxi.Register\"; } // Taxi.Register\n")]
    [TestCase("class C { void M() { GameServices.Taxi.Register(1); } }\n")]
    public void Code_that_does_not_call_a_removed_facade_is_left_alone(string text)
    {
        var result = CallerRewriter.Apply(text, All, false);

        result.Text.Should().BeSameAs(text);
        result.NeedsAccessorImport.Should().BeFalse();
    }

    [TestCase("class C { Holder Taxi { get; } void M() { Taxi.Register(1); } }\n")]
    [TestCase("class C { Holder Taxi; void M() { Taxi.Register(1); } }\n")]
    [TestCase("class C { static class Taxi { public static void Register(int i) { } } void M() { Taxi.Register(1); } }\n")]
    [TestCase("class C { void M(Holder Taxi) { Taxi.Register(1); } }\n")]
    [TestCase("class C { void M() { var Taxi = new Holder(); Taxi.Register(1); } }\n")]
    [TestCase("class C { void M() { Run(Taxi => Taxi.Register(1)); } }\n")]
    [TestCase("using Taxi = Some.Other.Type;\nclass C { void M() { Taxi.Register(1); } }\n")]
    [TestCase("class C { void M() { if (Get() is Holder Taxi) Taxi.Register(1); } }\n")]
    [TestCase("class C { int M() => Faction.MinimumFaction; static class Faction { public const int MinimumFaction = 1; } }\n")]
    [TestCase("class C { event Handler Taxi { add { } remove { } } void M() { Taxi.Register(1); } }\n")]
    [TestCase("class C { object M(int[] xs) => from Taxi in xs select Taxi.Register(1); }\n")]
    [TestCase("class C { object M(int[] xs) => from x in xs let Taxi = x select Taxi.Register(1); }\n")]
    [TestCase("class C { object M(int[] xs, int[] ys) => from x in xs join Taxi in ys on x equals Taxi select Taxi.Register(1); }\n")]
    [TestCase("class C { object M(int[] xs, int[] ys) => from x in xs join y in ys on x equals y into Taxi select Taxi.Register(1); }\n")]
    [TestCase("class C { object M(int[] xs) => from x in xs select x into Taxi select Taxi.Register(1); }\n")]
    public void A_bare_name_the_file_declares_itself_is_not_the_facade(string text)
    {
        var result = CallerRewriter.Apply(text, All, false);

        result.Text.Should().BeSameAs(text);
        result.NeedsAccessorImport.Should().BeFalse();
    }

    [Test]
    public void A_qualified_call_in_a_file_that_declares_the_name_is_written_explicitly()
    {
        const string text = "class C { Holder Taxi; void M() { Taxi.Register(1); Service.Taxi.Register(2); } }\n";

        var result = CallerRewriter.Apply(text, All, false);

        result.Text.Should().Be("using SWLOR.Game.Server.Composition;\nclass C { Holder Taxi; void M() { Taxi.Register(1); GameServices.Taxi.Register(2); } }\n");
        result.NeedsAccessorImport.Should().BeFalse("the explicit name needs the namespace, not the static import");
        CallerRewriter.Apply(result.Text, All, false).Text.Should().Be(result.Text);
    }

    [Test]
    public void A_project_that_imports_the_accessor_globally_gets_no_using_in_the_file()
    {
        const string text = "namespace N\n{\n    class C { void M() { Taxi.Register(1); } }\n}\n";

        var result = CallerRewriter.Apply(text, All, importedByProject: true);

        result.Text.Should().BeSameAs(text);
        result.NeedsAccessorImport.Should().BeTrue("the runner adds the global import to the project's GlobalUsings.cs");
    }

    [Test]
    public void A_using_goes_after_the_usings_inside_a_namespace_with_their_indentation()
    {
        const string text = "namespace N\n{\n    using System;\n\n    class C { void M() { Taxi.Register(1); } }\n}\n";

        CallerRewriter.Apply(text, All, false).Text.Should().Be(
            "namespace N\n{\n    using System;\n    using static SWLOR.Game.Server.Composition.GameServices;\n\n    class C { void M() { Taxi.Register(1); } }\n}\n");
    }

    [Test]
    public void A_file_without_usings_gets_the_using_on_the_line_of_its_first_member_after_its_header()
    {
        const string text = "// header\nnamespace N;\n\nclass C { void M() { Taxi.Register(1); } }\n";

        var result = CallerRewriter.Apply(text, All, false);

        result.Text.Should().Be("// header\nusing static SWLOR.Game.Server.Composition.GameServices;\nnamespace N;\n\nclass C { void M() { Taxi.Register(1); } }\n");
        CallerRewriter.Reverse(result.Text, All).Text.Should().Be(text);
    }

    [Test]
    public void A_last_using_with_code_after_it_on_its_line_gets_the_new_using_right_after_it()
    {
        const string text = "using System; class C { void M() { Taxi.Register(1); } }";

        CallerRewriter.Apply(text, All, false).Text.Should()
            .Be("using System;\nusing static SWLOR.Game.Server.Composition.GameServices; class C { void M() { Taxi.Register(1); } }");
    }

    [Test]
    public void A_last_using_with_a_comment_after_it_keeps_the_comment_on_its_line()
    {
        const string text = "using System; // why\n\nclass C { void M() { Taxi.Register(1); } }\n";

        var result = CallerRewriter.Apply(text, All, false);

        result.Text.Should().Be("using System; // why\nusing static SWLOR.Game.Server.Composition.GameServices;\n\nclass C { void M() { Taxi.Register(1); } }\n");
        CallerRewriter.Reverse(result.Text, All).Text.Should().Be(text);
    }

    [Test]
    public void A_moved_constant_read_inside_its_owners_namespace_needs_no_using()
    {
        const string text = "namespace SWLOR.Game.Server.Domain.Combat.Parts\n{\n    class C { int M() => Service.Combat.MaximumHitRate; }\n}\n";

        CallerRewriter.Apply(text, All, false).Text.Should().Be(text.Replace("Service.Combat.MaximumHitRate", "CombatRules.MaximumHitRate"));
    }

    [Test]
    public void The_reversal_drops_only_the_imports_of_namespaces_master_does_not_have()
    {
        const string text = "global using static SWLOR.Game.Server.Composition.GameServices;\n" +
                            "using SWLOR.Game.Server.Service.QuestService;\nusing SWLOR.Game.Server.Domain.Quests;\nusing Alias = SWLOR.Game.Server.Composition;\n" +
                            "using System; using SWLOR.Game.Server.Composition;\n" +
                            "class C { int M() => FactionService.MinimumFaction + SWLOR.Game.Server.Composition.GameServices.Taxi.Count + GameServices.Taxi; }\n";

        var result = CallerRewriter.Reverse(text, All);

        result.Text.Should().Be("using SWLOR.Game.Server.Service.QuestService;\nusing Alias = SWLOR.Game.Server.Composition;\nusing System; \n" +
                                "class C { int M() => Faction.MinimumFaction + Taxi.Count + GameServices.Taxi; }\n");
        result.UsingsAdded.Should().Be(3);
        result.Rewrites.Should().Be(2, "GameServices.Taxi alone (not followed by a member) has no master spelling and stays");
    }

    [Test]
    public void Positions_count_bytes_so_text_that_is_not_ascii_round_trips()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("﻿using System;\nclass C { string S = \"café\"; void M() { Taxi.Register(1); } }\n");
        var source = SourceFile.Decode(bytes);

        var applied = source.Encode(CallerRewriter.Apply(source.Text, All, false).Text);
        var reversed = SourceFile.Decode(applied);

        System.Text.Encoding.UTF8.GetString(applied).Should().Contain("using static SWLOR.Game.Server.Composition.GameServices;\nclass C { string S = \"café\";");
        reversed.Bom.Should().NotBeEmpty();
        reversed.Encode(CallerRewriter.Reverse(reversed.Text, All).Text).Should().Equal(bytes);
    }
}
