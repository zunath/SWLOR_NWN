using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Feature;

// Guards the incubation field note system. All expectations are derived from the live mutation
// configuration (the beast definitions) rather than a hand-maintained list, so the tests stay
// valid as beasts and mutation paths are added or removed.
public class IncubationFieldNoteTests
{
    // Every beast definition, built once and reused. Mirrors BeastMastery.LoadBeasts without the
    // engine-bound parts, so the graph is available in a plain unit-test context.
    private static readonly Dictionary<BeastType, BeastDetail> Beasts = BuildAllBeasts();

    // target beast -> the source beasts that can mutate into it.
    private static readonly Dictionary<BeastType, List<BeastType>> IncomingSources = BuildIncomingSources(Beasts);

    // Every distinct mutation target; there should be exactly one field note per entry.
    private static readonly HashSet<BeastType> MutationTargets = IncomingSources.Keys.ToHashSet();

    private static Dictionary<BeastType, BeastDetail> BuildAllBeasts()
    {
        var result = new Dictionary<BeastType, BeastDetail>();

        var definitionTypes = typeof(IBeastListDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           typeof(IBeastListDefinition).IsAssignableFrom(type));

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IBeastListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (beastType, detail) in definition.Build())
            {
                result[beastType] = detail;
            }
        }

        return result;
    }

    private static Dictionary<BeastType, List<BeastType>> BuildIncomingSources(Dictionary<BeastType, BeastDetail> beasts)
    {
        var incoming = new Dictionary<BeastType, List<BeastType>>();

        foreach (var (source, detail) in beasts)
        {
            foreach (var mutation in detail.PossibleMutations)
            {
                if (!incoming.TryGetValue(mutation.Type, out var sources))
                {
                    sources = new List<BeastType>();
                    incoming[mutation.Type] = sources;
                }

                sources.Add(source);
            }
        }

        return incoming;
    }

    // A second-level (tier-3) mutation is one whose source is itself a mutation product.
    private static bool IsSecondLevelMutation(BeastType target)
    {
        return IncomingSources[target].Any(MutationTargets.Contains);
    }

    private static KeyItemAttribute GetKeyItemAttribute(KeyItemType keyItem)
    {
        var field = typeof(KeyItemType).GetField(keyItem.ToString())!;
        return field.GetCustomAttribute<KeyItemAttribute>()!;
    }

    private static IEnumerable<KeyItemType> FieldNoteKeyItems()
    {
        return Enum.GetValues<KeyItemType>()
            .Where(keyItem => GetKeyItemAttribute(keyItem).Category == KeyItemCategoryType.FieldNotes);
    }

    [Test]
    public void EveryMutationTarget_HasAFieldNote()
    {
        var registeredTargets = IncubationFieldNote.GetAllNotes().Select(n => n.Target).ToHashSet();

        var missing = MutationTargets.Except(registeredTargets).ToList();

        missing.Should().BeEmpty("every beast reachable by mutation needs a field note");
    }

    [Test]
    public void EveryFieldNote_TargetsARealMutation()
    {
        var registeredTargets = IncubationFieldNote.GetAllNotes().Select(n => n.Target).ToHashSet();

        var stray = registeredTargets.Except(MutationTargets).ToList();

        stray.Should().BeEmpty("a field note should only exist for a beast that is actually a mutation product");
    }

    [Test]
    public void FieldNotesCategory_MatchesRegistry()
    {
        var categoryKeyItems = FieldNoteKeyItems().ToHashSet();
        var registeredKeyItems = IncubationFieldNote.GetAllNotes().Select(n => n.Note).ToHashSet();

        registeredKeyItems.Should().BeEquivalentTo(categoryKeyItems,
            "every FieldNotes key item must be registered and every registered note must be a FieldNotes key item");
    }

    [Test]
    public void RegisteredNotes_AreUniqueAndValid()
    {
        var notes = IncubationFieldNote.GetAllNotes();

        notes.Select(n => n.Note).Should().OnlyHaveUniqueItems();
        notes.Select(n => n.Target).Should().OnlyHaveUniqueItems();
        notes.Should().OnlyContain(n => n.Acquisition != FieldNoteAcquisitionType.Invalid,
            "every field note must declare a real acquisition type");
    }

    [Test]
    public void SecondLevelMutations_AreNeverSoldInStores()
    {
        var acquisitionByTarget = IncubationFieldNote.GetAllNotes().ToDictionary(n => n.Target, n => n.Acquisition);

        foreach (var target in MutationTargets.Where(IsSecondLevelMutation))
        {
            acquisitionByTarget[target].Should().NotBe(FieldNoteAcquisitionType.Store,
                $"{target} is a second-level mutation and its note must not be purchasable");
        }
    }

    [Test]
    public void SecondLevelMutations_AreMostlyDiscoveryOnly()
    {
        var acquisitionByTarget = IncubationFieldNote.GetAllNotes().ToDictionary(n => n.Target, n => n.Acquisition);

        var secondLevel = MutationTargets.Where(IsSecondLevelMutation).ToList();
        var discoveryOnly = secondLevel.Count(t => acquisitionByTarget[t] == FieldNoteAcquisitionType.DiscoveryOnly);

        discoveryOnly.Should().BeGreaterThan(secondLevel.Count / 2,
            "the majority of second-level mutation notes must only be obtainable by discovery");
    }

    [Test]
    public void EveryMutationRequirement_DescribesItself()
    {
        foreach (var (source, detail) in Beasts)
        {
            foreach (var mutation in detail.PossibleMutations)
            {
                foreach (var requirement in mutation.Requirements)
                {
                    requirement.GetRequirementDescription().Should().NotBeNullOrWhiteSpace(
                        $"{requirement.GetType().Name} on {source} -> {mutation.Type} must produce a field-note description");
                }
            }
        }
    }

    [Test]
    public void EnzymeRequirement_ExposesEachEnzymeAsASeparateDescription()
    {
        var requirement = new MutationRequirementEnzyme();
        requirement.LyaseEnzymeColors[EnzymeColorType.Black] = 2;
        requirement.IsomeraseEnzymeColors[EnzymeColorType.Red] = 2;
        requirement.HydrolaseEnzymeColors[EnzymeColorType.Red] = 1;

        requirement.GetEnzymeDescriptions().Should().Equal(
            "2x Black Lyase",
            "2x Red Isomerase",
            "1x Red Hydrolase");
    }
}
