using System.Text.RegularExpressions;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Configuration;

public static partial class ConfigurationValidator
{
    private static readonly TimeSpan MaximumQuickAnswerCooldown = TimeSpan.FromDays(30);
    private const int MaximumMessageLength = 2000;
    private const int MaximumEmbedFields = 25;
    private const int MaximumEmbedTextLength = 6000;

    public static IReadOnlyList<string> Validate(BotConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var errors = new List<string>();
        if (configuration.GuildId == 0) errors.Add("guildId must be a positive Discord ID.");
        if (string.IsNullOrWhiteSpace(configuration.Prefix) || configuration.Prefix.Length > 8 || configuration.Prefix.Any(char.IsWhiteSpace) || configuration.Prefix.Any(char.IsControl))
            errors.Add("prefix must contain 1 to 8 non-whitespace, printable characters.");

        if (configuration.Tickets is null) errors.Add("tickets must not be null.");
        if (configuration.Welcome is null) errors.Add("welcome must not be null.");
        if (configuration.Factions is null) errors.Add("factions must not be null.");
        if (configuration.Answers is null) errors.Add("answers must not be null.");

        ValidateUniqueIds(configuration.AdministratorRoleIds, "administratorRoleIds", errors);

        if (configuration.Tickets?.Enabled == true) ValidateTickets(configuration.Tickets, errors);
        if (configuration.Welcome?.Enabled == true) ValidateWelcome(configuration.Welcome, errors);
        if (configuration.Factions?.Enabled == true) ValidateFactions(configuration, errors);
        if (configuration.Answers is not null)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < configuration.Answers.Length; i++)
            {
                var answer = configuration.Answers[i];
                if (answer is null) { errors.Add($"answers[{i}] must not be null."); continue; }
                if (!answer.Enabled) continue;
                var name = answer.Name ?? "";
                if (!IsSafeCommandName(name)) errors.Add($"answers[{i}].name must be a safe command name without surrounding whitespace (letters, digits, underscore, or hyphen; 1 to 32 characters).");
                else if (!seen.Add(name)) errors.Add($"Duplicate quick answer command '{name}'.");
                if (configuration.Factions?.Enabled == true && string.Equals(name, "rank", StringComparison.OrdinalIgnoreCase)) errors.Add("The quick answer command 'rank' is reserved for faction role selection.");
                if (answer.Cooldown < TimeSpan.Zero || answer.Cooldown > MaximumQuickAnswerCooldown) errors.Add($"answers[{i}].cooldown must be between zero and 30 days.");
                if (answer.DeleteResponseAfter is { } deleteAfter && deleteAfter <= TimeSpan.Zero) errors.Add($"answers[{i}].deleteResponseAfter must be positive when set.");
                if (answer.DeleteResponseAfter is { } responseDeleteAfter && responseDeleteAfter > TimeSpan.FromDays(7)) errors.Add($"answers[{i}].deleteResponseAfter must not exceed 7 days.");
                if (answer.Embeds is null) errors.Add($"answers[{i}].embeds must not be null.");
                if ((answer.Responses?.Length ?? 0) == 0 && (answer.Embeds?.Length ?? 0) == 0) errors.Add($"answers[{i}] must have at least one response or embed.");
                if (answer.Responses is not null)
                    for (var j = 0; j < answer.Responses.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(answer.Responses[j])) errors.Add($"answers[{i}].responses[{j}] must not be empty.");
                        if (answer.Responses[j]?.Length > MaximumMessageLength) errors.Add($"answers[{i}].responses[{j}] exceeds the 2000 character message limit.");
                        ValidateTemplate(answer.Responses[j], TemplateKind.Answer, $"answers[{i}].responses[{j}]", errors);
                    }
                if (answer.Embeds is not null)
                {
                    if (answer.Embeds.Length > 10) errors.Add($"answers[{i}].embeds exceeds the 10 embed limit.");
                    var combinedEmbedText = 0;
                    for (var j = 0; j < answer.Embeds.Length; j++)
                    {
                        var embed = answer.Embeds[j];
                        if (embed is null) { errors.Add($"answers[{i}].embeds[{j}] must not be null."); continue; }
                        if (embed.Url is { Length: > 0 } url && (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(parsed.UserInfo) || url.Length > 2048)) errors.Add($"answers[{i}].embeds[{j}].url must be a safe absolute HTTP or HTTPS URL without credentials and at most 2048 characters.");
                        if (string.IsNullOrWhiteSpace(embed.Title) && string.IsNullOrWhiteSpace(embed.Description) && (embed.Fields?.Length ?? 0) == 0)
                            errors.Add($"answers[{i}].embeds[{j}] must contain a title, description, or field.");
                        if (embed.Title?.Length > 256) errors.Add($"answers[{i}].embeds[{j}].title exceeds the 256 character embed title limit.");
                        if (embed.Description?.Length > 4096) errors.Add($"answers[{i}].embeds[{j}].description exceeds the 4096 character embed description limit.");
                        if (embed.Color > 0xFFFFFF) errors.Add($"answers[{i}].embeds[{j}].color must be a 24-bit RGB value.");
                        combinedEmbedText += (embed.Title?.Length ?? 0) + (embed.Description?.Length ?? 0);
                        if (embed.Title is not null) ValidateTemplate(embed.Title, TemplateKind.Answer, $"answers[{i}].embeds[{j}].title", errors);
                        if (embed.Description is not null) ValidateTemplate(embed.Description, TemplateKind.Answer, $"answers[{i}].embeds[{j}].description", errors);
                        if (embed.Fields is not null)
                        {
                            if (embed.Fields.Length > MaximumEmbedFields) errors.Add($"answers[{i}].embeds[{j}] exceeds the 25 field limit.");
                            var totalTextLength = (embed.Title?.Length ?? 0) + (embed.Description?.Length ?? 0);
                            for (var k = 0; k < embed.Fields.Length; k++)
                            {
                                var field = embed.Fields[k];
                                if (field is null) { errors.Add($"answers[{i}].embeds[{j}].fields[{k}] must not be null."); continue; }
                                if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Value)) errors.Add($"answers[{i}].embeds[{j}].fields[{k}] needs a name and value.");
                                if (field.Name?.Length > 256) errors.Add($"answers[{i}].embeds[{j}].fields[{k}].name exceeds the 256 character limit.");
                                if (field.Value?.Length > 1024) errors.Add($"answers[{i}].embeds[{j}].fields[{k}].value exceeds the 1024 character limit.");
                                totalTextLength += (field.Name?.Length ?? 0) + (field.Value?.Length ?? 0);
                                ValidateTemplate(field.Name, TemplateKind.Answer, $"answers[{i}].embeds[{j}].fields[{k}].name", errors);
                                ValidateTemplate(field.Value, TemplateKind.Answer, $"answers[{i}].embeds[{j}].fields[{k}].value", errors);
                            }
                            if (totalTextLength > MaximumEmbedTextLength) errors.Add($"answers[{i}].embeds[{j}] exceeds the combined 6000 character text limit.");
                            combinedEmbedText += embed.Fields.Where(x => x is not null).Sum(x => (x.Name?.Length ?? 0) + (x.Value?.Length ?? 0));
                        }
                    }
                    if (combinedEmbedText > MaximumEmbedTextLength) errors.Add($"answers[{i}] exceeds the combined 6000 character text limit across embeds.");
                }
                ValidateUniqueIds(answer.AllowedRoleIds, $"answers[{i}].allowedRoleIds", errors);
                ValidateUniqueIds(answer.AllowedChannelIds, $"answers[{i}].allowedChannelIds", errors);
            }
        }
        return errors;
    }

    public static IReadOnlyList<string> ValidatePersistedTickets(BotConfiguration configuration, IReadOnlyList<Ticket> persistedTickets)
    {
        ArgumentNullException.ThrowIfNull(persistedTickets);
        var errors = Validate(configuration).ToList();
        if (configuration.Tickets is not { } options) return errors;
        var retained = persistedTickets.Where(ticket => ticket.State != TicketState.Deleted || ticket.ArchivePath is not null || ticket.ArchiveSnapshotPath is not null).ToArray();
        if (retained.Length == 0) return errors;
        var channelMaintenance = retained.Any(ticket => ticket.State != TicketState.Deleted);
        // Enabled controls accepting new tickets, not ownership of existing channel mutations.
        if (channelMaintenance && !options.Enabled)
        {
            ValidateRetainedTickets(options, retained.Where(ticket => ticket.State != TicketState.Deleted).ToArray(), errors);
            if (configuration.Factions?.Enabled == true)
            {
                var retainedRoles = (options.SupportRoleIds ?? []).ToHashSet();
                if (TicketMaintenanceRequirements.RequiresBypassRoles(configuration, persistedTickets))
                    retainedRoles.UnionWith(options.BypassRoleIds ?? []);
                foreach (var role in configuration.Factions.Roles ?? [])
                    if (role is not null && retainedRoles.Contains(role.RoleId))
                        errors.Add($"Faction role '{role.Name}' overlaps a ticket support or bypass role retained for persisted maintenance.");
            }
        }
        foreach (var ticket in retained.Where(ticket => TicketMaintenanceRequirements.RequiresPanel(ticket.State)))
            if (!(options.Panels ?? []).Any(panel => panel is not null && panel.Id == ticket.PanelId))
                errors.Add($"tickets.panels must retain panel '{ticket.PanelId}' required to keep persisted ticket {ticket.Id} reopenable.");

        // Deleted tickets only need archive ownership; expired archives must not depend on panel/role configuration.
        if (!channelMaintenance && !options.Enabled) ValidateArchiveDirectory(options.ArchiveDirectory, errors);
        if (TryArchiveRoot(options.ArchiveDirectory, out var root))
            foreach (var ticket in retained.Where(ticket => ticket.ArchivePath is not null || ticket.ArchiveSnapshotPath is not null))
            {
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                var expected = Path.Combine(root!, ticket.Id.ToString("N"));
                if (!TryArchiveRoot(ticket.ArchivePath!, out var archivePath) || !string.Equals(archivePath, expected, comparison))
                    errors.Add($"tickets.archiveDirectory must retain ownership of the persisted archive for ticket {ticket.Id}; restore its original archive root before startup.");
                if (ticket.ArchiveSnapshotPath is not null &&
                    (!TryArchiveRoot(ticket.ArchiveSnapshotPath, out var snapshotPath) ||
                     !Guid.TryParseExact(Path.GetFileName(snapshotPath), "N", out var snapshotId) ||
                     !string.Equals(snapshotPath, Path.Combine(expected, "snapshots", snapshotId.ToString("N")), comparison)))
                    errors.Add($"Persisted snapshot for ticket {ticket.Id} must remain inside its owned archive directory.");
            }
        return errors;
    }

    private static void ValidateArchiveDirectory(string? directory, List<string> errors)
    {
        if (!TryArchiveRoot(directory, out _)) errors.Add("tickets.archiveDirectory must be an absolute path.");
    }

    private static bool TryArchiveRoot(string? path, out string? root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) return false;
        try { root = Path.GetFullPath(path); return true; }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    private static void ValidateTickets(TicketOptions tickets, List<string> errors)
    {
        if (tickets.Panels is null || tickets.Panels.Length == 0) errors.Add("tickets.panels must contain at least one panel when tickets are enabled or persisted channels require maintenance.");
        if (tickets.SupportRoleIds is null || tickets.SupportRoleIds.Length == 0) errors.Add("tickets.supportRoleIds must contain at least one role when tickets are enabled or persisted channels require maintenance.");
        if ((tickets.BypassRoleIds?.Length ?? 0) > 0 && (tickets.BypassMemberLimit is null || tickets.BypassPanelLimit is null || tickets.BypassGuildLimit is null))
            errors.Add("tickets.bypassMemberLimit, bypassPanelLimit, and bypassGuildLimit must all be explicitly set when bypassRoleIds are configured.");
        var panelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (tickets.Panels is not null)
            for (var i = 0; i < tickets.Panels.Length; i++)
            {
                var panel = tickets.Panels[i];
                if (panel is null) { errors.Add($"tickets.panels[{i}] must not be null."); continue; }
                if (!IsSafeIdentifier(panel.Id)) errors.Add($"tickets.panels[{i}].id must be a safe identifier (letters, digits, underscore, or hyphen; 1 to 32 characters).");
                else if (!panelIds.Add(panel.Id)) errors.Add($"Duplicate ticket panel id '{panel.Id}'.");
                RequireId(panel.ChannelId, $"tickets.panels[{i}].channelId", errors);
                if (panel.OpenLimit <= 0) errors.Add($"tickets.panels[{i}].openLimit must be positive.");
                if (string.IsNullOrWhiteSpace(panel.Label) || panel.Label.Length > 80) errors.Add($"tickets.panels[{i}].label must contain 1 to 80 characters.");
                ValidateTemplate(panel.OpeningMessage, TemplateKind.Ticket, $"tickets.panels[{i}].openingMessage", errors);
                ValidateTemplate(panel.PanelMessage, TemplateKind.Ticket, $"tickets.panels[{i}].panelMessage", errors);
                if (panel.OpeningMessage?.Length > MaximumMessageLength) errors.Add($"tickets.panels[{i}].openingMessage exceeds the 2000 character message limit.");
                if (panel.PanelMessage?.Length > MaximumMessageLength) errors.Add($"tickets.panels[{i}].panelMessage exceeds the 2000 character message limit.");
                if (string.IsNullOrWhiteSpace(panel.PanelMessage)) errors.Add($"tickets.panels[{i}].panelMessage must not be empty.");
                ValidateUniqueIds(panel.OpenCategoryIds, $"tickets.panels[{i}].openCategoryIds", errors);
                if (panel.OpenCategoryIds is null || panel.OpenCategoryIds.Length == 0) errors.Add($"tickets.panels[{i}].openCategoryIds must contain at least one category.");
            }
        ValidateUniqueIds(tickets.SupportRoleIds, "tickets.supportRoleIds", errors);
        ValidateUniqueIds(tickets.BypassRoleIds, "tickets.bypassRoleIds", errors);
        RequireId(tickets.ClosedCategoryId, "tickets.closedCategoryId", errors);
        RequireId(tickets.LogChannelId, "tickets.logChannelId", errors);
        if (tickets.MemberLimit <= 0) errors.Add("tickets.memberLimit must be positive.");
        if (tickets.GuildLimit <= 0) errors.Add("tickets.guildLimit must be positive.");
        if (tickets.CleanupDelay <= TimeSpan.Zero) errors.Add("tickets.cleanupDelay must be positive.");
        if (tickets.CleanupInterval <= TimeSpan.Zero) errors.Add("tickets.cleanupInterval must be positive.");
        if (tickets.CleanupInterval > TimeSpan.FromDays(30)) errors.Add("tickets.cleanupInterval must not exceed 30 days.");
        if (tickets.ArchiveRetentionDays <= 0 || tickets.ArchiveRetentionDays <= tickets.CleanupDelay.TotalDays) errors.Add("tickets.archiveRetentionDays must be positive and longer than cleanupDelay.");
        if (tickets.ArchiveRetentionDays > 3650) errors.Add("tickets.archiveRetentionDays must not exceed 3650 days.");
        ValidateArchiveDirectory(tickets.ArchiveDirectory, errors);
        if (tickets.MaxAttachmentBytes <= 0) errors.Add("tickets.maxAttachmentBytes must be positive.");
        if (tickets.MaxAttachmentBytes > 10737418240L) errors.Add("tickets.maxAttachmentBytes must not exceed 10 GiB.");
        if (tickets.MaxTicketAttachmentBytes <= 0) errors.Add("tickets.maxTicketAttachmentBytes must be positive.");
        if (tickets.MaxTicketAttachmentBytes > 10737418240L) errors.Add("tickets.maxTicketAttachmentBytes must not exceed 10 GiB.");
        if (tickets.MaxTranscriptContentBytes <= 0) errors.Add("tickets.maxTranscriptContentBytes must be positive.");
        if (tickets.MaxTranscriptContentBytes > 67108864L) errors.Add("tickets.maxTranscriptContentBytes must not exceed 64 MiB.");
    }

    private static void ValidateRetainedTickets(TicketOptions tickets, IReadOnlyList<Ticket> retained, List<string> errors)
    {
        if (tickets.SupportRoleIds is null || tickets.SupportRoleIds.Length == 0)
            errors.Add("tickets.supportRoleIds must contain at least one role when persisted channels require maintenance.");
        ValidateUniqueIds(tickets.SupportRoleIds, "tickets.supportRoleIds", errors);
        var panelIds = retained.Where(ticket => TicketMaintenanceRequirements.RequiresPanel(ticket.State))
            .Select(ticket => ticket.PanelId).ToHashSet(StringComparer.Ordinal);
        if (panelIds.Count > 0 && (tickets.Panels is null || tickets.Panels.Length == 0))
            errors.Add("tickets.panels must retain the panels required by persisted tickets.");

        var needsReopenCapacity = retained.Any(ticket => TicketMaintenanceRequirements.RequiresPanel(ticket.State));
        if (needsReopenCapacity)
        {
            ValidateUniqueIds(tickets.BypassRoleIds, "tickets.bypassRoleIds", errors);
            if ((tickets.BypassRoleIds?.Length ?? 0) > 0 && (tickets.BypassMemberLimit is null || tickets.BypassPanelLimit is null || tickets.BypassGuildLimit is null))
                errors.Add("tickets.bypassMemberLimit, bypassPanelLimit, and bypassGuildLimit must all be explicitly set when bypassRoleIds are configured.");
            if (tickets.MemberLimit <= 0) errors.Add("tickets.memberLimit must be positive.");
            if (tickets.GuildLimit <= 0) errors.Add("tickets.guildLimit must be positive.");
        }

        var seenPanelIds = new HashSet<string>(StringComparer.Ordinal);
        if (tickets.Panels is not null)
            for (var i = 0; i < tickets.Panels.Length; i++)
            {
                var panel = tickets.Panels[i];
                if (panel is null) continue;
                if (!panelIds.Contains(panel.Id)) continue;
                if (!IsSafeIdentifier(panel.Id)) errors.Add($"tickets.panels[{i}].id must be a safe identifier (letters, digits, underscore, or hyphen; 1 to 32 characters).");
                else if (!seenPanelIds.Add(panel.Id)) errors.Add($"Duplicate ticket panel id '{panel.Id}'.");

                if (panel.OpenLimit <= 0) errors.Add($"tickets.panels[{i}].openLimit must be positive for persisted ticket reopen capacity.");
                ValidateUniqueIds(panel.OpenCategoryIds, $"tickets.panels[{i}].openCategoryIds", errors);
                if (panel.OpenCategoryIds is null || panel.OpenCategoryIds.Length == 0)
                    errors.Add($"tickets.panels[{i}].openCategoryIds must contain at least one category for persisted ticket creation or reopening.");
                if (retained.Any(ticket => ticket.State == TicketState.Creating && string.Equals(ticket.PanelId, panel.Id, StringComparison.Ordinal)))
                {
                    ValidateTemplate(panel.OpeningMessage, TemplateKind.Ticket, $"tickets.panels[{i}].openingMessage", errors);
                    if (panel.OpeningMessage?.Length > MaximumMessageLength) errors.Add($"tickets.panels[{i}].openingMessage exceeds the 2000 character message limit.");
                }
            }

        var closeCapable = retained.Any(ticket => TicketMaintenanceRequirements.RequiresPanel(ticket.State));
        if (closeCapable)
        {
            RequireId(tickets.ClosedCategoryId, "tickets.closedCategoryId", errors);
            if (tickets.CleanupDelay <= TimeSpan.Zero) errors.Add("tickets.cleanupDelay must be positive.");
            if (tickets.ArchiveRetentionDays <= 0 || tickets.ArchiveRetentionDays <= tickets.CleanupDelay.TotalDays)
                errors.Add("tickets.archiveRetentionDays must be positive and longer than cleanupDelay.");
        }
        else if (retained.Any(ticket => ticket.State is TicketState.Closing or TicketState.Deleting))
        {
            // Interrupted close/deletion can reconcile without reapplying a close deadline, but a missing channel
            // still needs a bounded archive expiration policy.
            if (tickets.ArchiveRetentionDays <= 0) errors.Add("tickets.archiveRetentionDays must be positive.");
        }

        if (retained.Any(ticket => TicketMaintenanceRequirements.RequiresPanel(ticket.State) || ticket.State == TicketState.Deleting))
        {
            RequireId(tickets.LogChannelId, "tickets.logChannelId", errors);
            ValidateArchiveDirectory(tickets.ArchiveDirectory, errors);
            if (tickets.ArchiveRetentionDays > 3650) errors.Add("tickets.archiveRetentionDays must not exceed 3650 days.");
            if (tickets.MaxAttachmentBytes <= 0) errors.Add("tickets.maxAttachmentBytes must be positive.");
            if (tickets.MaxAttachmentBytes > 10737418240L) errors.Add("tickets.maxAttachmentBytes must not exceed 10 GiB.");
            if (tickets.MaxTicketAttachmentBytes <= 0) errors.Add("tickets.maxTicketAttachmentBytes must be positive.");
            if (tickets.MaxTicketAttachmentBytes > 10737418240L) errors.Add("tickets.maxTicketAttachmentBytes must not exceed 10 GiB.");
            if (tickets.MaxTranscriptContentBytes <= 0) errors.Add("tickets.maxTranscriptContentBytes must be positive.");
            if (tickets.MaxTranscriptContentBytes > 67108864L) errors.Add("tickets.maxTranscriptContentBytes must not exceed 64 MiB.");
        }
    }

    private static void ValidateWelcome(WelcomeOptions welcome, List<string> errors)
    {
        if (welcome.ChannelMentions is null) errors.Add("welcome.channelMentions must not be null.");
        if (!welcome.DirectMessage) RequireId(welcome.ChannelId, "welcome.channelId", errors);
        ValidateTemplate(welcome.Template, TemplateKind.Welcome, "welcome.template", errors, welcome.ChannelMentions);
        if (string.IsNullOrWhiteSpace(welcome.Template)) errors.Add("welcome.template must not be empty when welcome messages are enabled.");
        if (welcome.Template?.Length > MaximumMessageLength) errors.Add("welcome.template exceeds the 2000 character message limit.");
        foreach (var pair in welcome.ChannelMentions ?? [])
        {
            if (!IsSafeIdentifier(pair.Key)) errors.Add($"welcome.channelMentions key '{pair.Key}' must be a safe channel name.");
            RequireId(pair.Value, $"welcome.channelMentions['{pair.Key}']", errors);
        }
    }

    private static void ValidateFactions(BotConfiguration config, List<string> errors)
    {
        var factions = config.Factions;
        if (factions.Roles is null || factions.Roles.Length == 0) errors.Add("factions.roles must contain at least one role when factions are enabled.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<ulong>();
        var protectedRoles = (config.AdministratorRoleIds ?? []).ToHashSet();
        if (config.Tickets?.Enabled == true)
        {
            protectedRoles.UnionWith(config.Tickets.SupportRoleIds ?? []);
            protectedRoles.UnionWith(config.Tickets.BypassRoleIds ?? []);
        }
        if (factions.Roles is not null)
            for (var i = 0; i < factions.Roles.Length; i++)
            {
                var role = factions.Roles[i];
                if (role is null) { errors.Add($"factions.roles[{i}] must not be null."); continue; }
                if (!IsSafeFactionName(role.Name)) errors.Add($"factions.roles[{i}].name must be a safe faction name (letters, digits, spaces, underscore, or hyphen; 1 to 64 characters).");
                else if (!names.Add(role.Name.Trim())) errors.Add($"Duplicate faction command '{role.Name}'.");
                RequireId(role.RoleId, $"factions.roles[{i}].roleId", errors);
                if (role.RoleId != 0 && !ids.Add(role.RoleId)) errors.Add($"Duplicate faction role ID {role.RoleId}.");
                if (role.RoleId != 0 && protectedRoles.Contains(role.RoleId)) errors.Add($"Faction role '{role.Name}' overlaps an administrator or ticket support role, or a ticket bypass role.");
            }
        if (factions.Exclusive is null) errors.Add("factions.exclusive must be set when factions are enabled.");
        if (factions.Behavior is null || !(factions.Behavior.Equals("toggle", StringComparison.OrdinalIgnoreCase) || factions.Behavior.Equals("join", StringComparison.OrdinalIgnoreCase))) errors.Add("factions.behavior must be 'toggle' or 'join'.");
    }

    private enum TemplateKind { Welcome, Answer, Ticket }
    private static void ValidateTemplate(string? template, TemplateKind kind, string path, List<string> errors, IReadOnlyDictionary<string, ulong>? channelMentions = null)
    {
        if (template is null) { errors.Add($"{path} must not be null."); return; }
        var pattern = new Regex(@"\{([^{}]*)\}", RegexOptions.CultureInvariant);
        var matches = pattern.Matches(template);
        foreach (Match match in matches)
        {
            var macro = match.Groups[1].Value;
            var valid = kind switch
            {
                TemplateKind.Welcome => macro is "user" or "server" || macro.StartsWith('#') && IsSafeIdentifier(macro[1..]),
                TemplateKind.Answer => macro is "user" or "server" or "args" || macro.Length == 1 && macro[0] is >= '1' and <= '9',
                _ => macro is "user" or "server"
            };
            if (!valid) errors.Add($"{path} contains unknown macro '{{{macro}}}'.");
            else if (kind == TemplateKind.Welcome && macro.StartsWith('#') &&
                     (channelMentions is null || !channelMentions.TryGetValue(macro[1..], out var channelId) || channelId == 0))
                errors.Add($"{path} references channel macro '{{{macro}}}' without a valid channelMentions mapping.");
        }
        var stripped = pattern.Replace(template, "");
        if (stripped.Contains('{') || stripped.Contains('}')) errors.Add($"{path} contains malformed template braces.");
    }

    private static bool IsSafeIdentifier(string? value) => !string.IsNullOrEmpty(value) && value.Length <= 32 && SafeIdentifierRegex().IsMatch(value);
    private static bool IsSafeCommandName(string? value) => IsSafeIdentifier(value) && value == value!.Trim();
    private static bool IsSafeFactionName(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 64 && value == value.Trim() && SafeFactionRegex().IsMatch(value);
    private static void RequireId(ulong id, string path, List<string> errors) { if (id == 0) errors.Add($"{path} must be a positive Discord ID."); }
    private static void ValidateUniqueIds(IEnumerable<ulong>? ids, string path, List<string> errors)
    {
        if (ids is null) { errors.Add($"{path} must not be null."); return; }
        var values = ids.ToArray();
        if (values.Any(x => x == 0)) errors.Add($"{path} must contain only positive Discord IDs.");
        if (values.Distinct().Count() != values.Length) errors.Add($"{path} contains duplicate IDs.");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdentifierRegex();

    [GeneratedRegex("^[A-Za-z0-9_-]+(?: [A-Za-z0-9_-]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeFactionRegex();
}
