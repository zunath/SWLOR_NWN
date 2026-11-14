using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Debt;
using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The domain order (P5-1b) on small inputs: how <c>DomainMap.json</c> records it, which references the source scanner
/// records, how pairs and upward references are counted by band, and what a native sink may do.
/// </summary>
[TestFixture]
public class DomainOrderMeasureTests
{
    private const string Server = SourceSnapshot.ServerProjectName;

    /// <summary>Platform, Foundation, Character, Combat, Abilities, Gameplay, bottom to top.</summary>
    private const string OrderedMap = """
        {
          "platformDomain": "Platform", "featureTypeDomain": "Gameplay", "unrestrictedTargetDomains": ["Foundation", "Platform"],
          "platformAreaDomains": { "Entity": "Foundation", "Enumeration": "Foundation" },
          "wiringPlatformAreas": ["Composition", "NativeSinks"], "contentRuleExemptAreas": ["Composition"],
          "order": ["Platform", "Foundation", "Character", "Combat", "Abilities", "Gameplay"], "contentBand": "aboveAllLogic",
          "domains": {
            "Foundation": { "services": [], "featureFolders": ["MigrationDefinition"] },
            "Character": { "services": [], "featureFolders": ["StatusEffectDefinition"] },
            "Combat": { "services": [], "featureFolders": [] },
            "Abilities": { "services": [], "featureFolders": ["AbilityDefinition"] },
            "Gameplay": { "services": [], "featureFolders": [] }
          }
        }
        """;

    private static DependencyUnit Unit(string text)
    {
        DependencyUnit.TryParse(text, out var unit).Should().BeTrue();
        return unit;
    }

    private static SourceReference Reference(string file, string from, string member, string to) =>
        new(file, Unit(from), member, Unit(to), "Target");

    /// <summary>One reference of each kind the order measures tell apart.</summary>
    private static readonly SourceReference[] References =
    [
        Reference("Combat.cs", "Domain/Combat", "Combat::A", "Domain/Character"),
        Reference("Stat.cs", "Domain/Character", "Stat::B", "Domain/Combat"),
        Reference("Stat.cs", "Domain/Character", "Stat::C", "Domain/Combat"),
        Reference("Ability.cs", "Feature/AbilityDefinition", "Ability::D", "Feature/StatusEffectDefinition"),
        Reference("Effect.cs", "Feature/StatusEffectDefinition", "Effect::E", "Feature/AbilityDefinition"),
        Reference("Effect.cs", "Feature/StatusEffectDefinition", "Effect::F", "Domain/Combat"),
        Reference("Combat.cs", "Domain/Combat", "Combat::G", "Feature/StatusEffectDefinition"),
        Reference("Player.cs", "Platform/Entity", "Player::H", "Domain/Character"),
        Reference("Stat.cs", "Domain/Character", "Stat::I", "Platform/Entity"),
        Reference("Player.cs", "Platform/Entity", "Player::J", "Domain/Foundation"),
        Reference("Sink.cs", "Platform/NativeSinks", "Sink::K", "Domain/Combat"),
        Reference("Combat.cs", "Domain/Combat", "Combat::L", "Platform/Core")
    ];

    [Test]
    public void The_map_records_the_order_with_content_above_logic_entity_in_foundation_and_the_sinks_as_wiring()
    {
        var map = DomainMap.Parse(OrderedMap);

        map.Problems.Should().BeEmpty();
        map.Order.Should().Equal("Platform", "Foundation", "Character", "Combat", "Abilities", "Gameplay");
        map.DomainOf(Unit("Platform/Entity")).Should().Be("Foundation");
        map.DomainOf(Unit("Platform/Enumeration")).Should().Be("Foundation");
        map.DomainOf(Unit("Platform/Native")).Should().Be("Platform");
        map.NodeOf(Unit("Feature/StatusEffectDefinition")).Should().Be(new OrderNode("Character", IsContent: true));
        map.NodeOf(Unit("Feature/StatusEffectDefinition"))!.Value.ToString().Should().Be("Character content");
        map.IsWiringSource(Unit("Platform/NativeSinks")).Should().BeTrue();
        map.IsContentRuleExempt(Unit("Platform/NativeSinks")).Should().BeFalse("the sinks stay under the no-content rule");
        map.IsContentRuleExempt(Unit("Platform/Composition")).Should().BeTrue();

        var character = new OrderNode("Character", false);
        var combat = new OrderNode("Combat", false);
        var characterContent = new OrderNode("Character", true);
        var abilitiesContent = new OrderNode("Abilities", true);
        map.DirectionOf(character, combat).Should().Be(OrderDirection.Up);
        map.DirectionOf(combat, character).Should().Be(OrderDirection.Down);
        map.DirectionOf(characterContent, abilitiesContent).Should().Be(OrderDirection.Up);
        map.DirectionOf(abilitiesContent, combat).Should().Be(OrderDirection.ContentUsesLogic);
        map.DirectionOf(combat, characterContent).Should().Be(OrderDirection.LogicNamesContent);
        map.DirectionOf(character, characterContent).Should().Be(OrderDirection.LogicNamesContent, "logic never names content, its own domain's included");
        map.DirectionOf(combat, combat).Should().Be(OrderDirection.Inside);
    }

