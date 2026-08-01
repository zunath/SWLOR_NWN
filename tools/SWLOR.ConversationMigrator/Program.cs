using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;

var repositoryRoot = args.Length > 0
    ? Path.GetFullPath(args[0])
    : FindRepositoryRoot(AppContext.BaseDirectory);
var overwrite = args.Any(argument => argument.Equals("--overwrite", StringComparison.OrdinalIgnoreCase));
var sourceDirectory = Path.Combine(repositoryRoot, "Module", "dlg");
var outputDirectory = Path.Combine(repositoryRoot, "SWLOR.Game.Server", "ConversationData");

if (!Directory.Exists(sourceDirectory))
    throw new DirectoryNotFoundException($"Conversation source directory '{sourceDirectory}' was not found.");

Directory.CreateDirectory(outputDirectory);

var report = new List<MigrationReportEntry>();
var generatedShellPattern = new Regex("^dialog[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
foreach (var sourcePath in Directory.GetFiles(sourceDirectory, "*.dlg.json").OrderBy(path => path))
{
    var fileName = Path.GetFileName(sourcePath);
    var conversationId = fileName[..^".dlg.json".Length];
    if (generatedShellPattern.IsMatch(conversationId))
    {
        report.Add(new MigrationReportEntry(conversationId, "GeneratedShell", Array.Empty<ConversationMigrationIssue>()));
        continue;
    }

    try
    {
        var result = DlgConversationMigrator.Convert(conversationId, DlgDocument.Load(sourcePath));
        if (!result.CanRunInNui)
        {
            report.Add(new MigrationReportEntry(conversationId, "RequiresLegacyException", result.Issues));
            continue;
        }

        var outputPath = Path.Combine(outputDirectory, conversationId + ".conversation.json");
        if (File.Exists(outputPath) && !overwrite)
            throw new IOException($"Output '{outputPath}' already exists. Pass --overwrite to replace generated graphs.");

        File.WriteAllText(outputPath, JsonConvert.SerializeObject(result.Graph, Formatting.Indented));
        report.Add(new MigrationReportEntry(conversationId, "Converted", result.Issues));
    }
    catch (Exception exception)
    {
        report.Add(new MigrationReportEntry(
            conversationId,
            "ConversionFailed",
            new[]
            {
                new ConversationMigrationIssue(
                    ConversationMigrationIssueSeverity.RequiresLegacyException,
                    "file",
                    exception.Message)
            }));
    }
}

var reportPath = Path.Combine(outputDirectory, "migration-report.json");
File.WriteAllText(reportPath, JsonConvert.SerializeObject(report, Formatting.Indented));

var legacyExceptions = report
    .Where(entry => entry.Status == "RequiresLegacyException")
    .Select(entry => new LegacyExceptionEntry(
        entry.ConversationId,
        entry.Issues
            .Where(issue => issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException)
            .Select(issue => issue.Message)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(message => message)
            .ToArray()))
    .ToArray();
var exceptionPath = Path.Combine(outputDirectory, "legacy-exceptions.json");
File.WriteAllText(exceptionPath, JsonConvert.SerializeObject(legacyExceptions, Formatting.Indented));

var convertedIds = report
    .Where(entry => entry.Status == "Converted")
    .Select(entry => entry.ConversationId)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var routing = ModuleConversationRouter.RouteRepository(repositoryRoot, convertedIds);

if (overwrite)
{
    foreach (var graphPath in Directory.GetFiles(outputDirectory, "*.conversation.json"))
    {
        var graphId = Path.GetFileName(graphPath)[..^".conversation.json".Length];
        if (!convertedIds.Contains(graphId))
            File.Delete(graphPath);
    }
}

var converted = report.Count(entry => entry.Status == "Converted");
var exceptions = report.Count(entry => entry.Status == "RequiresLegacyException");
var failed = report.Count(entry => entry.Status == "ConversionFailed");
var shells = report.Count(entry => entry.Status == "GeneratedShell");
Console.WriteLine($"Converted: {converted}");
Console.WriteLine($"Legacy exceptions: {exceptions}");
Console.WriteLine($"Conversion failures: {failed}");
Console.WriteLine($"Generated shells skipped: {shells}");
Console.WriteLine($"Module conversation references found: {routing.ReferencesFound}");
Console.WriteLine($"Module conversation references routed: {routing.ReferencesUpdated}");
Console.WriteLine($"Module conversation references already routed: {routing.ReferencesAlreadyRouted}");
foreach (var issue in routing.Issues)
    Console.WriteLine($"Routing error: {issue.FilePath} ({issue.ConversationId}): {issue.Message}");
Console.WriteLine($"Report: {reportPath}");
Console.WriteLine($"Legacy exceptions: {exceptionPath}");

return failed == 0 && routing.Issues.Count == 0 ? 0 : 1;

static string FindRepositoryRoot(string startingPath)
{
    var directory = new DirectoryInfo(startingPath);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        directory = directory.Parent;

    return directory?.FullName ??
           throw new DirectoryNotFoundException("Could not locate the SWLOR repository root.");
}

internal sealed record MigrationReportEntry(
    string ConversationId,
    string Status,
    IReadOnlyList<ConversationMigrationIssue> Issues);

internal sealed record LegacyExceptionEntry(
    string ConversationId,
    IReadOnlyList<string> Reasons);
