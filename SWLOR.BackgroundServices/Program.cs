using Serilog;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.BackgroundJobs;
using SWLOR.BackgroundServices.BackgroundJobs.Handlers;
using SWLOR.BackgroundServices.DiscordEvents;
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
var discordEventSync = new DiscordEventSync(settings, httpClient, logger);

using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdown.Cancel();

try
{
    // Discord sync returns at once when it is not configured; the worker keeps running.
    var loops = new List<Task> { worker.RunAsync(shutdown.Token), discordEventSync.RunAsync(shutdown.Token) };
    while (loops.Count > 0)
    {
        var finished = await Task.WhenAny(loops);
        loops.Remove(finished);
        if (finished.Status == TaskStatus.RanToCompletion || finished.IsCanceled && shutdown.IsCancellationRequested)
            continue;

        // A failed loop stops the whole service so its host restarts it, instead of leaving the other loop running alone.
        shutdown.Cancel();
        await Task.WhenAny(Task.WhenAll(loops), Task.Delay(TimeSpan.FromSeconds(10)));
        await finished;
    }
}
finally
{
    Log.CloseAndFlush();
}
