using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The places where humanoid content met an astromech: status effect names, disguises, the shuttle merchant and the
/// working animations.
/// </summary>
public class AstromechContentFitTests
{
    private static readonly string Root = FindRepositoryRoot();

    [Test]
    public void StatusEffectsAppliedByAnAstromech_CarryTheNameOfItsAbility()
    {
        AstromechStatusEffectNames.Resolve(new Shielding2StatusEffect(), appliedByAstromech: true).Should().Be("Deflector Field II");
        AstromechStatusEffectNames.Resolve(new Shielding2StatusEffect(), appliedByAstromech: false).Should().Be("Shielding II");
        AstromechStatusEffectNames.Resolve(new CrusherStanceStatusEffect(), appliedByAstromech: true).Should().Be("Aggression Routine");

        // A status effect that is not named after a humanoid ability keeps its name.
        AstromechStatusEffectNames.Resolve(new HamstringStatusEffect(), appliedByAstromech: true).Should().Be(new HamstringStatusEffect().Name);
    }

    [Test]
    public void EveryAstromechStatusName_IsAnAstromechPerkName_OnAStatusNamedAfterItsSource()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "tools", "AstromechSkillMirrors.json")));
        var pairs = new Dictionary<string, string>();
        foreach (var mirror in config.RootElement.GetProperty("mirrors").EnumerateArray())
        foreach (var line in mirror.GetProperty("lines").EnumerateArray())
        foreach (var perk in line.GetProperty("perks").EnumerateObject())
            pairs[perk.Value.GetProperty("name").GetString()!] = perk.Value.GetProperty("source_name").GetString()!;

        AstromechStatusEffectNames.All.Should().NotBeEmpty();
        foreach (var (type, name) in AstromechStatusEffectNames.All)
        {
            typeof(StatusEffectBase).IsAssignableFrom(type).Should().BeTrue(type.Name);
            var perkName = Regex.Replace(name, " (I|II|III)$", string.Empty);
            pairs.Should().ContainKey(perkName, $"{name} is the name of an astromech perk");

            // The status the astromech name replaces is the one named after the source ability.
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            var sourceName = Regex.Replace(((StatusEffectBase)Activator.CreateInstance(type)!).Name, " (I|II|III)$", string.Empty);
            sourceName.Should().Be(pairs[perkName], $"{type.Name} belongs to {pairs[perkName]}");
        }
    }

    [Test]
    public void EveryStatusAppliedByAGeneratedAbility_ThatIsNamedAfterAHumanoidPerk_HasAnAstromechName()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "tools", "AstromechSkillMirrors.json")));
        var sourceNames = new HashSet<string>();
        foreach (var mirror in config.RootElement.GetProperty("mirrors").EnumerateArray())
        foreach (var line in mirror.GetProperty("lines").EnumerateArray())
        foreach (var perk in line.GetProperty("perks").EnumerateObject())
            sourceNames.Add(perk.Value.GetProperty("source_name").GetString()!);

        var applied = new HashSet<string>();
        foreach (var folder in new[] { "UtilitySystems", "Armaments" })
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "AbilityDefinition", folder), "*.cs"))
        foreach (Match match in Regex.Matches(File.ReadAllText(file), @"\b(\w+StatusEffect)\b"))
            applied.Add(match.Groups[1].Value);

        var named = AstromechStatusEffectNames.All.Keys.Select(type => type.Name).ToHashSet();
        foreach (var className in applied)
        {
            var type = typeof(Shielding1StatusEffect).Assembly.GetType("SWLOR.Game.Server.Feature.StatusEffectDefinition." + className);
            if (type == null || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            var name = ((StatusEffectBase)Activator.CreateInstance(type)!).Name;
            if (sourceNames.Contains(Regex.Replace(name, " (I|II|III)$", string.Empty)))
                named.Should().Contain(className, $"'{name}' is the name of a humanoid perk");
        }
    }

    [Test]
    public void EveryAstromechStatus_HasItsOwnIconRowNameAndArtwork()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "tools", "AstromechSkillMirrors.json")));
        var statuses = config.RootElement.GetProperty("statuses").EnumerateArray().ToList();
        statuses.Should().HaveCount(AstromechStatusEffectNames.All.Count);

        var rows = File.ReadAllLines(Path.Combine(Root, "SWLOR_Haks", "sw_2da", "effecticons.2da"))
            .Select(line => line.Split(' ', '\t').Where(token => token.Length > 0).ToArray())
            .Where(tokens => tokens.Length == 4 && int.TryParse(tokens[0], out _))
            .ToDictionary(tokens => int.Parse(tokens[0]));
        using var tlk = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));
        var text = tlk.RootElement.GetProperty("entries").EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("id").GetInt32(), entry => entry.GetProperty("text").GetString());

        var icons = new HashSet<string>();
        foreach (var status in statuses)
        {
            var label = status.GetProperty("label").GetString()!;
            var name = status.GetProperty("name").GetString()!;
            var icon = status.GetProperty("icon").GetString()!;
            var type = typeof(Shielding1StatusEffect).Assembly.GetType(
                "SWLOR.Game.Server.Feature.StatusEffectDefinition." + status.GetProperty("class").GetString())!;
            var value = Enum.Parse<EffectIconType>(label);
            ((int)value).Should().Be(status.GetProperty("row").GetInt32());

            // The row the client reads: its own label, artwork and name.
            var row = rows[(int)value];
            row[1].Should().Be(label);
            row[2].Should().Be(icon);
            text[int.Parse(row[3]) - 16777216].Should().Be(name);
            icon.Length.Should().BeLessThanOrEqualTo(16);
            icons.Add(icon).Should().BeTrue($"{icon} is used once");
            File.Exists(Path.Combine(Root, "SWLOR_Haks", "sw_ability", icon + ".tga")).Should().BeTrue(icon);

            AstromechStatusEffectNames.All[type].Should().Be(name);
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            var effect = (StatusEffectBase)Activator.CreateInstance(type)!;
            AstromechStatusEffectNames.ResolveIcon(effect, appliedByAstromech: true).Should().Be(value);
            AstromechStatusEffectNames.ResolveIcon(effect, appliedByAstromech: false).Should().Be(effect.Icon);
            AstromechStatusEffectNames.ShowsIcon(effect, value).Should().BeTrue();
            AstromechStatusEffectNames.ShowsIcon(effect, effect.Icon).Should().BeTrue();
        }
    }

    [Test]
    public void EveryAstromechAbility_HasItsOwnIcon()
    {
        // Gameplay icons are unique (Readmes/IconStandards.md): a copied ability never reuses its source's artwork.
        var lines = File.ReadAllLines(Path.Combine(Root, "SWLOR_Haks", "sw_2da", "feat.2da"));
        var columns = lines[2].Split(' ', '\t').Where(token => token.Length > 0).ToList();
        var iconColumn = columns.IndexOf("ICON") + 1;
        var icons = new Dictionary<string, List<int>>();
        foreach (var tokens in lines.Skip(3).Select(line => line.Split(' ', '\t').Where(token => token.Length > 0).ToArray()))
        {
            if (tokens.Length <= iconColumn || !int.TryParse(tokens[0], out var id) || tokens[iconColumn] == "****")
                continue;
            // The quick-cast twin of an ability is the same ability and carries the same icon (QuickCastTests).
            if (tokens[1].StartsWith("AutoCast", StringComparison.Ordinal))
                continue;
            if (!icons.TryGetValue(tokens[iconColumn], out var list))
                icons[tokens[iconColumn]] = list = new List<int>();
            list.Add(id);
        }

        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "tools", "AstromechSkillMirrors.json")));
        var feats = config.RootElement.GetProperty("ledger").EnumerateObject()
            .Where(skill => !skill.Name.StartsWith('#'))
            .SelectMany(skill => skill.Value.EnumerateObject())
            .Where(entry => entry.Value.TryGetProperty("feat", out _))
            .Select(entry => entry.Value.GetProperty("feat").GetInt32())
            .ToHashSet();
        feats.Should().HaveCount(62);

        foreach (var (icon, users) in icons.Where(pair => pair.Value.Any(feats.Contains)))
        {
            icon.Should().StartWith("ife_am_");
            users.Should().HaveCount(1, $"{icon} belongs to one astromech feat");
            File.Exists(Path.Combine(Root, "SWLOR_Haks", "sw_ability", icon + ".tga")).Should().BeTrue(icon);
        }
    }

    [Test]
    public void StatusMessages_UseTheNameOfTheApplyingAbility()
    {
        var source = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        source.Should().Contain("var effectName = AstromechStatusEffectNames.Resolve(statusEffect, source);");
        source.Should().Contain("var effectName = AstromechStatusEffectNames.Resolve(statusEffect, statusEffect.Source);");
        source.Should().NotContain("var effectName = statusEffect.Name;");
        source.Should().Contain("var icon = AstromechStatusEffectNames.ResolveIcon(statusEffect, statusEffect.Source);");
        source.Should().Contain(".Where(effect => AstromechStatusEffectNames.ResolveIcon(effect, effect.Source) == effectIcon)");

        var timer = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "BuffTimer.cs"));
        timer.Should().Contain("StatusEffect.GetStatusEffectName(player, statusType)");
    }

    [Test]
    public void ADisguise_NeverGivesAnAstromechAHumanoidVoice()
    {
        var service = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "Disguise.cs"));
        service.Should().Contain("soundSetId = ResolveSoundSetId(player, soundSetId);");
        service.Should().Contain("var soundSetId = ResolveSoundSetId(player, disguise.SoundSetId);");
        Regex.IsMatch(service, @"return Astromech\.IsAstromech\(player\)\s+\? Astromech\.SoundSetId\s+: ResolveSoundSetId\(soundSetId\);")
            .Should().BeTrue();

        var window = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "DisguiseViewModel.cs"));
        window.Should().Contain("if (isAstromech && soundSetId != Astromech.SoundSetId)");
    }

    [Test]
    public void TheShuttleMerchant_OffersAnAstromechTheChassisWorkshop_NotClothes()
    {
        using var conversation = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Root, "SWLOR.Game.Server", "ConversationData", "starting_items.conversation.json")));
        var root = conversation.RootElement;

        // The astromech greeting is tried first; everyone else falls through to the outfit store.
        var first = root.GetProperty("EntryPoints")[0];
        var condition = first.GetProperty("Conditions")[0];
        condition.GetProperty("Key").GetString().Should().Be("condition-player-is-astromech");
        condition.GetProperty("IsNegated").GetBoolean().Should().BeFalse();
        root.GetProperty("EntryPoints")[1].GetProperty("Conditions").GetArrayLength().Should().Be(0);

        var node = root.GetProperty("Nodes").GetProperty(first.GetProperty("TargetNodeId").GetString()!);
        var actions = node.GetProperty("Choices").EnumerateArray()
            .Select(choice => root.GetProperty("Choices").GetProperty(choice.GetProperty("ChoiceId").GetString()!))
            .SelectMany(choice => choice.GetProperty("Actions").EnumerateArray())
            .Select(action => action.GetProperty("Key").GetString())
            .ToList();
        actions.Should().Contain("action-open-chassis-workshop");
        actions.Should().NotContain("action-open-store", "the store sells clothing an astromech cannot wear");

        var snippets = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "SnippetDefinition", "AstromechSnippetDefinition.cs"));
        snippets.Should().Contain("_builder.Create(\"condition-player-is-astromech\")");
        snippets.Should().Contain("_builder.Create(\"action-open-chassis-workshop\")");
    }

    [Test]
    public void AnAstromech_HasTheWorkingAnimations()
    {
        // Crafting, gathering and item use play these; the chassis has a clip for each (cgetmid, cgetlow and their loops).
        AstromechAnimations.CanPlay(Animation.LoopingGetMid).Should().BeTrue();
        AstromechAnimations.CanPlay(Animation.LoopingGetLow).Should().BeTrue();

        AstromechAnimations.CanPlay(AstromechAnimations.Work).Should().BeTrue();

        // The authored working animations are humanoid clips; an astromech plays its own working motion in their place.
        var named = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "NamedAnimation.cs"));
        named.Should().Contain("AstromechWorkAnimation.Queue(creature, clip, duration);");
        named.Should().Contain("AstromechWorkAnimation.Play(creature, clip, duration);");
        var work = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "AstromechService", "AstromechWorkAnimation.cs"));
        work.Should().Contain("ActionPlayAnimation(AstromechAnimations.Work, 1f, duration);");
        work.Should().Contain("PlayAnimation(AstromechAnimations.Work, 1f, duration);");

        var export = File.ReadAllText(Path.Combine(Root, "tools", "AstromechCreatureExport.py"));
        export.Should().Contain("for name, lean in (('cgetmid', -5), ('cgetlow', -16)):");
    }

    [Test]
    public void AnAstromech_FishesWithItsBuiltInRod()
    {
        // Held items are not drawn on the chassis, so the rod is part of the model and only the fishing clips bring it out.
        var export = File.ReadAllText(Path.Combine(Root, "tools", "AstromechCreatureExport.py"));
        var declared = System.Text.RegularExpressions.Regex.Match(export, @"FISHING_CLIPS = \(([^)]*)\)").Groups[1].Value
            .Split(',').Select(name => name.Trim().Trim('\'')).ToHashSet();
        declared.Should().HaveCount(8);

        var used = new HashSet<string>();
        foreach (var clip in new[]
                 {
                     Fishing.GetFishingAnimation(6), Fishing.GetFishingAnimation(7), Fishing.GetFishingAnimation(8),
                     WorkAnimation.ForFishing(LineActionIds.Reel), WorkAnimation.ForFishing(LineActionIds.GiveLine),
                     WorkAnimation.ForFishing(LineActionIds.Hold), WorkAnimation.FishLanded
                 })
        {
            AstromechAnimations.TryGetRodClips(clip, out var rod).Should().BeTrue(clip.Name);
            used.Add(rod.Entry);
            used.Add(rod.Loop);
        }

        // Every rod clip the exporter writes is played, and every clip played is one it writes.
        used.Should().BeEquivalentTo(declared);
        used.Should().OnlyContain(name => name.Length <= 16, "an object animation replacement name has 16 characters");

        // Crafting and gathering clips keep the tool arm.
        AstromechAnimations.TryGetRodClips(WorkAnimation.ForGathering(GatheringAction.Survey), out _).Should().BeFalse();
        AstromechAnimations.TryGetRodClips(null, out _).Should().BeFalse();

        // The work animation's own clips are the ones swapped (AstromechWorkPlaybackTests covers the swap and its end).
        AstromechWorkPlayback.EntrySource.Should().Be("cgetmid");
        AstromechWorkPlayback.LoopSource.Should().Be("cgetmidlp");
        AstromechAnimations.Work.Should().Be(Animation.LoopingGetMid);

        // The caller can end the motion early (a cancelled cast, a craft that ends): the play hands back a token, and
        // the token's owner ends it, so a native preview on an astromech stops as it does on any creature.
        var named = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "NamedAnimation.cs"));
        named.Should().Contain("return AstromechWorkAnimation.Play(creature, clip, duration);");
        named.Should().Contain(
            "AstromechWorkAnimation.StopIfCurrent(creature, token) || Playback.StopIfCurrent(creature, token);");
        // Each start releases the other owner, so only one token is ever held.
        named.Should().Contain("AstromechWorkAnimation.ReleaseForNativePlayback(creature);");
        var work = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "AstromechService", "AstromechWorkAnimation.cs"));
        work.Should().Contain("NamedAnimation.ReleaseForNativePlayback(creature);");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
