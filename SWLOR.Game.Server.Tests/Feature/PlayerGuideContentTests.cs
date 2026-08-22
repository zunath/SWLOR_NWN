using System.Collections;
using System.Globalization;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerGuideContentTests
{
    private static readonly string[] RequiredTopics =
    {
        "Common Questions",
        "Communication",
        "Skills",
        "Skill Decay",
        "Perks",
        "Force Affinity",
        "Perk Refunds",
        "XP Debt",
        "Abilities",
        "Mimicry & Techniques",
        "Attributes",
        "Rebuilds",
        "Death & Recovery",
        "Combat Basics",
        "Espionage",
        "Disguises",
        "Crafting",
        "Gathering & Fishing",
        "Training Store",
        "Beasts & Stables",
        "Droids",
        "Quests & Key Items",
        "Quest Contracts",
        "Guilds & Citizenship",
        "Housing & Markets",
        "Travel & Navigation",
        "Ships & Space",
        "Useful Windows"
    };

    [Test]
    public void Topics_AreCompleteAndEveryRelatedTopicResolves()
    {
        var topics = GetTopics();
        var topicNames = topics.Select(topic => GetString(topic, "Name")).ToList();
        var topicNameSet = topicNames.ToHashSet(StringComparer.Ordinal);

        topicNames.Should().OnlyHaveUniqueItems();
        RequiredTopics.Except(topicNameSet).Should().BeEmpty("all major player systems should have a guide topic");

        foreach (var topic in topics)
        {
            var topicName = GetString(topic, "Name");
            topicName.Should().NotBeNullOrWhiteSpace();
            GetString(topic, "Category").Should().NotBeNullOrWhiteSpace($"{topicName} needs a category");
            GetString(topic, "Summary").Should().NotBeNullOrWhiteSpace($"{topicName} needs a summary");
            GetString(topic, "RailSummary").Should().NotBeNullOrWhiteSpace($"{topicName} needs a rail summary");

            var blocks = GetItems(topic, "Blocks");
            blocks.Should().NotBeEmpty($"{topicName} needs article content");
            foreach (var block in blocks)
            {
                GetString(block, "Title").Should().NotBeNullOrWhiteSpace($"{topicName} has an untitled article block");
                GetString(block, "Body").Should().NotBeNullOrWhiteSpace($"{topicName} has an empty article block");
            }

            var questions = GetItems(topic, "Questions");
            questions.Should().NotBeEmpty($"{topicName} needs quick answers");
            foreach (var question in questions)
            {
                GetString(question, "Question").Should().NotBeNullOrWhiteSpace($"{topicName} has an empty question");
                GetString(question, "Answer").Should().NotBeNullOrWhiteSpace($"{topicName} has an empty answer");
            }

            var relatedTopics = GetItems(topic, "RelatedTopics")
                .Cast<string>()
                .ToList();
            relatedTopics.Should().OnlyHaveUniqueItems($"{topicName} should not repeat related links");
            relatedTopics.Should().NotContain(topicName, $"{topicName} should not link to itself");
            relatedTopics.Where(related => !topicNameSet.Contains(related))
                .Should().BeEmpty($"every related link from {topicName} should resolve");
        }
    }

    [Test]
    public void CriticalPublishedLimits_MatchCurrentGameplayConstants()
    {
        var guideText = string.Join("\n", GetTopics().SelectMany(GetAllText));

        guideText.Should().Contain($"up to {Skill.SkillCap} total ranks");
        guideText.Should().Contain($"{Skill.StartingSkillPoints} starting SP");
        guideText.Should().Contain($"up to {Skill.APCap} AP");

        guideText.Should().Contain($"costs {HoloNetViewModel.BroadcastPrice} credits");
        guideText.Should().Contain($"limited to {HoloNetViewModel.MaxHoloNetTextLength} characters");
        guideText.Should().Contain($"up to {Notes.MaxNumberOfNotes} notes");
        guideText.Should().Contain($"up to {Notes.MaxNoteLength} characters");
        guideText.Should().Contain($"up to {Notes.MaxNumberOfCategories} categories");
        guideText.Should().Contain($"normal listing limit is {Player.DefaultMarketListingLimit} items");

        guideText.Should().Contain($"up to {QuestContractBoard.MaxActiveContractsPerCDKey} published");
        guideText.Should().Contain($"between 1 and {QuestContractBoard.MaxObjectives} item objectives");
        guideText.Should().Contain($"up to {QuestContractBoard.MaxRewardItems} reward items");
        guideText.Should().Contain($"{QuestContractBoard.ContractDurationDays} days");
        guideText.Should().Contain($"{QuestContractBoard.PostingFeePercent} percent");
        guideText.Should().Contain($"minimum fee of {QuestContractBoard.MinimumPostingFee} credits");

        guideText.Should().Contain($"begin with {Player.DefaultDisguiseSlotLimit} identity slot");
        guideText.Should().Contain($"base wait between disguise activations is {Disguise.ActivationDelayMinutes} minutes");
        guideText.Should().Contain($"minimum wait of {Disguise.MinimumActivationDelayMinutes} minutes");
        guideText.Should().Contain($"{Disguise.WipeCreditCost.ToString("N0", CultureInfo.InvariantCulture)} credits");
        guideText.Should().Contain($"{Disguise.WipeRoleplayXPCost.ToString("N0", CultureInfo.InvariantCulture)} Available RP XP");

        guideText.Should().Contain($"{Property.ElectionRegistrationDays}-day candidate-registration period");
        guideText.Should().Contain($"{Property.ElectionVotingDays} days of voting");
    }

    [Test]
    public void ForceAffinityTopic_ExplainsContributionMagnitudeHitChanceAndDuration()
    {
        var topic = GetTopics().Single(topic => GetString(topic, "Name") == "Force Affinity");
        var text = string.Join("\n", GetAllText(topic));

        text.Should().Contain("-10 Dark to +10 Light");
        text.Should().Contain("Additional ranks of the same perk do not contribute additional affinity");
        text.Should().Contain("increases that power's damage, healing, shields, regeneration, or drain magnitude by 5 percent");
        text.Should().Contain("At +10 Light, Light powers gain +5% hit chance and Dark powers suffer -5%");
        text.Should().Contain("Universal Force powers");
        text.Should().Contain("Force Affinity does not change effect duration");
        text.Should().Contain("At +6 Light, a Light power uses 130% magnitude and gains +3% hit chance");
    }

    [Test]
    public void CombatBasics_ExplainsCombatReadinessMagnitudeAndCooldownScope()
    {
        var topic = GetTopics().Single(topic => GetString(topic, "Name") == "Combat Basics");
        var text = string.Join("\n", GetAllText(topic));

        text.Should().Contain("activated ability damage, healing, and temporary HP");
        text.Should().Contain("does not reduce cooldowns");
    }

    [Test]
    public void NamesAndDisguises_ExplainObserverSpecificIdentityModel()
    {
        var communication = GetTopics().Single(topic => GetString(topic, "Name") == "Communication");
        var communicationText = string.Join("\n", GetAllText(communication));

        communicationText.Should().Contain("private label only your character can see");
        communicationText.Should().Contain("It does not rename the other character");
        communicationText.Should().Contain("no other player sees the label you entered");
        communicationText.Should().Contain("does not have to be the truth");
        communicationText.Should().Contain("Can two players see different names for the same character?");
        communicationText.Should().Contain("It records what your character believes");
        communicationText.Should().Contain("only Mira sees Red Coat");
        communicationText.Should().Contain("Jax still sees Tall Armored Human in gray");

        var disguises = GetTopics().Single(topic => GetString(topic, "Name") == "Disguises");
        var disguiseText = string.Join("\n", GetAllText(disguises));

        disguiseText.Should().Contain("normal identity and every disguise are remembered separately");
        disguiseText.Should().Contain("Each observer may label the same disguise differently");
        disguiseText.Should().Contain("does not hide your underlying character from staff");
        disguiseText.Should().Contain("audit logs retain the real character and account identity");
        disguiseText.Should().Contain("Each disguise has its own biography");
        disguiseText.Should().Contain("deactivation restores your normal biography");
        disguiseText.Should().Contain("do not change your equipped clothing or armor");
    }

    private static List<object> GetTopics()
    {
        var field = typeof(PlayerGuideViewModel).GetField("Topics", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        return ((IEnumerable)field!.GetValue(null)!).Cast<object>().ToList();
    }

    private static List<object> GetItems(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        property.Should().NotBeNull();

        return ((IEnumerable)property!.GetValue(source)!).Cast<object>().ToList();
    }

    private static string GetString(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        property.Should().NotBeNull();

        return property!.GetValue(source) as string ?? string.Empty;
    }

    private static IEnumerable<string> GetAllText(object topic)
    {
        yield return GetString(topic, "Name");
        yield return GetString(topic, "Category");
        yield return GetString(topic, "Summary");
        yield return GetString(topic, "RailSummary");

        foreach (var block in GetItems(topic, "Blocks"))
        {
            yield return GetString(block, "Title");
            yield return GetString(block, "Body");
        }

        foreach (var question in GetItems(topic, "Questions"))
        {
            yield return GetString(question, "Question");
            yield return GetString(question, "Answer");
        }
    }
}
