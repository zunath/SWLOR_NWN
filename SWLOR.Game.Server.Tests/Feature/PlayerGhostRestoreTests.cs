using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerGhostRestoreTests
{
    [Test]
    public void EveryRevive_RestoresTheLoginGhost()
    {
        var server = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server");
        var revivingFiles = Directory.EnumerateFiles(server, "*.cs", SearchOption.AllDirectories)
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .Where(file => file.Source.Contains("EffectResurrection()"))
            .ToList();

        revivingFiles.Should().NotBeEmpty();
        foreach (var (path, source) in revivingFiles)
        {
            Regex.Matches(source, @"EffectResurrection\(\)[^\r\n]*\r?\n\s*(?:[\w.]+\.)?RestoreGhostAfterRevive\(").Count.Should().Be(
                Regex.Matches(source, @"EffectResurrection\(\)").Count,
                $"death strips the login ghost, so every revive in {Path.GetFileName(path)} must restore it");
        }
    }

    [Test]
    public void LeavingSpaceMode_RestoresTheLoginGhost()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "PlayerTemporaryEffects.cs"));

        source.Should().MatchRegex(@"\[NWNEventHandler\(ScriptName\.OnSpaceExit\)\]\s+public static void RestoreGhostOnSpaceExit\(\)",
            "entering space mode strips every effect, including the login ghost");
    }

    [Test]
    public void A_reload_restores_the_ghost_and_styles_of_a_player_whose_revive_closure_it_dropped()
    {
        const uint revived = 11, diedAgain = 12, notRevived = 13;
        var styles = ScriptedProxy.Create<ICraftStyleEffectsService>(out var styleCalls);
        styleCalls.On("Restore", _ => null);
        using var facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton(styles)));
        using var facadeScope = facades.Install();
        var engine = new FakeNWScript();
        var locals = new LocalVariables(engine);
        engine.SetReturnHandler("GetIsObjectValid", _ => true);
        engine.SetReturnHandler("GetIsPC", _ => true);
        engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == diedAgain);
        using var engineScope = NWScriptRuntime.Override(engine);
        PlayerTemporaryEffects.RestoreGhostAfterRevive(revived);
        PlayerTemporaryEffects.RestoreGhostAfterRevive(diedAgain);
        locals.ClearCalls();

        PlayerTemporaryEffects.RestoreGhostForReload(new[] { revived, diedAgain, notRevived });

        engine.CallsTo("ApplyEffectToObject").Select(call => call.Arguments[2]).Should().Equal(new object[] { revived },
            "a player who died again gets it at the next revive, and one not revived already has it");
        styleCalls.Calls.Where(call => call.Member == "Restore").Select(call => call.Arguments[0]).Should().Equal(new object[] { revived });
        locals.Has(revived, "GHOST_RESTORE_PENDING").Should().BeFalse();
        locals.Has(diedAgain, "GHOST_RESTORE_PENDING").Should().BeFalse();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
