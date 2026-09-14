using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors;
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

        private static Dictionary<FeatType, AbilityDetail> BuildFeatToAbilityMap()
        {
            var map = new Dictionary<FeatType, AbilityDetail>();
            var definitionTypes = typeof(IAbilityListDefinition).Assembly
                .GetTypes()
                .Where(t => typeof(IAbilityListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in definitionTypes)
            {
                var instance = (IAbilityListDefinition)Activator.CreateInstance(type);
                foreach (var (feat, detail) in instance.BuildAbilities())
                    map.TryAdd(feat, detail);
            }

            return map;
        }

        private static bool HasObservableOutcome(AbilityBehaviorCase behaviorCase)
        {
            return behaviorCase.ExpectedActivatorStatusEffects.Length > 0 ||
                   behaviorCase.ExpectedTargetStatusEffects.Length > 0 ||
                   behaviorCase.ExpectedActivatorStatAdjustments.Count > 0 ||
                   behaviorCase.ExpectedTargetStatAdjustments.Count > 0 ||
                   behaviorCase.ExpectedRemovedTargetStatusEffects.Length > 0 ||
                   behaviorCase.ExpectsTargetDamage ||
                   behaviorCase.ExpectsTargetRevived ||
                   behaviorCase.ExpectsActivatorTemporaryHP ||
                   behaviorCase.ExpectsTargetTemporaryHP ||
                   behaviorCase.ExpectsActivatorHealing ||
                   behaviorCase.ExpectsTargetHealing ||
                   behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.HasValue;
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

        [Test]
        public void ExecutableBehaviorCasesCoverEveryDefinitionDeclaredResourceCost()
        {
            var abilities = BuildFeatToAbilityMap();
            var problems = new List<string>();

            foreach (var behaviorCase in BuildAllCases().Where(c => string.IsNullOrWhiteSpace(c.SkipReason)))
            {
                var ability = abilities[behaviorCase.Feat];
                var requiresFP = ability.Requirements.OfType<AbilityRequirementFP>().Any(r => r.RequiredFP > 0);
                var requiresSTM = ability.Requirements.OfType<AbilityRequirementStamina>().Any(r => r.RequiredSTM > 0);
                var omittedDeclaredCost =
                    (requiresFP && !behaviorCase.ExpectsFPCost) ||
                    (requiresSTM && !behaviorCase.ExpectsSTMCost);
                var assertsUndefinedCost =
                    (!requiresFP && behaviorCase.ExpectsFPCost) ||
                    (!requiresSTM && behaviorCase.ExpectsSTMCost);
                var hasWaiver = !string.IsNullOrWhiteSpace(behaviorCase.CostAssertionWaiverReason);

                if (assertsUndefinedCost)
                    problems.Add($"{behaviorCase.Feat}: asserts a resource cost the definition does not declare");
                if (omittedDeclaredCost && !hasWaiver)
                    problems.Add($"{behaviorCase.Feat}: omits a definition-declared resource cost without CostAssertionWaiverReason");
                if (!omittedDeclaredCost && behaviorCase.CostAssertionWaiverReason != null)
                    problems.Add($"{behaviorCase.Feat}: has a stale CostAssertionWaiverReason even though no declared cost is omitted");
            }

            problems.Should().BeEmpty(
                "engine cases must exercise every FP/STM requirement, or explicitly document why same-tick impact behavior makes the deduction unobservable. Problems: {0}",
                string.Join(" | ", problems));
        }

        [Test]
        public void ExecutableImpactsDeclareAnObservableOutcomeOrFocusedWaiver()
        {
            var abilities = BuildFeatToAbilityMap();
            var problems = BuildAllCases()
                .Where(c => string.IsNullOrWhiteSpace(c.SkipReason))
                .Where(c => abilities[c.Feat].ImpactAction != null)
                .Where(c => !HasObservableOutcome(c))
                .Where(c => string.IsNullOrWhiteSpace(c.OutcomeAssertionWaiverReason))
                .Select(c => $"{c.Feat}: executable impact has no observable outcome assertion")
                .OrderBy(x => x)
                .ToList();

            problems.Should().BeEmpty(
                "activation, cost, and recast alone can all pass while the defining impact is broken; assert an outcome or record a focused harness limitation. Problems: {0}",
                string.Join(" | ", problems));
        }

        [Test]
        public void BehaviorCaseContractsAreInternallyConsistent()
        {
            var problems = new List<string>();

            foreach (var behaviorCase in BuildAllCases())
            {
                if (behaviorCase.ExpectsTargetHealing &&
                    behaviorCase.Target != AbilityTargetKind.FriendlyCreature)
                {
                    problems.Add($"{behaviorCase.Feat}: target healing requires a distinct FriendlyCreature target");
                }

                if (behaviorCase.MinimumTargetHitPointsAfterRevive > 0 &&
                    !behaviorCase.ExpectsTargetRevived)
                {
                    problems.Add($"{behaviorCase.Feat}: minimum revived HP requires ExpectsTargetRevived");
                }

                if (behaviorCase.ExpectedTargetHealingPercentAfterRevive.HasValue)
                {
                    if (!behaviorCase.ExpectsTargetRevived)
                        problems.Add($"{behaviorCase.Feat}: post-revive healing percentage requires ExpectsTargetRevived");
                    if (behaviorCase.ExpectedTargetHealingPercentAfterRevive.Value <= 0f)
                        problems.Add($"{behaviorCase.Feat}: post-revive healing percentage must be positive");
                }

                if (behaviorCase.ExpectsTargetRevived && !behaviorCase.TargetStartsDead)
                    problems.Add($"{behaviorCase.Feat}: revival assertion requires TargetStartsDead");

                if (behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.HasValue)
                {
                    if (behaviorCase.Target == AbilityTargetKind.Self)
                        problems.Add($"{behaviorCase.Feat}: distance assertion requires a distinct target");
                    else if (behaviorCase.TargetDistanceMeters <=
                             behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.Value)
                        problems.Add($"{behaviorCase.Feat}: target starts inside its post-impact distance threshold");
                }

                var setupTypes = behaviorCase.TargetSetupStatusEffects.ToHashSet();
                foreach (var removedType in behaviorCase.ExpectedRemovedTargetStatusEffects)
                {
                    if (!setupTypes.Contains(removedType))
                        problems.Add($"{behaviorCase.Feat}: expects removal of {removedType.Name} but does not pre-apply it");
                }

                if (behaviorCase.OutcomeAssertionWaiverReason != null &&
                    string.IsNullOrWhiteSpace(behaviorCase.OutcomeAssertionWaiverReason))
                {
                    problems.Add($"{behaviorCase.Feat}: OutcomeAssertionWaiverReason is blank");
                }

                if (HasObservableOutcome(behaviorCase) &&
                    behaviorCase.OutcomeAssertionWaiverReason != null)
                {
                    problems.Add($"{behaviorCase.Feat}: has a stale OutcomeAssertionWaiverReason despite declaring an observable outcome");
                }
            }

            problems.Should().BeEmpty("declarative cases must create the preconditions needed for every outcome assertion");
        }
    }
}
