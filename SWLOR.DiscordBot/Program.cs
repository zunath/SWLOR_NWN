using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Discord;
using SWLOR.DiscordBot.Hosting;
using SWLOR.DiscordBot.Persistence;

namespace SWLOR.DiscordBot;

public static class Program
{
    internal static GatewayIntents GatewayIntentsFor(BotConfiguration config)
    {
        // Ticket transcripts use REST; their application-level content access is validated separately at startup.
        var intents = GatewayIntents.Guilds;
        if (config.Welcome.Enabled) intents |= GatewayIntents.GuildMembers;
        if (config.Factions.Enabled || config.Answers.Any(x => x.Enabled))
            intents |= GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
        return intents;
    }

    internal static void ValidateApplicationCapabilities(BotConfiguration config, ApplicationFlags flags)
    {
        var intents = GatewayIntentsFor(config);
        if ((intents & GatewayIntents.GuildMembers) != 0 &&
            (flags & (ApplicationFlags.GatewayGuildMembers | ApplicationFlags.GatewayGuildMembersLimited)) == 0)
            throw new DiscordValidationException("Enable Server Members Intent for the Discord application to deliver welcome messages.");
        if ((intents & GatewayIntents.MessageContent) != 0 &&
            (flags & (ApplicationFlags.GatewayMessageContent | ApplicationFlags.GatewayMessageContentLimited)) == 0)
            throw new DiscordValidationException("Enable Message Content Intent for the Discord application to run faction and quick-answer commands.");
        if (config.Tickets.Enabled) DiscordOperations.ValidateTranscriptCapability(flags);
    }

    internal static async Task StartGatewayAsync(BotConfiguration config,
        Func<Task<ApplicationFlags>> applicationFlags, Func<Task> start)
    {
        // Application flags are available after REST login, before an Identify with disallowed intents can be rejected.
        ValidateApplicationCapabilities(config, await applicationFlags());
        await start();
    }

    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--health-check", StringComparer.Ordinal)) return ReadinessMarker.IsHealthy() ? 0 : 1;
        var configPath = "/config/bot.json";
        var validateOnly = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--validate") validateOnly = true;
            else if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else { Console.Error.WriteLine("Usage: SWLOR.DiscordBot [--config path] [--validate] | --health-check"); return 2; }
        }
        BotConfiguration config;
        try { config = ConfigurationLoader.Load(configPath); }
        catch (Exception ex) { Console.Error.WriteLine($"Cannot load bot configuration ({ex.GetType().Name})."); return 2; }
        var errors = ConfigurationValidator.Validate(config);
        if (errors.Count > 0)
        {
            foreach (var error in errors) Console.Error.WriteLine(error);
            return 2;
        }
        if (validateOnly) { Console.WriteLine("Bot configuration is valid."); return 0; }
        BotSecrets secrets;
        try { secrets = BotSecrets.Load(); }
        catch (Exception ex) { Console.Error.WriteLine($"Cannot load bot secrets/database settings ({ex.GetType().Name})."); return 2; }
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; });
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(secrets);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntentsFor(config),
            AlwaysDownloadUsers = false, MessageCacheSize = 0, LogLevel = LogSeverity.Warning,
            DefaultRetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502
        }));
        builder.Services.AddSingleton<ReadinessMarker>();
        builder.Services.AddSingleton<ResponseDeletionQueue>();
        builder.Services.AddSingleton<DiscordOperations>();
        builder.Services.AddHttpClient<DiscordCommunityPoster>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.AddSingleton<IDiscordTickets>(provider => provider.GetRequiredService<DiscordOperations>());
        builder.Services.AddSingleton<ICommunityDiscord>(provider => provider.GetRequiredService<DiscordOperations>());
        builder.Services.AddSingleton(_ => new PostgresTicketStore(secrets.Database, config.GuildId));
        builder.Services.AddSingleton<ITicketStore>(provider => provider.GetRequiredService<PostgresTicketStore>());
        builder.Services.AddSingleton<IResponseDeletionStore>(provider => provider.GetRequiredService<PostgresTicketStore>());
        builder.Services.AddHttpClient("transcripts", client => client.Timeout = TimeSpan.FromMinutes(2))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.AddSingleton<ITranscriptArchive>(provider => new FileTranscriptArchive(config,
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("transcripts")));
        builder.Services.AddSingleton<TicketService>();
        builder.Services.AddSingleton<CommunityService>();
        builder.Services.AddSingleton<DiscordGateway>();
        builder.Services.AddHostedService<BotWorker>();
        using var host = builder.Build();
        await host.RunAsync();
        return Environment.ExitCode;
    }
}
