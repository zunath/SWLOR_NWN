using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The enmity transcript, taken before <c>EnmityService</c> is split into the table, change and attack parts: a scripted
/// sequence of damage, attack, healing (with a nested heal), taunt, reduce, proximity, removal, limbo, death, destroy, player death
/// and exit events, then the attack paths (the highest target, the attack command, the leash retarget). After every step the
/// transcript lists the three carried tables, the creature-to-enemies list, the attack commands and the healing observations, and
/// every call into a leaf and every engine write the step made, in order. A last step writes the carried keys to the reload carrier
/// and reads them into a new owner, which must hold the same tables. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a
/// deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class EnmityTranscriptTests
{
    private static readonly string[] Tables =
    {
        "_enemyEnmityTables", "_creatureToEnemies", "_proximityEnmityAmounts", "_attackCommandTimes", "_attackCommandTargets", "_healingHitPoints"
    };

    private static readonly Type[] Leaves = DefaultLeaves
        .Where(type => type != typeof(IEnmityService))
        .Concat(new[]
        {
            typeof(IAIService), typeof(IActivityService), typeof(IHoloComService), typeof(ICompanionLookup), typeof(IStealthService),
            typeof(ICompanionTargetAuthorityService), typeof(ICombatQueriesService)
        })
        .ToArray();

    [Test]
    public void AScriptedSequenceOfEnmityEventsProducesTheCapturedTables()
    {
        using var bench = new CombatTranscriptBench(Leaves);
        var enmity = bench.Get<IEnmityService>();
        var services = new object[] { enmity };
        var currentHitPoints = 100;
        var eventData = new Dictionary<string, string>();
        var self = Defender;
        var damager = Attacker;
        var attacker = Other;
        var died = Attacker;
        var exiting = Attacker;
        var damage = 0;
        bench.Engine.SetReturnHandler("OBJECT_SELF", _ => self);
        bench.Engine.SetReturnHandler("GetLastDamager", _ => damager);
        bench.Engine.SetReturnHandler("GetLastAttacker", _ => attacker);
        bench.Engine.SetReturnHandler("GetLastPlayerDied", _ => died);
        bench.Engine.SetReturnHandler("GetExitingObject", _ => exiting);
        bench.Engine.SetReturnHandler("GetTotalDamageDealt", _ => damage);
        bench.Engine.SetReturnHandler("GetCurrentHitPoints", _ => currentHitPoints);
        bench.Engine.SetReturnHandler("GetIsDead", _ => false);
        // The attacker is the one player; the enemies are creatures. Everything is in the one area and in line of sight.
        bench.Engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Attacker);
        bench.Engine.SetReturnHandler("LineOfSightObject", _ => true);
        // The creature AI's aggro rules read the engine: nobody is inside an aggro aura unless a scene says so (the stale proximity check
        // is the only caller of the aggro range), and a creature is leash-evading while the scene sets the flag.
        var leashing = false;
        bench.Engine.SetReturnHandler("GetLocalBool", _ => leashing);
        bench.Engine.SetReturnHandler("GetDistanceBetween",
            _ => new System.Diagnostics.StackTrace().GetFrames().Any(frame => frame.GetMethod()?.Name == "ShouldRemoveStaleProximityTarget") ? 20f : 3.0f);
        bench.Engine.SetReturnHandler("StringToObject", args => uint.TryParse((string)args[0]!, out var id) ? id : 0x7F000000u);
        bench.Events.SetReturnHandler("GetEventData", args => eventData.GetValueOrDefault((string)args[0]!, string.Empty));
        bench.Leaf<ICompanionTargetAuthorityService>().Script("CanIssueAttackCommand", _ => true);
        bench.SetStat(Attacker, StatType.DamageEnmityPercentAdjustment, 25);
        bench.SetStat(Other, StatType.DamageEnmityPercentAdjustment, -30);

        void Scene(string name, Action action) => bench.Step(name, () =>
        {
            bench.Now = bench.Now.AddSeconds(5);
            action();
            bench.Lines.Add("-- tables");
            foreach (var line in CombatTableState.DumpFields(services, Tables, bench.Format))
                bench.Lines.Add(line);
        });

        Scene("an enemy is damaged by a creature", () =>
        {
            self = Defender;
            damager = Attacker;
            damage = 40;
            enmity.CreatureDamaged();
        });
        Scene("an enemy is damaged by a creature with a reduced enmity stat", () =>
        {
            damager = Other;
            damage = 40;
            enmity.CreatureDamaged();
        });
        Scene("an enemy is damaged for nothing", () =>
        {
            damage = 0;
            enmity.CreatureDamaged();
        });
        Scene("an enemy is damaged by something that is gone", () =>
        {
            damager = Gone;
            damage = 10;
            enmity.CreatureDamaged();
        });
        Scene("a creature attacks an enemy", () =>
        {
            self = Defender;
            attacker = Ally;
            enmity.CreatureAttacked();
        });
        Scene("modify enmity directly", () =>
        {
            enmity.ModifyEnmity(Master, Defender, 100);
            enmity.ModifyEnmity(Master, Other, 5);
            enmity.ModifyEnmity(Master, Defender, -500);
        });
        Scene("modify enmity on all", () => enmity.ModifyEnmityOnAll(Attacker, 30));
        Scene("a heal starts and completes", () =>
        {
            self = Ally;
            eventData["TARGET_OBJECT_ID"] = Attacker.ToString();
            eventData["HEAL_AMOUNT"] = "60";
            currentHitPoints = 50;
            enmity.HealingStarted();
            currentHitPoints = 110;
            enmity.HealingCompleted();
        });
        Scene("a heal with a nested heal", () =>
        {
            self = Ally;
            currentHitPoints = 50;
            enmity.HealingStarted();
            self = Master;
            currentHitPoints = 70;
            enmity.HealingStarted();
            currentHitPoints = 90;
            enmity.HealingCompleted();
            self = Ally;
            currentHitPoints = 120;
            enmity.HealingCompleted();
        });
        Scene("a heal that reaches nobody", () =>
        {
            eventData["TARGET_OBJECT_ID"] = Gone.ToString();
            enmity.HealingStarted();
            enmity.HealingCompleted();
            eventData["TARGET_OBJECT_ID"] = Attacker.ToString();
        });
        Scene("a taunt", () =>
        {
            enmity.TryTaunt(Other, Defender, 50);
            enmity.TryTaunt(Master, Defender, 20);
            enmity.IsThreatSecured(Defender, Master);
        });
        Scene("reduce enmity", () =>
        {
            enmity.ReduceEnmity(Attacker, Defender, 50);
            enmity.ReduceEnmityOnAll(Master, 25);
        });
        Scene("proximity enmity", () =>
        {
            enmity.ModifyProximityEnmity(Attacker, Ally, 20);
            enmity.ModifyProximityEnmity(Other, Ally, 5);
            bench.Lines.Add("HasProximityEnmity " + enmity.HasProximityEnmity(Attacker, Ally));
            bench.Lines.Add("HasEnmity " + enmity.HasEnmity(Attacker));
            bench.Lines.Add("HasNonProximityEnmity " + enmity.HasNonProximityEnmity(Defender));
            bench.Lines.Add("RemoveProximityEnmity " + enmity.RemoveProximityEnmity(Other, Ally));
        });
        Scene("the highest target and the threat reads", () =>
        {
            bench.Lines.Add("highest " + bench.Name(enmity.GetHighestEnmityTarget(Defender)));
            bench.Lines.Add("highest attack target " + bench.Name(enmity.GetHighestEnmityAttackTarget(Defender)));
            bench.Lines.Add("table size " + enmity.GetEnmityTable(Defender).Count);
            bench.Lines.Add("towards all enemies " + enmity.GetEnmityTowardsAllEnemies(Attacker).Count);
        });
        Scene("the enemy attacks its highest target", () =>
        {
            enmity.AttackHighestEnmityTarget(Defender);
            enmity.AttackHighestEnmityTarget(Defender);
        });
        Scene("an attack command is issued and the actions are cleared", () =>
        {
            enmity.IssueAttackCommand(Defender, Attacker);
            enmity.IssueAttackCommand(Defender, Attacker, false);
            enmity.ResumeAttackAfterActionsCleared(Defender);
        });
        Scene("a creature is hidden and its enemies reevaluate", () => enmity.ReevaluateEnemyAttackTargets(Attacker));
        Scene("remove one creature from one enemy", () => enmity.RemoveEnmity(Attacker, Defender));
        Scene("remove a creature from every table", () => enmity.RemoveCreatureEnmity(Master));
        Scene("clear one enemy's table", () => enmity.ClearEnmityTable(Defender));
        Scene("enmity is built again for the next events", () =>
        {
            enmity.ModifyEnmity(Attacker, Defender, 70);
            enmity.ModifyEnmity(Other, Defender, 30);
            enmity.ModifyEnmity(Attacker, Ally, 20);
            enmity.ModifyProximityEnmity(Attacker, Master, 8);
        });
        Scene("a player dies", () =>
        {
            died = Attacker;
            enmity.PlayerDeath();
        });
        Scene("a player exits", () =>
        {
            enmity.ModifyEnmity(Attacker, Defender, 70);
            exiting = Attacker;
            enmity.PlayerExit();
        });
        Scene("an enemy dies", () =>
        {
            enmity.ModifyEnmity(Attacker, Defender, 70);
            enmity.ModifyEnmity(Defender, Ally, 12);
            self = Defender;
            enmity.CreatureDeath();
        });
        Scene("an enemy is destroyed", () =>
        {
            enmity.ModifyEnmity(Attacker, Defender, 70);
            self = Defender;
            enmity.CreatureDestroyed();
        });
        Scene("a DM limbos two creatures", () =>
        {
            enmity.ModifyEnmity(Attacker, Defender, 70);
            enmity.ModifyEnmity(Attacker, Other, 70);
            eventData["NUM_TARGETS"] = "2";
            eventData["TARGET_1"] = Defender.ToString();
            eventData["TARGET_2"] = Other.ToString();
            enmity.CreatureLimbo();
        });

        var aiLeaf = bench.Leaf<IAIService>();
        var stealthLeaf = bench.Leaf<IStealthService>();
        Scene("a proximity-only target out of aggro range is dropped", () =>
        {
            enmity.ClearEnmityTable(Defender);
            enmity.ModifyProximityEnmity(Ally, Defender, 15);
            enmity.AttackHighestEnmityTarget(Defender);
        });
        Scene("every target is hidden, so the enemy stops attacking", () =>
        {
            enmity.ClearEnmityTable(Defender);
            enmity.ModifyEnmity(Attacker, Defender, 70);
            stealthLeaf.Script("IsHiddenFrom", _ => true);
            bench.Engine.SetReturn("GetCurrentAction", ActionType.AttackObject);
            enmity.AttackHighestEnmityTarget(Defender);
            enmity.ReevaluateEnemyAttackTargets(Attacker);
            stealthLeaf.Script("IsHiddenFrom", _ => false);
            bench.Engine.SetReturn("GetCurrentAction", 0);
        });
        Scene("the leash takes the enemy while enmity changes and it is told to attack", () =>
        {
            enmity.ClearEnmityTable(Defender);
            aiLeaf.Script("TryStartCombatLeashEvade", _ => true);
            enmity.ModifyEnmity(Attacker, Defender, 70);
            enmity.TryTaunt(Attacker, Defender, 50);
            enmity.IssueAttackCommand(Defender, Attacker);
            aiLeaf.Script("TryStartCombatLeashEvade", _ => false);
            enmity.ModifyEnmity(Attacker, Defender, 70);
            aiLeaf.Script("TryStartCombatLeashEvade", _ => true);
            enmity.AttackHighestEnmityTarget(Defender);
            aiLeaf.Script("TryStartCombatLeashEvade", _ => false);
        });
        Scene("an enemy that is already leashing gains no enmity", () =>
        {
            leashing = true;
            enmity.ModifyEnmity(Other, Defender, 70);
            enmity.TryTaunt(Other, Defender, 50);
            leashing = false;
        });

        // A reload carries the three tables through the reload carrier under their field keys and reads them into a new build's instance.
        bench.Step("a reload round trip through the carried keys", () =>
        {
            enmity.ModifyEnmity(Attacker, Defender, 70);
            enmity.ModifyEnmity(Other, Defender, 30);
            enmity.ModifyProximityEnmity(Attacker, Ally, 8);
            var game = typeof(EnmityService).Assembly;
            var carrier = new ReloadStateCarrier(game, new JsonConverter[] { new AbilityDetailReloadConverter(() => new Dictionary<FeatType, AbilityDetail>()) });
            var fields = ReloadStateCarrier.CarriedClasses(game).Single(entry => entry.Type == typeof(EnmityService)).Fields;
            bench.Lines.Add("carried keys: " + string.Join(", ", fields.Select(field => field.Name).OrderBy(name => name, StringComparer.Ordinal)));
            var text = carrier.Write(fields, enmity);
            var reloaded = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EnmityService));
            foreach (var field in fields)
                field.SetValue(reloaded, System.Activator.CreateInstance(field.FieldType));
            carrier.Read(fields, reloaded, text);

            var before = CombatTableState.DumpFields(new[] { (object)enmity }, fields.Select(field => field.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray(), bench.Format).ToArray();
            var after = CombatTableState.DumpFields(new[] { reloaded }, fields.Select(field => field.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray(), bench.Format).ToArray();
            foreach (var line in before)
                bench.Lines.Add("before " + line);
            after.Should().Equal(before, "the carried tables come back as they were");
            bench.Lines.Add("after the reload: the same");
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/Enmity.txt", bench.Text);
    }
}
