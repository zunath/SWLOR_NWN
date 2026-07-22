using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature
{
    /// <summary>
    /// Coverage ratchet for the in-engine ability behavior program: every ability feat
    /// registered by an IAbilityListDefinition must have exactly one AbilityBehaviorCase
    /// declared in an IAbilityBehaviorSource, unless its defining tree is still on the
    /// explicit not-yet-covered list below. Shrink that list as batches land; never grow it
    /// for new player content - new abilities must ship with a behavior case.
    /// </summary>
    public class AbilityBehaviorCoverageTests
    {
        /// <summary>
        /// Trees whose behavior cases have not been written yet. Currently empty - every
        /// registered ability feat in the game has a behavior case. If a brand-new tree is
        /// added and genuinely cannot be covered in the same change, add its namespace
        /// fragment here temporarily; never grow this list for individual abilities.
        /// </summary>
        private static readonly string[] NotYetCoveredNamespaceFragments = Array.Empty<string>();

        private static Dictionary<FeatType, List<Type>> BuildFeatToDefinitionMap()
        {
            var map = new Dictionary<FeatType, List<Type>>();
            var definitionTypes = typeof(IAbilityListDefinition).Assembly
                .GetTypes()
                .Where(t => typeof(IAbilityListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in definitionTypes)
            {
                var instance = (IAbilityListDefinition)Activator.CreateInstance(type);
                foreach (var feat in instance.BuildAbilities().Keys)
                {
                    if (!map.TryGetValue(feat, out var definers))
                    {
                        definers = new List<Type>();
                        map[feat] = definers;
                    }

                    definers.Add(type);
                }
            }

            return map;
        }

        private static List<AbilityBehaviorCase> BuildAllCases()
        {
            var sourceTypes = typeof(IAbilityBehaviorSource).Assembly
                .GetTypes()
                .Where(t => typeof(IAbilityBehaviorSource).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            return sourceTypes
                .Select(t => (IAbilityBehaviorSource)Activator.CreateInstance(t))
                .SelectMany(s => s.BuildCases())
                .ToList();
        }

        private static bool IsExempt(List<Type> definers)
        {
            // A feat is exempt only if every definition that registers it lives in a
            // not-yet-covered tree.
            return definers.All(d =>
                NotYetCoveredNamespaceFragments.Any(fragment =>
                    d.Namespace != null && d.Namespace.Contains(fragment)));
        }

        [Test]
        public void EveryCoveredTreeAbilityFeatHasABehaviorCase()
        {
            var featMap = BuildFeatToDefinitionMap();
            var coveredFeats = BuildAllCases().Select(c => c.Feat).ToHashSet();

            var missing = featMap
                .Where(kvp => !IsExempt(kvp.Value) && !coveredFeats.Contains(kvp.Key))
                .Select(kvp => $"{kvp.Key} (defined in {string.Join(", ", kvp.Value.Select(t => t.Name))})")
                .OrderBy(x => x)
                .ToList();

            missing.Should().BeEmpty(
                "every ability feat in a covered tree must have an AbilityBehaviorCase - add one to the tree's IAbilityBehaviorSource (or, for a brand-new tree, add it to the batch plan)");
        }

        [Test]
        public void BehaviorCasesReferenceOnlyRegisteredFeats()
        {
            var registeredFeats = BuildFeatToDefinitionMap().Keys.ToHashSet();

            var orphaned = BuildAllCases()
                .Select(c => c.Feat)
                .Where(f => !registeredFeats.Contains(f))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            orphaned.Should().BeEmpty("behavior cases must reference feats registered by an IAbilityListDefinition");
        }

        [Test]
        public void BehaviorCasesHaveNoDuplicateFeats()
        {
            var duplicates = BuildAllCases()
                .GroupBy(c => c.Feat)
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key} (x{g.Count()})")
                .OrderBy(x => x)
                .ToList();

            duplicates.Should().BeEmpty("each ability feat must have exactly one behavior case");
        }

        [Test]
        public void SkippedBehaviorCasesAlwaysDocumentAReason()
        {
            var badSkips = BuildAllCases()
                .Where(c => c.SkipReason != null && string.IsNullOrWhiteSpace(c.SkipReason))
                .Select(c => c.Feat.ToString())
                .ToList();

            badSkips.Should().BeEmpty("a skipped behavior case must say why it cannot run in-engine yet");
        }
    }
}