    [Test]
    public void The_map_reports_an_order_that_misses_repeats_or_invents_a_domain_and_any_other_band_rule()
    {
        var json = OrderedMap
            .Replace("\"Combat\", \"Abilities\", \"Gameplay\"]", "\"Combat\", \"Combat\", \"Missing\", \"Gameplay\"]")
            .Replace("aboveAllLogic", "none")
            .Replace("\"Enumeration\": \"Foundation\"", "\"Enumeration\": \"Nowhere\"")
            .Replace("\"contentRuleExemptAreas\": [\"Composition\"]", "\"contentRuleExemptAreas\": [\"Core\"]");

        var problems = DomainMap.Parse(json).Problems;

        problems.Should().Contain(problem => problem.Contains("lists 'Combat' twice"))
            .And.Contain(problem => problem.Contains("'Missing', which is not a domain"))
            .And.Contain(problem => problem.Contains("does not list 'Abilities'"))
            .And.Contain(problem => problem.Contains("contentBand is 'none'"))
            .And.Contain(problem => problem.Contains("'Enumeration' in 'Nowhere'"))
            .And.Contain(problem => problem.Contains("'Core', which is not in wiringPlatformAreas"));
    }

    [Test]
    public void References_are_bound_like_the_compiler_per_member_and_only_between_units()
    {
        var snapshot = SourceSnapshot.FromText(new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [Server] = new Dictionary<string, string>
            {
                ["SWLOR.Game.Server/Domain/Character/Stats.cs"] = """
                    namespace SWLOR.Game.Server.Domain.Character
                    {
                        public enum StatKind { Might, Agility }
                        public static class StatRules { public const int Cap = 50; public static int Clamp(int value) => value; }
                        public interface IStatService { int Get(StatKind kind); }
                        public sealed class StatService : IStatService
                        {
                            public int Get(StatKind kind) => StatRules.Clamp(1);
                        }
                    }
                    """,
                ["SWLOR.Game.Server/Domain/Combat/CombatService.cs"] = """
                    using SWLOR.Game.Server.Domain.Character;

                    namespace SWLOR.Game.Server.Domain.Combat
                    {
                        public sealed class CombatService
                        {
                            // StatRules.Cap in a comment is not a reference.
                            private readonly IStatService _stat;

                            public CombatService(IStatService stat) { _stat = stat; }

                            public int Hit() => _stat.Get(StatKind.Might) + StatRules.Cap;

                            public string Name() => nameof(StatRules);

                            public int Infer()
                            {
                                var service = Make();
                                System.Func<int, int> twice = value => value * 2;
                                return twice(service.Get(default));
                            }

                            private static StatService Make() => new();

                            private sealed class Nested { public int Run() => StatRules.Clamp(2); }
                        }
                    }
                    """
            }
        });

        var references = SourceReferenceScanner.Scan(snapshot);

