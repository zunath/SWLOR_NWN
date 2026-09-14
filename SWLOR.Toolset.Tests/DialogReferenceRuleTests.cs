using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The two module-wide conversation rules, run against the real module. Both report real
    /// findings today, and both are pinned so the numbers cannot drift unnoticed.
    /// </summary>
    public class DialogReferenceRuleTests
    {
        private static ValidationContext Context() =>
            new(new ModuleWorkspace(CorpusLocator.ModuleDirectory));

        /// <summary>
        /// Conversations named by something in the module that exist nowhere — not in Module\dlg,
        /// not in a hak, not in the base game. Mostly furniture and prop conversations from imported
        /// content whose .dlg never came with it. Reported as errors because the object silently
        /// does nothing when a player uses it.
        /// </summary>
        /// <remarks>
        /// Two of these are reachable only through placed instances rather than blueprints
        /// (<c>untitled000</c> is named by seventeen of them), which is why the rule walks the .git
        /// files as well. A blueprint-only sweep reports four and looks complete.
        /// </remarks>
        private static readonly string[] KnownMissingConversations =
        {
            "chair", "pug_cap_computer", "untitled000",
            "zep_demi_regen_c", "_mdrn_conv_chair", "_mdrn_conv_ship"
        };

        [Test]
        public void ConversationsThatDoNotExistAreReported()
        {
            var issues = new DanglingConversationRule().Validate(Context()).ToList();

            // The message quotes the referring object first and the missing conversation second, so
            // the conversation is the fourth field when split on the quote.
            var named = issues
                .Select(issue => issue.Message.Split('\'')[3])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            named.Should().BeEquivalentTo(KnownMissingConversations);
            issues.Should().OnlyContain(issue => issue.Severity == ValidationSeverity.Error);

            // Four missing conversations, but twenty objects pointing at them - the finding is per
            // broken object, because that is what a builder has to go and fix.
            issues.Should().HaveCountGreaterThan(named.Count);
        }

        [Test]
        public void ConversationsNothingPointsAtAreReported()
        {
            var issues = new UnreferencedConversationRule().Validate(Context()).ToList();

            // Every one is a hand-authored conversation: the generated shells are excluded by
            // design, and if they were not this would be 287 findings instead of 32 and nobody
            // would read any of them.
            issues.Should().HaveCount(32);
            issues.Should().OnlyContain(issue => issue.Severity == ValidationSeverity.Warning);
            issues.Select(issue => issue.ResRef).Should().Contain("trooperquest");
            issues.Select(issue => issue.ResRef).Should().NotContain("dmfi_universal",
                "that conversation is started directly by an NSS script");
        }

        [Test]
        public void TheGeneratedShellsAreNeverReported()
        {
            var issues = new UnreferencedConversationRule().Validate(Context()).ToList();

            issues.Should().NotContain(issue =>
                issue.ResRef != null && UnreferencedConversationRule.IsGeneratedShell(issue.ResRef));
        }

        [Test]
        public void TheShellPatternMatchesTheNumberedPoolAndNothingElse()
        {
            UnreferencedConversationRule.IsGeneratedShell("dialog1").Should().BeTrue();
            UnreferencedConversationRule.IsGeneratedShell("dialog255").Should().BeTrue();
            UnreferencedConversationRule.IsGeneratedShell("dialog256").Should().BeFalse();

            // Real conversations that merely start with the same letters.
            UnreferencedConversationRule.IsGeneratedShell("dialognova").Should().BeFalse();
            UnreferencedConversationRule.IsGeneratedShell("dandialog").Should().BeFalse();
            UnreferencedConversationRule.IsGeneratedShell("dialog").Should().BeFalse();
        }
    }
}
