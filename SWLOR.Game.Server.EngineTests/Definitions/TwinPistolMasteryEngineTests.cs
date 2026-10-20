using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolMasteryEngineTests
{
    [EngineTest("Twin Pistol actual mastery trials save and reload independent proof and unlocks", Category = "TwinPistolMastery", TimeoutSeconds = 150f)]
    public static async Task ActualTrialsPersist(EngineTestContext ctx)
    {
        // Created area copies receive a generated native resref. The trial deliberately
        // requires the authored public range, so use that loaded area without changing its gate.
        var range = Area.GetAreaByResref("pw_sc_canyonrng");
        ctx.Assert(GetIsObjectValid(range), "The authored Canyon Range is loaded");
        ctx.AssertEqual("pw_sc_canyonrng", GetResRef(range), "The real trial area retains its required native identity");
        var entry = OBJECT_INVALID;
        for (var waypoint = GetFirstObjectInArea(range, ObjectType.Waypoint); GetIsObjectValid(waypoint);
             waypoint = GetNextObjectInArea(range, ObjectType.Waypoint))
            if (GetTag(waypoint) == "pw_sc_canyonrng_entry") { entry = waypoint; break; }
        ctx.Assert(GetIsObjectValid(entry), "Use the authored Canyon Range public entrance, not guessed coordinates");
        var trial = new EngineTestContext(ctx.TestName, range, GetLocation(entry));
        using var cancellation = ctx.CancellationToken.Register(trial.CancelTest);
        var actor = OBJECT_INVALID;
        try
        {
            using var player = await PlayerAbilityFixture.CreateAsync(trial);
            actor = player.Creature;
            ctx.AssertEqual(range, GetArea(actor), "The trial player starts in the authored Canyon Range");
            player.Update(record =>
            {
                record.Skills[SkillType.TwinPistol].Rank = 50;
                record.Skills[SkillType.Pistol].Rank = 0;
                record.Perks[PerkType.TwinPistolMomentum] = 1;
                record.Perks[PerkType.BlasterSlide] = 1;
                record.Perks[PerkType.PistolRush] = 1;
                record.Perks[PerkType.GrappleShot] = 1;
            });
            var main = await trial.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
            var companion = await TwinPistolEquipmentEngineTests.WaitForPair(trial, actor, main);
            var target = trial.SpawnCreature("nw_rat001", 2f);
            var jek = trial.SpawnCreature("cq_lastword", -2f);
            var pavo = trial.SpawnCreature("cq_deadhand", -3f);
            await trial.WaitFrameAsync();
            var firingPosition = GetPosition(target);
            await trial.ExecuteInCreatureContextAsync(actor, () =>
            {
                var origin = GetPosition(actor);
                var found = false;
                for (var direction = 0; direction < 16 && !found; direction++)
                {
                    var angle = direction * MathF.PI / 8;
                    var candidate = GroundLocation(range, origin + new NumericsVector3(MathF.Cos(angle), MathF.Sin(angle), 0) * 3f);
                    if (!string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), candidate, 0f, 4f)) ||
                        !LineOfSightVector(origin, GetPositionFromLocation(candidate))) continue;
                    firingPosition = GetPositionFromLocation(candidate);
                    found = true;
                }
                ctx.Assert(found, "The public Canyon Range entrance provides a reachable firing target location");
            });
            ObjectPlugin.SetPosition(target, firingPosition);
            await trial.WaitFrameAsync();
            await trial.ExecuteInCreatureContextAsync(actor, () =>
            {
                trial.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 150f);
                trial.MakeHostile(target);
                ctx.Assert(GetIsEnemy(target, actor) && LineOfSightObject(actor, target) &&
                           LineOfSightVector(GetPosition(actor), GetPosition(target)), "The firing target is hostile and visible from the authored entrance");
                TwinPistolMastery.RecordOrdinaryHit(actor, target, false);
                ctx.Assert(!KeyItem.HasKeyItem(actor, KeyItemType.TwinSunsCanyonFiringTrace), "No proof is awarded before the quest is accepted");
                ctx.Assert(Quest.AcceptQuest(actor, jek, "twin_suns_mastery"), "Twin rank fifty with Pistol rank zero can accept the firing trial");
            });
            Combat.SetAutoAttackHitResolutionOverride(true);
            AssignCommand(actor, () => ActionAttack(target));
            try
            {
                await trial.WaitUntilAsync(() => DB.Get<Player>(player.Id).Quests["twin_suns_mastery"].CurrentState == 2,
                    38f, "twelve actual ordinary impacts, including six from each launcher");
            }
            catch (Exception exception)
            {
                var attempts = (Dictionary<uint, TwinPistolTrialProgress>)typeof(TwinPistolMastery)
                    .GetField("Attempts", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                var counts = attempts.TryGetValue(actor, out var progress)
                    ? $"main={typeof(TwinPistolTrialProgress).GetField("_mainHits", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(progress)}, " +
                      $"companion={typeof(TwinPistolTrialProgress).GetField("_companionHits", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(progress)}"
                    : "no active trial tracker";
                throw new InvalidOperationException($"{exception.Message}; {counts}; area={GetResRef(GetArea(actor))}; " +
                    $"pair={TwinPistolEquipment.IsActivePair(actor)}, inCombat={GetIsInCombat(actor)}, attackTarget={GetAttackTarget(actor)}, " +
                    $"actor={GetPosition(actor)}, target={GetPosition(target)}, hostile={GetIsEnemy(target, actor)}, " +
                    $"objectLOS={LineOfSightObject(actor, target)}, vectorLOS={LineOfSightVector(GetPosition(actor), GetPosition(target))}", exception);
            }
            await trial.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
            PairedRangedAttackStop(actor);
            ctx.Assert(KeyItem.HasKeyItem(actor, KeyItemType.TwinSunsCanyonFiringTrace), "A completed native firing sequence awards its persisted proof");
            ctx.AssertEqual(0, DB.Get<Player>(player.Id).Quests["twin_suns_mastery"].TimesCompleted, "Proof still needs the trainer's turn-in");

            await trial.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(Quest.AcceptQuest(actor, pavo, "final_salvo_mastery"), "The movement trial is accepted independently");
                CreaturePlugin.RunUnequip(actor, companion);
                CreaturePlugin.AddFeat(actor, FeatType.BlasterSlide1);
                CreaturePlugin.AddFeat(actor, FeatType.PistolRush1);
                CreaturePlugin.AddFeat(actor, FeatType.GrappleShot1);
            });
            await TwinPistolEquipmentEngineTests.WaitForRemovedPair(trial, actor, main, companion);
            var routeWeapon = await trial.EquipItemAsync(actor, "b_longsword", InventorySlot.RightHand);
            ctx.Assert(!TwinPistolEquipment.IsActivePair(actor), "The movement mastery supports a cross-trained single weapon");
            Combat.SetAutoAttackHitResolutionOverride(false);
            Combat.SetAbilityHitResolutionOverride(true);
            var origin = GetPosition(actor);
            NumericsVector3 unit = default;
            var laneFound = false;
            await trial.ExecuteInCreatureContextAsync(actor, () =>
            {
                for (var direction = 0; direction < 16 && !laneFound; direction++)
                {
                    var angle = direction * MathF.PI / 8;
                    var candidate = new NumericsVector3(MathF.Cos(angle), MathF.Sin(angle), 0);
                    var forward = GroundLocation(range, origin + candidate * 4f);
                    var behind = GroundLocation(range, origin - candidate * 2f);
                    if (!string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), forward, 2f, 6f)) ||
                        !string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), behind, 0f, 6f))) continue;
                    unit = candidate;
                    laneFound = true;
                }
                ctx.Assert(laneFound, "The authored entrance supplies a safe forward lane and rear target position");
            });
            ObjectPlugin.SetPosition(target, GetPositionFromLocation(GroundLocation(range, origin - unit * 2f)));
            await trial.WaitFrameAsync();
            await UseMovement(trial, actor, target, FeatType.BlasterSlide1, GetLocation(target));
            await trial.DelaySecondsAsync(2.05f);

            var secondaryPosition = origin + unit * 2f + new NumericsVector3(-unit.Y, unit.X, 0) * 1.2f;
            var secondary = OBJECT_INVALID;
            await trial.ExecuteInCreatureContextAsync(actor, () =>
            {
                secondary = CreateObject(ObjectType.Creature, "nw_rat001", GroundLocation(range, secondaryPosition));
                trial.Track(secondary);
                ctx.Assert(GetIsObjectValid(secondary), "Create a hostile target beside, not blocking, the Rush travel path");
                trial.MakeHostile(secondary);
                Stat.SetNPCMaxHitPoints(secondary, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), secondary, 90f);
            });
            await trial.WaitFrameAsync();
            await UseMovement(trial, actor, OBJECT_INVALID, FeatType.PistolRush1, GroundLocation(range, origin));
            await trial.DelaySecondsAsync(2.05f);
            await UseMovement(trial, actor, target, FeatType.GrappleShot1, GetLocation(target));
            ctx.AssertEqual(2, DB.Get<Player>(player.Id).Quests["final_salvo_mastery"].CurrentState,
                "Actual Slide and Rush movement plus a charged landed Grapple award the route proof");
            ctx.Assert(KeyItem.HasKeyItem(actor, KeyItemType.FinalSalvoCanyonRouteTrace), "The second path owns separate proof");

            for (var cycle = 0; cycle < 2; cycle++)
            {
                await trial.ExecuteInCreatureContextAsync(actor, () => DB.Set(DB.Get<Player>(player.Id)));
                await trial.WaitFrameAsync(); // Script context end clears DB's entity cache before the reload.
                var restored = DB.Get<Player>(player.Id);
                ctx.AssertEqual(2, restored.Quests["twin_suns_mastery"].CurrentState, "Firing proof state survives a real DB reload");
                ctx.AssertEqual(2, restored.Quests["final_salvo_mastery"].CurrentState, "Route proof state survives a real DB reload");
                ctx.Assert(restored.KeyItems.ContainsKey(KeyItemType.TwinSunsCanyonFiringTrace) &&
                           restored.KeyItems.ContainsKey(KeyItemType.FinalSalvoCanyonRouteTrace), "Both independently earned proofs survive repeated persistence cycles");
                ctx.AssertEqual(actor, GetItemPossessor(main), "Saving quest proof preserves the owned main gun");
                ctx.Assert(!GetIsObjectValid(companion), "Saving quest proof cannot restore the destroyed temporary gun");
                TwinPistolEquipmentEngineTests.AssertOwnedItems(ctx, actor, main, 0);
                ctx.AssertEqual(actor, GetItemPossessor(routeWeapon), "Saving quest proof preserves the cross-trained route weapon");
                ctx.AssertEqual(routeWeapon, GetItemInSlot(InventorySlot.RightHand, actor), "The cross-trained route weapon stays equipped across proof reloads");
            }
            await trial.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(Quest.AdvanceQuest(actor, jek, "twin_suns_mastery"), "Jek completes only the firing mastery path");
                var restored = DB.Get<Player>(player.Id);
                ctx.AssertEqual(1, restored.Quests["twin_suns_mastery"].TimesCompleted, "Firing mastery is recorded exactly once");
                ctx.AssertEqual(0, restored.Quests["final_salvo_mastery"].TimesCompleted, "Firing mastery never unlocks Final Salvo automatically");
                ctx.Assert(!restored.Perks.ContainsKey(PerkType.TwinSuns), "Mastery approval grants no free perk rank");
                ctx.Assert(!restored.KeyItems.ContainsKey(KeyItemType.TwinSunsCanyonFiringTrace), "The trainer consumes only the firing proof");
                ctx.Assert(restored.KeyItems.ContainsKey(KeyItemType.FinalSalvoCanyonRouteTrace), "The other proof remains available for its own turn-in");
                ctx.Assert(Quest.AdvanceQuest(actor, pavo, "final_salvo_mastery"), "Pavo completes his independent movement mastery");
            });
        }
        finally
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
            if (GetIsObjectValid(actor))
            {
                TwinPistolAbility.Cancel(actor);
                PairedRangedAttackStop(actor);
                PairedRangedTraits.ClearActor(actor);
            }
            trial.Cleanup();
        }
    }

    private static async Task UseMovement(EngineTestContext context, uint actor, uint target, FeatType feat, Location location)
    {
        await context.ExecuteInCreatureContextAsync(actor, () =>
        {
            Ability.ClearLastCompletedAbilityImpactSummary(actor);
            var activated = UsePerkFeat.TryUseAbility(actor, target, feat, location);
            context.Assert(activated, $"The mastery action uses the normal validated feat pipeline: {Ability.GetLastActivationDenialReason()}");
        });
        await context.WaitUntilAsync(() => !WeaponRepositioning.IsMoving(actor) &&
            Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
            8f, "confirmed movement and a landed captured attack");
        // A single melee weapon naturally resumes pursuit after its paid impact.
        // Stop that ordinary follow-up before choosing the next player-directed route leg.
        await context.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
        PairedRangedAttackStop(actor);
    }

    private static void PairedRangedAttackStop(uint actor) => SWLOR.Game.Server.Native.PairedRangedAttackCycle.Cancel(actor);

    private static Location GroundLocation(uint area, NumericsVector3 position)
    {
        position.Z = GetGroundHeight(Location(area, position, 0));
        return Location(area, position, 0);
    }
}
