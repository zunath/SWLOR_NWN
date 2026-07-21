using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Regression coverage for Mastery.cs / Mastery view-model orchestration fixes that
/// cannot be exercised directly by an NUnit run - those methods touch DB.Get/DB.Set
/// against a live Redis connection, or NUI event plumbing, neither of which is available
/// in this test project (see MasteryRulesTests/MasteryCatalogSeedTests for the pure logic
/// that IS exercised directly). Follows the source-text-assertion pattern already used by
/// PropertyOnDemandLoadingTests for the same reason.
/// </summary>
public class MasteryOrchestrationSourceTests
{
    [Test]
    public void ApproveRequest_RejectsNonCustomRequestWhoseCatalogMasteryNoLongerResolves()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Service", "Mastery.cs")).Replace("\r\n", "\n");
        var body = ExtractMethod(source, "public static bool ApproveRequest(");

        body.Should().Contain("var existingMastery = string.IsNullOrWhiteSpace(request.MasteryId) ? null : GetMastery(request.MasteryId);");
        body.Should().Contain("if (request.Type != MasteryRequestType.Custom && existingMastery == null)");
        body.IndexOf("if (request.Type != MasteryRequestType.Custom && existingMastery == null)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(body.IndexOf("var checkMastery = existingMastery ?? new Entity.Mastery", StringComparison.Ordinal));
    }

    [Test]
    public void ApproveRequest_ChecksQuickSlotAvailabilityBeforeCreatingACustomCatalogRow()
    {
        // CreateMastery persists immediately (no transaction spans it and the
        // EnqueueTraining rejection further down), so the Quick Slot availability gate
        // must run BEFORE it - otherwise every retry of a doomed zero-slot Custom
        // approval leaves behind another orphaned catalog row.
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Service", "Mastery.cs")).Replace("\r\n", "\n");
        var body = ExtractMethod(source, "public static bool ApproveRequest(");

        body.Should().Contain("if (!MasteryRules.CanUseQuickSlot(profile, useQuickSlot, isInstant))");
        body.Should().Contain("var created = CreateMastery(");
        body.IndexOf("if (!MasteryRules.CanUseQuickSlot(profile, useQuickSlot, isInstant))", StringComparison.Ordinal)
            .Should()
            .BeLessThan(body.IndexOf("var created = CreateMastery(", StringComparison.Ordinal));
    }

    [Test]
    public void SubmitRequest_RejectsADuplicatePendingRequestBeforePersistingANewOne()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Service", "Mastery.cs")).Replace("\r\n", "\n");
        var body = ExtractMethod(source, "public static MasteryRequest SubmitRequest(");

        body.Should().Contain("MasteryRules.FindDuplicatePendingRequest(");
        body.Should().Contain("if (duplicate != null)");
        body.Should().Contain("return duplicate;");
        body.IndexOf("if (duplicate != null)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(body.IndexOf("DB.Set(request);", StringComparison.Ordinal));
    }

    [Test]
    public void CompletionNotices_AreOnlyClearedByAcknowledgeNeverByPeek()
    {
        // Splitting peek from acknowledge is what makes delivery exactly-once instead of
        // at-most-once: a UI exception between reading the notices and displaying them
        // must never be able to lose them, which requires Peek to never clear anything.
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Service", "Mastery.cs")).Replace("\r\n", "\n");

        source.Should().NotContain("DrainPendingCompletionNotices");

        var peekBody = ExtractMethod(source, "public static List<string> PeekPendingCompletionNotices(string playerId)");
        var acknowledgeBody = ExtractMethod(source, "public static void AcknowledgeCompletionNotices(string playerId)");

        peekBody.Should().NotContain(".Clear()");
        peekBody.Should().NotContain("DB.Set(profile);");
        acknowledgeBody.Should().Contain("profile.PendingCompletionNotices.Clear();");
        acknowledgeBody.Should().Contain("DB.Set(profile);");
    }

    [Test]
    public void CompletionNoticeCallers_ToastBeforeAcknowledging()
    {
        var root = FindRepositoryRoot();
        var notificationsSource = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Feature", "MasteryNotifications.cs")).Replace("\r\n", "\n");
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "MasteriesViewModel.cs")).Replace("\r\n", "\n");

        notificationsSource.Should().NotContain("DrainPendingCompletionNotices");
        viewModelSource.Should().NotContain("DrainPendingCompletionNotices");

        var loginBody = ExtractMethod(notificationsSource, "public static void NotifyMasteryUpdatesOnLogin()");
        loginBody.IndexOf("Mastery.PeekPendingCompletionNotices(playerId)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(loginBody.IndexOf("Mastery.AcknowledgeCompletionNotices(playerId)", StringComparison.Ordinal));

        var initializeBody = ExtractMethod(viewModelSource, "protected override void Initialize(GuiPayloadBase initialPayload)");
        initializeBody.IndexOf("Mastery.PeekPendingCompletionNotices(playerId)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(initializeBody.IndexOf("Mastery.AcknowledgeCompletionNotices(playerId)", StringComparison.Ordinal));
    }

    [Test]
    public void OnClickSubmitRequest_GuardsAgainstADoubleClickWhileAwaitingDiscord()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "MasteriesViewModel.cs")).Replace("\r\n", "\n");

        source.Should().Contain("private bool _isSubmittingRequest;");

        var body = ExtractMethod(source, "public Action OnClickSubmitRequest() => async () =>");

        body.Should().Contain("if (_isSubmittingRequest)");
        body.Should().Contain("_isSubmittingRequest = true;");
        body.Should().Contain("finally");
        body.Should().Contain("_isSubmittingRequest = false;");

        // The guard must be checked before the request is ever persisted via
        // Mastery.SubmitRequest, and reset in a finally so a validation failure or
        // Discord error never leaves the form permanently locked.
        body.IndexOf("if (_isSubmittingRequest)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(body.IndexOf("Mastery.SubmitRequest(", StringComparison.Ordinal));
        body.IndexOf("_isSubmittingRequest = true;", StringComparison.Ordinal)
            .Should()
            .BeLessThan(body.IndexOf("Mastery.SubmitRequest(", StringComparison.Ordinal));
    }

    [Test]
    public void OnClickReduceTraining_RevalidatesTheActiveEntryBeforeMutating()
    {
        // row is a stale UI snapshot, while Mastery.ReduceTrainingTime always targets
        // whichever entry is currently at TrainingQueue[0] - if the queue advanced
        // between load and click, a reduce without this re-check would silently apply to
        // the wrong mastery.
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "DMPlayerExamineViewModel.cs")).Replace("\r\n", "\n");
        var body = ExtractMethod(source, "public Action OnClickReduceTraining() => () =>");

        body.Should().Contain("Mastery.GetOrCreateProfile(_playerId)");
        body.Should().Contain("profile.TrainingQueue[0].MasteryId != masteryId");
        body.Should().Contain("profile.TrainingQueue[0].TargetTier != targetTier");

        var revalidateIndex = body.IndexOf("profile.TrainingQueue[0].MasteryId != masteryId", StringComparison.Ordinal);
        var mutateIndex = body.IndexOf("Mastery.ReduceTrainingTime(", StringComparison.Ordinal);
        revalidateIndex.Should().BeLessThan(mutateIndex);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should exist");

        var bodyStart = source.IndexOf('{', start);
        bodyStart.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should have a body");

        var depth = 0;
        for (var i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(start, i - start + 1);
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
