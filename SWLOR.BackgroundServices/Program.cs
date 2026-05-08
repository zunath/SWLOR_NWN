using Serilog;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.BackgroundJobs;
using SWLOR.BackgroundServices.BackgroundJobs.Handlers;
using SWLOR.BackgroundServices.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var settings = BackgroundServiceSettings.FromEnvironment();
var logger = new ConsoleAppLogger();
var httpClient = new HttpClient
{
    Timeout = settings.HttpTimeout
};

var handlers = new Dictionary<string, IBackgroundJobHandler>(StringComparer.OrdinalIgnoreCase)
{
    [BackgroundJobTypes.GitHubIssue] = new GitHubIssueJobHandler(httpClient, settings),
    [BackgroundJobTypes.DiscordWebhook] = new DiscordWebhookJobHandler(httpClient)
};

var processor = new BackgroundJobProcessor(handlers, new BackgroundJobFailureHandler(settings, logger), logger);
var worker = new BackgroundJobWorker(settings, processor, logger);

using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdown.Cancel();

try
{
    await worker.RunAsync(shutdown.Token);
}
finally
{
    Log.CloseAndFlush();
}