        references.Should().OnlyContain(reference => reference.File == "SWLOR.Game.Server/Domain/Combat/CombatService.cs" &&
                                                      reference.From == Unit("Domain/Combat") && reference.To == Unit("Domain/Character"),
            "references inside the Character unit, using directives and comments are not recorded");
        const string Type = "SWLOR.Game.Server.Domain.Combat.CombatService";
        references.Select(reference => (reference.Member, reference.Target)).Should().BeEquivalentTo(new[]
        {
            (Type + "::_stat", "IStatService"),
            (Type + "::.ctor", "IStatService"),
            (Type + "::Hit", "IStatService"),
            (Type + "::Hit", "StatKind"),
            (Type + "::Hit", "StatRules"),
            (Type + "::Name", "StatRules"),
            (Type + "::Infer", "StatService"),
            (Type + "::Make", "StatService"),
            (Type + "+Nested::Run", "StatRules")
        }, "a constant, an enum member, nameof, the type a var infers, a target-typed new and a nested type's member all count");
    }

    [Test]
    public void Pairs_are_counted_by_band_with_entity_as_foundation_and_wiring_left_out()
    {
        var cycles = DomainCycleScanner.Scan(References, DomainMap.Parse(OrderedMap));

        cycles.Select(cycle => cycle.Key).Should().Equal("Abilities content <-> Character content", "Character <-> Combat", "Character <-> Foundation");
        var combat = cycles.Single(cycle => cycle.Key == "Character <-> Combat");
        combat.Detail.Should().Be("Character -> Combat: 1 edge(s), 2 member(s); Combat -> Character: 1 edge(s), 1 member(s)");
        DebtMeasures.DomainCycles(cycles).Entries.Should().Contain(new DebtEntry("Character <-> Combat", 2, combat.Detail));
        DebtMeasures.DomainCycles(cycles).Summary[0].Should().Be("3 pairs of domains depend on each other: 2 between logic (1 of them with Foundation or Platform), 1 between content.");
    }

    [Test]
    public void Upward_references_are_listed_per_file_and_higher_node_with_their_members()
    {
        var upward = DomainCycleScanner.Upward(References, DomainMap.Parse(OrderedMap));
        var measure = DebtMeasures.UpwardEdges(upward);

        measure.Entries.Select(entry => (entry.Key, entry.Count)).Should().BeEquivalentTo(new[]
        {
            ("Stat.cs -> Combat", 2),
            ("Player.cs -> Character", 1),
            ("Effect.cs -> Abilities content", 1)
        }, "down, inside, content-to-logic, logic-to-content and wiring references are not up");
        measure.Entries.Single(entry => entry.Key == "Stat.cs -> Combat").Detail.Should().Be("Character -> Combat: Target");
        measure.Summary[0].Should().Be("4 members in 3 files reference a domain above their own: 3 directions, 3 unit edges.");
    }

    [Test]
    public void A_sink_that_only_forwards_does_no_work_and_everything_else_in_the_sinks_is_counted()
    {
        var snapshot = SourceSnapshot.FromText(new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [Server] = new Dictionary<string, string>
            {
                ["SWLOR.Game.Server/NativeSinks/Sinks.cs"] = """
                    namespace SWLOR.Game.Server.NativeSinks
                    {
                        public interface IHandler { int Handle(int a, int b); }

                        public sealed class ForwardingSink
                        {
                            private readonly IHandler _handler;
                            public ForwardingSink(IHandler handler) { _handler = handler; }
                            public int Run(int a, int b) => _handler.Handle(a, b);
                            public int Rule(int a, int b) { return Rules.Resolve(a, b); }
                        }

                        public static class Rules { public static int Resolve(int a, int b) => a + b; }

                        public sealed class WorkingSink
                        {
                            private readonly System.Collections.Generic.Dictionary<int, int> _state = new();
                            public int Run(int a, int b)
                            {
                                if (a > b) return a;
                                return _state.Count;
                            }
                            public int Shifted(int a, int b) => Handle(b, a + 1);
                            private int Handle(int a, int b) => a;
                        }

                        public static class NativeSinkFactory { public static object Create() => new ForwardingSink(null!); }
                    }
                    """
            }
        });

        var sinks = SinkWorkScanner.Scan(snapshot.Server);
        var measure = DebtMeasures.SinkWork(sinks);

        sinks.Select(sink => (sink.Type, sink.Lines, sink.ForwardingMembers)).Should().BeEquivalentTo(new[]
        {
            ("SWLOR.Game.Server.NativeSinks.ForwardingSink", 0, 4),
            ("SWLOR.Game.Server.NativeSinks.IHandler", 0, 1),
            ("SWLOR.Game.Server.NativeSinks.Rules", 1, 0),
            ("SWLOR.Game.Server.NativeSinks.WorkingSink", 8, 0)
        }, "the factory is not measured, and an interface member has no body to do work in");
        measure.Entries.Should().BeEquivalentTo(new[]
        {
            new DebtEntry("SWLOR.Game.Server.NativeSinks.Rules", 1, "Resolve"),
            new DebtEntry("SWLOR.Game.Server.NativeSinks.WorkingSink", 8, "_state, Run, Shifted, Handle")
        });
    }
}
