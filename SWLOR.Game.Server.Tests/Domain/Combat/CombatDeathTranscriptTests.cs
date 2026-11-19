using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ordered call-and-draw transcripts of <c>DeathService</c>'s handlers and of <c>CombatService</c>'s handlers, taken
/// before either is split. The death handlers run for a player killed by a creature, by a player, and by a player in subdual mode,
/// with and without a recording; the combat handlers run the defeated-enemy effects, the exit cleanup, the tracker refresh with
/// every tracker configured and with none, and the six-second facing cleanup. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c>
/// only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatDeathTranscriptTests
{
    private static readonly Type[] DeathLeaves = DefaultLeaves
        .Concat(new[]
        {
            typeof(INamedAnimationService), typeof(IPlayerNameService), typeof(IMessagingService), typeof(ICityUpgradeLookup),
            typeof(IAreaLookup), typeof(ICombatDamageTakenService)
        })
        .ToArray();

    [Test]
    public void TheDeathServiceHandlersProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench(DeathLeaves);
        var death = bench.Get<IDeathService>();
        // The player being killed holds some skill points so the penalty has a size; the killer is a player in one scenario.
        var defender = bench.Db.Get<Player>("uuid-Defender");
        defender.TotalSPAcquired = 120;
        defender.XPDebt = 10;
        bench.Db.Set(defender);
        bench.Engine.SetReturn("GetLastPlayerDying", Defender);
        bench.Engine.SetReturn("GetLastPlayerDied", Defender);
        bench.Engine.SetReturn("GetLastRespawnButtonPresser", Defender);
        bench.Engine.SetReturn("GetEnteringObject", Defender);
        bench.Engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is Attacker or Defender);

        var prevent = false;
        bench.Leaf<ICombatDamageTakenService>().Script(nameof(ICombatDamageTakenService.TryPreventFatalDamageAndGrantTemporaryHP), _ => prevent);

        bench.Step("dying: the fatal damage is prevented", () =>
        {
            prevent = true;
            death.OnPlayerDying();
        });
        bench.Step("dying: nothing prevents it", () =>
        {
            prevent = false;
            death.OnPlayerDying();
        });
        bench.Step("death: killed by a creature", () =>
        {
            bench.Engine.SetReturn("GetLastHostileActor", Other);
            death.OnPlayerDeath();
        });
        bench.Step("death: killed by a player", () =>
        {
            bench.Engine.SetReturn("GetLastHostileActor", Attacker);
            death.OnPlayerDeath();
        });
        bench.Step("death: killed by a player in subdual mode, not recording", () =>
        {
            var attacker = bench.Db.Get<Player>("uuid-Attacker");
            attacker.Settings.IsSubdualModeEnabled = true;
            bench.Db.Set(attacker);
            death.OnPlayerDeath();
        });
        bench.Step("death: killed by a player in subdual mode, recording", () =>
        {
            bench.Leaf<ICombatRecorderService>().Script("get_IsRecording", _ => true);
            death.OnPlayerDeath();
        });
        bench.Step("respawn: recording", () => death.OnPlayerRespawn());
        bench.Step("respawn: not recording", () =>
        {
            bench.Leaf<ICombatRecorderService>().Script("get_IsRecording", _ => false);
            death.OnPlayerRespawn();
        });
        bench.Step("respawn: the debt is waived", () =>
        {
            bench.Engine.SetReturnHandler("GetLocalInt", args => (string)args[1]! == "DEATH_DEBT_WAIVED" ? 1 : 0);
            death.OnPlayerRespawn();
            bench.Engine.SetReturnHandler("GetLocalInt", args => 0);
        });
        bench.Step("login: a player with no respawn point gets the default", () => death.InitializeRespawnPoint());
        bench.Step("login: a player with a respawn point keeps it", () => death.InitializeRespawnPoint());
        bench.Step("login: a DM is skipped", () =>
        {
            bench.Engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == Defender);
            death.InitializeRespawnPoint();
            bench.Engine.SetReturnHandler("GetIsDM", args => false);
        });
        bench.Step("respawn location and area of a player with and without a point", () =>
        {
            var with = new Player("respawn-with") { RespawnAreaResref = "area_a", RespawnLocationX = 1f, RespawnLocationY = 2f, RespawnLocationZ = 0.5f };
            death.HasRespawnArea(with);
            death.GetRespawnLocation(with);
            death.HasRespawnArea(new Player("respawn-without"));
            death.GetRespawnLocation(new Player("respawn-without"));
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/Death.txt", bench.Text);
    }

    [Test]
    public void TheCombatServiceHandlersProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench();
        var combat = bench.Get<ICombatService>();

        foreach (var stat in new[] { 0, 5 })
        {
            bench.DefaultStat = stat;
            var tag = " (stats " + stat + ")";
            bench.Step("death handler with a valid killer" + tag, () =>
            {
                bench.Engine.SetReturn("OBJECT_SELF", Defender);
                bench.Engine.SetReturn("GetLastKiller", Attacker);
                ((CombatService)combat).ApplyDefeatedEnemyEffects();
            });
            bench.Step("death handler with no killer" + tag, () =>
            {
                bench.Engine.SetReturn("GetLastKiller", Gone);
                ((CombatService)combat).ApplyDefeatedEnemyEffects();
            });
            bench.Step("exit handler" + tag, () =>
            {
                bench.Engine.SetReturn("GetExitingObject", Attacker);
                ((CombatService)combat).ClearStatTriggerState();
            });
            bench.Step("exit handler for an invalid object" + tag, () =>
            {
                bench.Engine.SetReturn("GetExitingObject", Gone);
                ((CombatService)combat).ClearStatTriggerState();
            });
            bench.Step("refresh the stat-driven trackers" + tag, () => combat.RefreshStatDrivenTrackerEffects(Attacker));
            bench.Step("refresh the trackers of an invalid creature" + tag, () => combat.RefreshStatDrivenTrackerEffects(Gone));
            bench.Step("six-second cleanup out of combat" + tag, () =>
            {
                bench.Engine.SetReturn("OBJECT_SELF", Attacker);
                bench.Engine.SetReturn("GetIsInCombat", false);
                ((CombatService)combat).ClearCombatState();
            });
            bench.Step("six-second cleanup in combat" + tag, () =>
            {
                bench.Engine.SetReturn("GetIsInCombat", true);
                ((CombatService)combat).ClearCombatState();
            });
            bench.Step("damage roll" + tag, () => bench.Get<ICombatDamageRollService>().CalculateDamage(100, 45, 40, 100, 10, 120, 5));
        }

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/CombatService.txt", bench.Text);
    }
}
