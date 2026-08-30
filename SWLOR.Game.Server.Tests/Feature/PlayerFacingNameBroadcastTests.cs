using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerFacingNameBroadcastTests
{
    [Test]
    public void CommsChannel_UsesOneCommsLabelWithoutExposingPlayerNames()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Communication.cs"));
        var normalizedSource = source.Replace("\r\n", "\n");

        source.Should().Contain("private const int PartyChatChannelNameStrRef = 66755;");
        source.Should().Contain("private const int PartyChatMessagePrefixStrRef = 10303;");
        source.Should().Contain("private const string CommsChannelName = \"Comms\";");
        source.Should().Contain("private const string CommsMessagePrefix = \"[Comms] \";");
        source.Should().Contain("private const string WhisperMessagePrefix = \"[Whisper] \";");
        var moduleEnterHandlerIndex = normalizedSource.IndexOf("[NWNEventHandler(ScriptName.OnModuleEnter)]", StringComparison.Ordinal);
        var applyChannelNameIndex = normalizedSource.IndexOf("public static void ApplyCommsChannelName()", StringComparison.Ordinal);
        var applyChannelNameOverrideIndex = normalizedSource.IndexOf(
            "PlayerPlugin.SetTlkOverride(player, PartyChatChannelNameStrRef, CommsChannelName);",
            StringComparison.Ordinal);
        var applyMessagePrefixOverrideIndex = normalizedSource.IndexOf(
            "PlayerPlugin.SetTlkOverride(player, PartyChatMessagePrefixStrRef, CommsMessagePrefix);",
            StringComparison.Ordinal);
        moduleEnterHandlerIndex.Should().BeGreaterThanOrEqualTo(0);
        applyChannelNameIndex.Should().BeGreaterThan(moduleEnterHandlerIndex);
        applyChannelNameOverrideIndex.Should().BeGreaterThan(applyChannelNameIndex);
        applyMessagePrefixOverrideIndex.Should().BeGreaterThan(applyChannelNameOverrideIndex);
        normalizedSource.Should().Contain("var player = GetEnteringObject();");
        normalizedSource.Should().Contain("if (!GetIsPC(player))");

        source.Should().NotContain("finalMessage.Append(\"[Comms] \");");
        source.Should().Contain("ChatPlugin.SendMessage(channel, message, identitySpeaker, receiver)");
        source.Should().NotContain("ChatChannel.DMTalk");
    }

    [Test]
    public void CombatAndSpaceBroadcasts_DoNotInterpolateRawPlayerNames()
    {
        var root = FindRepositoryRoot();
        var paths = new List<string>
        {
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Space.cs")
        };

        paths.AddRange(Directory.GetFiles(
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "ShipModuleDefinition"),
            "*.cs"));

        foreach (var path in paths)
        {
            var source = File.ReadAllText(path);
            var rawNameBroadcastInvocations = ExtractInvocations(source, "Messaging.SendMessageNearbyToPlayers")
                .Where(invocation => invocation.Text.Contains("GetName("))
                .Select(invocation => $"{Path.GetFileName(path)}:{GetLineNumber(source, invocation.StartIndex)}")
                .ToList();

            rawNameBroadcastInvocations.Should().BeEmpty($"{Path.GetFileName(path)} should render player-facing broadcast names per receiver");
        }
    }

    [Test]
    public void HoloComDirectory_UsesObserverSpecificDisplayNames()
    {
        var root = FindRepositoryRoot();

        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "HoloComViewModel.cs"));

        viewModelSource.Should().NotContain("GetName(");
        viewModelSource.Should().Contain("GetPlainLiveDisplayName(pc)");
        viewModelSource.Should().Contain("GetPlainLiveDisplayName(callSender)");
        viewModelSource.Should().Contain("GetPlainLiveDisplayName(callReceiver)");
        viewModelSource.Should().Contain("UtilPlugin.StripColors(PlayerName.GetDisplayName(Player, target))");
        viewModelSource.Should().Contain("PlayerName.GetPlainDisplayNameByIdentity(");

        var holoComServiceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "HoloCom.cs"));

        holoComServiceSource.Should().NotContain("GetName(");
        holoComServiceSource.Should().Contain("PlayerName.GetDisplayName(sender, receiver)");
    }

    [Test]
    public void PublicPlayerFacingSurfaces_DoNotExposeCanonicalPlayerNames()
    {
        var root = FindRepositoryRoot();

        var holoNetSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "HoloNetViewModel.cs"));
        holoNetSource.Should().Contain("PlayerName.GetChatDisplayName(onlinePlayer, Player)");
        holoNetSource.Should().Contain("\"HoloNet Broadcast\"");
        holoNetSource.Should().NotContain("authorName + \" broadcasts a new HoloNet message");

        var communicationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Communication.cs"));
        var normalizedCommunicationSource = communicationSource.Replace("\r\n", "\n");
        communicationSource.Should().Contain("var speaker = GetEffectiveChatSpeaker(sender);");
        communicationSource.Should().Contain("PlayerName.SendChatMessageWithChatNameOverride(");
        communicationSource.Should().NotContain("finalMessage.Append(PlayerName.GetColoredDisplayName");
        communicationSource.Should().Contain("public const string EventCommsAreaVariable = \"COMMS_EVENT_AREA\";");
        communicationSource.Should().Contain("private const string DisabledChannelMessage = \"This chat channel is disabled.\";");
        communicationSource.Should().Contain("private const string CommsOutOfRangeMessage = \"Your Comms message could not reach one or more out-of-range receivers.\";");
        communicationSource.Should().Contain("var handledChat =");
        communicationSource.Should().NotContain("var inCharacterChat =");
        communicationSource.Should().Contain("if (channel == ChatChannel.PlayerShout && (GetIsDM(sender) || GetIsDMPossessed(sender)))");
        communicationSource.Should().Contain("SendMessageToPC(sender, ColorToken.Red(DisabledChannelMessage));");
        communicationSource.Should().Contain("private static bool IsChatCommandMessage(string message)");
        var chatCommandEarlyOutIndex = communicationSource.IndexOf("if (IsChatCommandMessage(message))", StringComparison.Ordinal);
        var disabledChannelMessageIndex = communicationSource.IndexOf("SendMessageToPC(sender, ColorToken.Red(DisabledChannelMessage));", StringComparison.Ordinal);
        chatCommandEarlyOutIndex.Should().BeGreaterThanOrEqualTo(0);
        disabledChannelMessageIndex.Should().BeGreaterThanOrEqualTo(0);
        chatCommandEarlyOutIndex.Should().BeLessThan(disabledChannelMessageIndex, "slash chat commands must bypass disabled-channel handling");
        communicationSource.Should().Contain("var recipients = new List<uint> { sender };");
        communicationSource.Should().NotContain("recipients.AddRange(allPlayers.Where(player => GetLocalBool(player, \"DISPLAY_HOLONET\")))");
        communicationSource.Should().NotContain("LoadHolonetSetting");
        communicationSource.Should().NotContain("\"DISPLAY_HOLONET\"");
        communicationSource.Should().Contain("recipients.AddRange(allDMs);");
        communicationSource.Should().NotContain("var allPlayers = new List<uint>();");
        communicationSource.Should().NotContain("foreach (var player in allPlayers)");
        communicationSource.Should().NotContain("if (sender != player && IsCommsReceiverInRange(sender, player))");
        communicationSource.Should().NotContain("recipients.Add(player);");
        communicationSource.Should().Contain("for (var member = GetFirstFactionMember(sender); GetIsObjectValid(member); member = GetNextFactionMember(sender))");
        communicationSource.Should().Contain("if (IsCommsReceiverInRange(sender, member))");
        communicationSource.Should().Contain("recipients.Add(member);");
        communicationSource.Should().Contain("else if (GetIsPC(member) &&");
        communicationSource.Should().Contain("outOfRangeCommsPartyMembers++;");
        communicationSource.Should().Contain("Nearby non-party listeners can still overhear it.");
        normalizedCommunicationSource.Should().Contain("recipients.AddRange(allDMs);\n\n                needsAreaCheck = true;\n                distanceCheck = 20.0f;");
        communicationSource.Should().NotContain("AddSameStarshipCommsRecipients");
        communicationSource.Should().Contain("if (dbSender?.Settings?.DisplayCommsOutOfRangeWarnings ?? true)");
        communicationSource.Should().Contain("SendMessageToPC(sender, ColorToken.Red(CommsOutOfRangeMessage));");
        communicationSource.Should().NotContain("SendCommsOutOfRangeMessage(sender);");
        communicationSource.Should().NotContain("ChatPlugin.SendMessage(ChatChannel.ServerMessage, ColorToken.Red(CommsOutOfRangeMessage), sender, sender);");
        communicationSource.Should().Contain("private static bool IsCommsReceiverInRange(uint sender, uint receiver)");
        communicationSource.Should().Contain("private static bool IsSpaceCommsArea(uint area)");
        communicationSource.Should().Contain("private static bool IsEventCommsArea(uint area)");
        communicationSource.Should().Contain("return IsEventCommsArea(receiverArea);");
        communicationSource.Should().Contain("return ResolveCommsPlanet(receiverArea) == senderPlanet;");
        communicationSource.Should().Contain("return senderArea == receiverArea;");
        communicationSource.Should().Contain("Property.GetPropertyId(area)");
        communicationSource.Should().Contain("property.PropertyType == PropertyType.Starship");
        communicationSource.Should().Contain("PropertyLocationType.CurrentPosition");
        communicationSource.Should().Contain("PropertyLocationType.DockPosition");
        communicationSource.Should().Contain("DB.Get<PlayerShip>(dbPlayer.ActiveShipId)");
        communicationSource.Should().Contain("distanceCheck = 20.0f;");
        communicationSource.Should().Contain("var distance = GetDistanceBetween(sender, target);");
        normalizedCommunicationSource.Should().Contain(
            "if (GetArea(target) == GetArea(sender) &&\n" +
            "                        distance <= distanceCheck &&\n" +
            "                        !recipients.Contains(target))");
        communicationSource.Should().NotContain("channel != ChatChannel.PlayerParty || IsCommsReceiverInRange(sender, target)");
        communicationSource.Should().Contain("Comms scope applies only to the party");
        communicationSource.Should().Contain("else if (channel == ChatChannel.PlayerWhisper)");
        communicationSource.Should().NotContain("finalMessage.Append(\"[Whisper] \");");
        communicationSource.Should().NotContain("finalMessage.Append(\"[Holonet] \");");
        communicationSource.Should().Contain("SendProcessedChatMessage(channel, receiver, sender, speaker, finalMessageColored);");
        communicationSource.Should().Contain("private static void SendProcessedChatMessage(");
        communicationSource.Should().NotContain("SendMessageToPC(receiver");
        // NWNX_Rename only patches the per-observer name override around the native Party/Shout/Tell
        // chat functions (see its HOOK_CHAT registrations) - not Talk/Whisper (which instead rely on
        // the speaker's object update already being visible/patched to a nearby observer) and not any
        // DM_* channel. Comms must dispatch on the native PlayerParty channel, not DMTalk, or the
        // override never applies and the speaker's true name leaks once they leave the receiver's area.
        communicationSource.Should().NotContain("ChatChannel.DMTalk");
        communicationSource.Should().Contain("ChatPlugin.SendMessage(channel, message, identitySpeaker, receiver)");
        communicationSource.Should().NotContain("PlayerName.SendWithChatNameOverride");
        communicationSource.Should().NotContain("ChatPlugin.SendMessage(ChatChannel.PlayerDM");
        communicationSource.Should().NotContain("ChatChannel.PlayerDM, finalMessageColored");
        communicationSource.Should().NotContain("var finalChannel = ChatChannel.ServerMessage;");
        communicationSource.Should().NotContain("finalSender = GetModule();");

        var planetSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Planet.cs"));
        planetSource.Should().Contain("private const string PlanetTypeIdVariable = \"PLANET_TYPE_ID\";");
        planetSource.Should().Contain("if (_planets.Count <= 0)");
        planetSource.Should().Contain("CachePlanets();");
        planetSource.Should().Contain("if (_planets.ContainsKey(planetType))");
        planetSource.Should().Contain("areaName.StartsWith(detail.Prefix)");
        planetSource.Should().Contain("GetPlanetTypeByAreaResref(string areaResref)");
        planetSource.Should().Contain("ResolvePlanetTypeByAreaResref(GetResRef(area))");
        planetSource.Should().Contain("if (GetLocalBool(area, \"SPACE\") || areaName.StartsWith(\"Space -\"))");
        planetSource.Should().Contain("private static readonly HashSet<string> _spaceAreaResrefs");
        planetSource.Should().Contain("\"viscaraorbit\"");
        planetSource.Should().Contain("if (_spaceAreaResrefs.Contains(areaResref))");
        planetSource.Should().Contain("[\"veles\"] = PlanetType.Viscara");
        planetSource.Should().Contain("[\"veles_exterior\"] = PlanetType.Viscara");
        planetSource.Should().Contain("[\"viscara\"] = PlanetType.Viscara");
        planetSource.Should().Contain("[\"viscarawildlands\"] = PlanetType.Viscara");
        planetSource.Should().Contain("SetLocalInt(area, PlanetTypeIdVariable, (int)planetType);");
        planetSource.Should().Contain("SetLocalInt(area, PlanetTypeIdVariable, (int)resolvedPlanetType);");

        var planetTypeSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Enumeration",
            "PlanetType.cs"));
        planetTypeSource.Should().Contain("\"Viscara - \"");
        planetTypeSource.Should().Contain("\"CZ-220 - \"");

        var planetTypeIdsByPrefix = new Dictionary<string, int>
        {
            ["Viscara - "] = 1,
            ["Tatooine - "] = 2,
            ["Mon Cala - "] = 4,
            ["Hutlar - "] = 8,
            ["CZ-220 - "] = 16,
            ["Korriban - "] = 32,
            ["Dathomir - "] = 64,
            ["Dantooine - "] = 128
        };
        var planetAreaFiles = Directory.GetFiles(Path.Combine(root.FullName, "Module", "are"), "*.are.json")
            .Select(path => new
            {
                Path = path,
                Resref = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)),
                AreaName = GetAreaDisplayName(path)
            })
            .Where(area => planetTypeIdsByPrefix.Keys.Any(prefix => area.AreaName.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        planetAreaFiles.Should().Contain(area => area.Resref == "viscarawildlands");
        planetAreaFiles.Should().Contain(area => area.Resref == "veles_exterior");

        foreach (var planetArea in planetAreaFiles)
        {
            var planet = planetTypeIdsByPrefix.First(entry =>
                planetArea.AreaName.StartsWith(entry.Key, StringComparison.Ordinal));
            var gitPath = Path.Combine(root.FullName, "Module", "git", planetArea.Resref + ".git.json");

            File.Exists(gitPath).Should().BeTrue($"{planetArea.Resref} should have a matching GIT area instance file");
            GitLocalInt(gitPath, "PLANET_TYPE_ID").Should().Be(
                planet.Value,
                $"{planetArea.Resref} should have an explicit planet id for Comms planet range checks");
        }

        var chatCommandSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "ChatCommand.cs"));
        chatCommandSource.Should().NotContain("ChatPlugin.GetChannel() == ChatChannel.PlayerShout");

        var eventAreaDirectory = Path.Combine(root.FullName, "Module", "are");
        var eventAreaFiles = Directory.GetFiles(eventAreaDirectory, "*.are.json")
            .Where(path =>
            {
                var areaResref = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
                var hasEventResref = areaResref.StartsWith("vrotr") ||
                       areaResref.StartsWith("ka_") ||
                       areaResref.StartsWith("na_ka_") ||
                       areaResref == "republicshipevnt";
                // The DM Comms event areas are all "[Prefab] ..." prefab areas. Require the prefab
                // name too, so non-prefab areas that merely share a resref prefix (e.g. the capstone
                // boss arena ka_ar_czweaparen) are not held to the COMMS_EVENT_AREA invariant.
                return hasEventResref && GetAreaDisplayName(path).StartsWith("[Prefab]", StringComparison.Ordinal);
            })
            .ToList();

        eventAreaFiles.Should().NotBeEmpty();
        foreach (var eventAreaFile in eventAreaFiles)
        {
            var areaResref = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(eventAreaFile));
            var eventGitFile = Path.Combine(root.FullName, "Module", "git", areaResref + ".git.json");

            File.Exists(eventGitFile).Should().BeTrue($"{areaResref} should have a matching GIT area instance file");
            File.ReadAllText(eventAreaFile).Should().NotContain("\"COMMS_EVENT_AREA\"");
            File.ReadAllText(eventGitFile).Should().Contain("\"COMMS_EVENT_AREA\"");
        }

        var tlkOverrideSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "TlkOverrides.cs"));
        tlkOverrideSource.Should().Contain("SetTlkOverride(10303, \"[Comms] \");");
        tlkOverrideSource.Should().Contain("SetTlkOverride(66751, \"Disabled\");");
        tlkOverrideSource.Should().Contain("SetTlkOverride(66755, \"Comms\");");

        var settingsDefinitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "SettingsDefinition.cs"));
        settingsDefinitionSource.Should().NotContain("Show Holonet");
        settingsDefinitionSource.Should().NotContain("Holonet (aka Shout)");
        settingsDefinitionSource.Should().NotContain("DisplayHolonetChannel");

        var settingsViewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "SettingsViewModel.cs"));
        settingsViewModelSource.Should().NotContain("DisplayHolonetChannel");
        settingsViewModelSource.Should().NotContain("UpdateHolonetSetting");
        settingsViewModelSource.Should().NotContain("\"DISPLAY_HOLONET\"");

        var playerEntitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Entity",
            "Player.cs"));
        playerEntitySource.Should().NotContain("IsHolonetEnabled");

        var playerOverviewSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Admin",
            "Shared",
            "Components",
            "PlayerOverview.razor"));
        playerOverviewSource.Should().NotContain("Holonet Enabled");
        playerOverviewSource.Should().NotContain("IsHolonetEnabled");

        var playerGuideSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PlayerGuideViewModel.cs"));
        playerGuideSource.Should().Contain("\"Disabled Shout Channel\"");
        playerGuideSource.Should().Contain("The Shout chat channel is disabled for players.");
        playerGuideSource.Should().Contain("\"HoloNet Broadcast Window\"");
        playerGuideSource.Should().NotContain("\"HoloNet Chat\"");
        playerGuideSource.Should().NotContain("Shout sends an in-character HoloNet message");
        playerGuideSource.Should().NotContain("HoloNet channel display");
        playerGuideSource.Should().NotContain("HoloNet display");

        var roleplayXpSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "RoleplayXP.cs"));
        roleplayXpSource.Should().Contain("channel == ChatChannel.PlayerParty;");
        roleplayXpSource.Should().NotContain("channel == ChatChannel.PlayerShout");

        var statusEffectSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs"));
        statusEffectSource.Should().Contain("PlayerName.GetDisplayName(receiver, creature)");
        statusEffectSource.Should().Contain("PlayerName.GetDisplayName(receiver, source)");
        statusEffectSource.Should().NotContain("var name = GetName(creature);");

        var propertyPermissionsSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PropertyPermissionsViewModel.cs"));
        propertyPermissionsSource.Should().Contain("PlayerNameService.SearchKnownPlayerIdsByName(Player, SearchText, int.MaxValue)");
        propertyPermissionsSource.Should().Contain("PlayerNameService.GetKnownNameOrFallbackByPlayerId");
        propertyPermissionsSource.Should().Contain("AddFieldSearch(nameof(Entity.Player.Name), sanitizedSearch, true)");
        propertyPermissionsSource.Should().NotContain("PlayerNameService.GetDisplayNameByPlayerId");

        var agentsSource = File.ReadAllText(Path.Combine(root.FullName, "AGENTS.md"));
        agentsSource.Should().Contain("Property and ship permission management is a narrow exception");

        var electionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ElectionViewModel.cs"));
        electionSource.Should().Contain("PlayerName.GetPlainDisplayNameByPlayerId(Player, candidate.Id, candidate.Name)");
        electionSource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, selectedCandidateId, dbCandidate.Name)");
        electionSource.Should().NotContain("Your vote for {dbCandidate.Name} has been cast.");

        var citySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageCityViewModel.cs"));
        citySource.Should().Contain("PlayerName.GetPlainDisplayNameByPlayerId(Player, citizen.Id, citizen.Name)");

        var citizenshipSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageCitizenshipViewModel.cs"));
        citizenshipSource.Should().Contain("PlayerName.GetPlainDisplayNameByPlayerId(Player, dbCity.OwnerPlayerId, dbMayorPlayer.Name)");
        citizenshipSource.Should().NotContain("Mayor: {dbMayorPlayer.Name}");

        var propertySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs"));
        propertySource.Should().NotContain("$\"{GetName(player)}'s Apartment\"");
        propertySource.Should().NotContain("$\"{GetName(player)}'s Starship\"");
        propertySource.Should().NotContain("$\"{GetName(player)}'s City\"");
        propertySource.Should().NotContain("GetPlayerName(");
        propertySource.Should().NotContain("**Mayor**:");
        propertySource.Should().NotContain("**New Mayor**:");
        propertySource.Should().NotContain("**Founding Mayor**:");
    }

    [Test]
    public void HoloComDialogue_UsesTheAreaLocalHologramAsItsTransportSpeaker()
    {
        var root = FindRepositoryRoot();
        var communicationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Communication.cs"));
        var normalizedCommunicationSource = communicationSource.Replace("\r\n", "\n");

        communicationSource.Should().Contain("var speaker = GetEffectiveChatSpeaker(sender);");
        communicationSource.Should().Contain("var isHoloComRelay = sender != speaker;");
        communicationSource.Should().Contain(
            "SendProcessedChatMessage(channel, receiver, sender, speaker, finalMessageColored);");
        communicationSource.Should().Contain("uint transportSpeaker,");
        communicationSource.Should().Contain("uint identitySpeaker,");
        communicationSource.Should().Contain("if (isHoloComRelay)");
        communicationSource.Should().Contain(
            "finalMessage.Append(GetHoloComRelayChannelPrefix(channel));");
        communicationSource.Should().Contain(
            "finalMessage.Append(PlayerName.GetColoredChatDisplayName(receiver, speaker));");
        communicationSource.Should().Contain(
            "private static string GetHoloComRelayChannelPrefix(ChatChannel channel)");
        communicationSource.Should().Contain("if (channel == ChatChannel.PlayerWhisper)");
        communicationSource.Should().Contain("return WhisperMessagePrefix;");
        communicationSource.Should().Contain("if (channel == ChatChannel.PlayerParty)");
        communicationSource.Should().Contain("return CommsMessagePrefix;");
        communicationSource.Should().Contain("return string.Empty;");
        communicationSource.Should().Contain("if (transportSpeaker != identitySpeaker)");
        communicationSource.Should().Contain(
            "ChatPlugin.SendMessage(ChatChannel.ServerMessage, message, transportSpeaker, receiver);");
        normalizedCommunicationSource.Should().Contain(
            "PlayerName.SendChatMessageWithChatNameOverride(\n" +
            "                receiver,\n" +
            "                identitySpeaker,");
        communicationSource.Should().Contain(
            "ChatPlugin.SendMessage(channel, message, identitySpeaker, receiver)");
        communicationSource.Should().NotContain(
            "ChatPlugin.SendMessage(channel, message, transportSpeaker, receiver)");
    }

    [Test]
    public void CopiedPlayerObjects_UseGenericDisplaySafeNames()
    {
        var root = FindRepositoryRoot();

        var holoComSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "HoloCom.cs"));
        holoComSource.Should().Contain("var holoSender = CopyObject(sender");
        holoComSource.Should().Contain("var holoReceiver = CopyObject(receiver");
        holoComSource.Should().Contain("ConfigureHologram(holoSender);");
        holoComSource.Should().Contain("ConfigureHologram(holoReceiver);");
        holoComSource.Should().Contain("SetName(hologram, \"Hologram\");");

        var holoComMessagingSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "HoloComMessaging.cs"));
        holoComMessagingSource.Should().Contain("HoloCom.ConfigureHologram(hologram);");
        holoComMessagingSource.Should().NotContain("SetName(hologram,");

        var spaceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Space.cs"));
        spaceSource.Should().Contain("var copy = CopyObject(player");
        spaceSource.Should().Contain("SetName(copy, \"Pilot\");");
    }

    private static List<(int StartIndex, string Text)> ExtractInvocations(string source, string methodName)
    {
        var invocations = new List<(int StartIndex, string Text)>();
        var searchIndex = 0;

        while (searchIndex < source.Length)
        {
            var methodIndex = source.IndexOf(methodName, searchIndex, StringComparison.Ordinal);
            if (methodIndex < 0)
                break;

            var openParenIndex = source.IndexOf('(', methodIndex + methodName.Length);
            if (openParenIndex < 0)
                break;

            var closeParenIndex = FindClosingParenthesis(source, openParenIndex);
            closeParenIndex.Should().BeGreaterThan(openParenIndex, $"the {methodName} invocation should be parseable");

            invocations.Add((methodIndex, source.Substring(methodIndex, closeParenIndex - methodIndex + 1)));
            searchIndex = closeParenIndex + 1;
        }

        return invocations;
    }

    private static int FindClosingParenthesis(string source, int openParenIndex)
    {
        // This lightweight parser intentionally does not support C# raw string literals.
        // If SendMessageNearbyToPlayers invocations start using """ raw strings, parsing may be incorrect.
        var depth = 0;
        var inString = false;
        var inVerbatimString = false;
        var inChar = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var index = openParenIndex; index < source.Length; index++)
        {
            var character = source[index];
            var nextCharacter = index + 1 < source.Length ? source[index + 1] : '\0';

            if (inLineComment)
            {
                if (character == '\n')
                    inLineComment = false;

                continue;
            }

            if (inBlockComment)
            {
                if (character == '*' && nextCharacter == '/')
                {
                    inBlockComment = false;
                    index++;
                }

                continue;
            }

            if (inString)
            {
                if (inVerbatimString)
                {
                    if (character == '"' && nextCharacter == '"')
                    {
                        index++;
                        continue;
                    }

                    if (character == '"')
                        inString = false;

                    continue;
                }

                if (character == '\\')
                {
                    index++;
                    continue;
                }

                if (character == '"')
                    inString = false;

                continue;
            }

            if (inChar)
            {
                if (character == '\\')
                {
                    index++;
                    continue;
                }

                if (character == '\'')
                    inChar = false;

                continue;
            }

            if (character == '/' && nextCharacter == '/')
            {
                inLineComment = true;
                index++;
                continue;
            }

            if (character == '/' && nextCharacter == '*')
            {
                inBlockComment = true;
                index++;
                continue;
            }

            if (character == '"')
            {
                var previousCharacter = index > 0 ? source[index - 1] : '\0';
                var twoCharactersBack = index > 1 ? source[index - 2] : '\0';

                inString = true;
                inVerbatimString =
                    previousCharacter == '@' ||
                    previousCharacter == '$' && twoCharactersBack == '@';
                continue;
            }

            if (character == '\'')
            {
                inChar = true;
                continue;
            }

            if (character == '(')
            {
                depth++;
                continue;
            }

            if (character != ')')
                continue;

            depth--;
            if (depth == 0)
                return index;
        }

        return -1;
    }

    private static int GetLineNumber(string source, int index)
    {
        return source.Take(index).Count(character => character == '\n') + 1;
    }

    private static string GetAreaDisplayName(string areaPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(areaPath));
        return document.RootElement
            .GetProperty("Name")
            .GetProperty("value")
            .GetProperty("0")
            .GetString() ?? string.Empty;
    }

    private static int? GitLocalInt(string gitPath, string variableName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(gitPath));
        var root = document.RootElement;

        // The runtime reads this with GetLocalInt(area, ...), i.e. the area instance's top-level
        // VarTable, which is also where the toolset writes area locals. Older hand-authored areas
        // keep it under AreaProperties, so accept either location.
        if (TryReadIntLocal(root, variableName, out var topLevel))
        {
            return topLevel;
        }

        if (root.TryGetProperty("AreaProperties", out var areaProperties) &&
            areaProperties.TryGetProperty("value", out var areaPropertiesValue) &&
            TryReadIntLocal(areaPropertiesValue, variableName, out var nested))
        {
            return nested;
        }

        return null;
    }

    private static bool TryReadIntLocal(JsonElement container, string variableName, out int result)
    {
        result = 0;
        if (!container.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (!variable.TryGetProperty("Name", out var name) ||
                name.GetProperty("value").GetString() != variableName)
            {
                continue;
            }

            if (!variable.TryGetProperty("Type", out var type) ||
                type.GetProperty("value").GetInt32() != 1 ||
                !variable.TryGetProperty("Value", out var value) ||
                value.GetProperty("type").GetString() != "int")
            {
                return false;
            }

            result = value.GetProperty("value").GetInt32();
            return true;
        }

        return false;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
