using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The stances and toggles a planned server restart saves and the next login applies again (master #3249).</summary>
[TestFixture]
public class StatusEffectServiceRestartTests : StatusEffectServiceTestBase
{
    private const uint Online = 500;
    private const uint LoggedOut = 501;
    private const uint Returning = 502;
    private const FeatType ToggleFeat = FeatType.Invalid;

    [SetUp]
    public void Players()
    {
        Bench.Players.Add(Online);
        Bench.Players.Add(LoggedOut);
        Bench.Players.Add(Returning);
        Bench.Engine.SetReturn("GetFirstPC", Online);
        Bench.Engine.SetReturn("GetNextPC", Invalid);
    }

    private static void Stance(ProbeEffect effect) => effect.SourceValue = StatusEffectSourceType.Stance;

    [Test]
    public void SaveTogglesForRestart_SavesAnOnlinePlayersPermanentStance()
    {
        Applied<ProbeEffect>(Online, Online, Stance);
        Bench.Db.With(new Player { Id = "uuid-500" });

        Service.SaveTogglesForRestart();

        Bench.Db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>()
            .Which.TogglesToRestore.Should().Equal(nameof(ProbeEffect));
    }

    [Test]
    public void SaveTogglesForRestart_SkipsEffectsThatAreNotPermanentStances()
    {
        Applied<ProbeEffect>(Online, Online);
        Bench.Db.With(new Player { Id = "uuid-500" });

        Service.SaveTogglesForRestart();

        Bench.Db.Saved.Should().BeEmpty();
    }

    [Test]
    public void SaveTogglesForRestart_SkipsDMsAndPlayersWithNoRecord()
    {
        Applied<ProbeEffect>(Online, Online, Stance);

        Service.SaveTogglesForRestart();
        Bench.Db.Saved.Should().BeEmpty();

        Bench.Db.With(new Player { Id = "uuid-500" });
        Bench.Dms.Add(Online);
        Service.SaveTogglesForRestart();
        Bench.Db.Saved.Should().BeEmpty();
    }

    [Test]
    public void SaveTogglesForRestart_SavesWhatALoggedOutPlayerIsHolding()
    {
        Applied<ProbeEffect>(LoggedOut, LoggedOut, Stance);
        Bench.Engine.SetReturn("GetExitingObject", LoggedOut);
        Service.OnPlayerExit();
        Bench.Engine.SetReturn("GetFirstPC", Invalid);
        Bench.Db.With(new Player { Id = "uuid-501" });

        Service.SaveTogglesForRestart();

        Bench.Db.Saved.Should().ContainSingle().Which.Id.Should().Be("uuid-501");
    }

    private void StartLoginWith(params string[] toggles)
    {
        Bench.Db.With(new Player { Id = "uuid-502", TogglesToRestore = toggles.ToList() });
        Service.DefinitionTypes = new[] { typeof(ProbeEffect) };
        Service.CacheData();
        Bench.Engine.SetReturn("GetEnteringObject", Returning);
        Service.OnPlayerEnter();
        Bench.Engine.SetReturnHandler("GetCurrentHitPoints", _ => 10);
    }

    private void KnowTheToggle(bool known)
    {
        var detail = new AbilityDetail();
        detail.StatusEffectTypesRemovedOnPerkRefund.Add(typeof(ProbeEffect));
        Service.AbilityDetails[ToggleFeat] = detail;
        if (known)
            Bench.KnownFeats.Add((Returning, ToggleFeat));
    }

    [Test]
    public void Login_AppliesAgainASavedToggleThePlayerStillKnows()
    {
        KnowTheToggle(true);
        StartLoginWith(nameof(ProbeEffect));

        Bench.RunDeferred("DelayCommand");

        Service.HasStatusEffect(Returning, typeof(ProbeEffect)).Should().BeTrue();
        Bench.Db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>()
            .Which.TogglesToRestore.Should().BeNull();
    }

    [Test]
    public void Login_DropsASavedToggleWhoseAbilityThePlayerNoLongerKnows()
    {
        KnowTheToggle(false);
        StartLoginWith(nameof(ProbeEffect));

        Bench.RunDeferred("DelayCommand");

        Service.HasStatusEffect(Returning, typeof(ProbeEffect)).Should().BeFalse();
        Bench.Db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>()
            .Which.TogglesToRestore.Should().BeNull();
    }

    [Test]
    public void Login_DoesNotApplyAToggleToADeadPlayer()
    {
        KnowTheToggle(true);
        StartLoginWith(nameof(ProbeEffect));
        Bench.Engine.SetReturnHandler("GetCurrentHitPoints", _ => 0);

        Bench.RunDeferred("DelayCommand");

        Service.HasStatusEffect(Returning, typeof(ProbeEffect)).Should().BeFalse();
    }
}
