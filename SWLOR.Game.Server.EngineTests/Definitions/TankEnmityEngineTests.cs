using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TankEnmityEngineTests
    {
        [EngineTest("Taunts recover accumulated threat and NPCs continue attacking the new tank", Category = "TankEnmity", TimeoutSeconds = 90f)]
        public static async Task ProvokeRecoversAndSwaps(EngineTestContext ctx)
        {
            using var tankPlayer = await PlayerAbilityFixture.CreateAsync(ctx, -2f);
            using var rivalPlayer = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var tank = tankPlayer.Creature;
            var rival = rivalPlayer.Creature;
            foreach (var player in new[] { tank, rival })
            {
                SetAILevel(player, AILevel.VeryLow);
                SetPlotFlag(player, false);
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), player, 120f);
            }
            tankPlayer.Update(player => player.Perks[PerkType.Provoke] = 1);
            rivalPlayer.Update(player => player.Perks[PerkType.Provoke] = 1);
            var enemy = Spawn(ctx, 1f);
            ctx.MakeHostile(enemy);
            SetAILevel(enemy, AILevel.High);
            TemporaryStatModifier.Replace(enemy, StatType.Accuracy, 1000, 120f);
            TemporaryStatModifier.Replace(enemy, StatType.Attack, 200, 120f);
            ctx.SeedRandom(1);
            await ctx.WaitFrameAsync();
            CreaturePlugin.AddFeat(tank, FeatType.Provoke1);
            CreaturePlugin.AddFeat(rival, FeatType.Provoke1);
            Enmity.ModifyEnmity(tank, enemy, 100);
            Enmity.ModifyEnmity(rival, enemy, 20000);
            await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == rival, 8f, "enemy to attack original threat leader");
            await Activate(ctx, tank, enemy, FeatType.Provoke1);
            await ctx.WaitUntilAsync(() => Enmity.GetEnmityTable(enemy).GetValueOrDefault(tank) > 20000, 4f, "Provoke impact to recover the existing deficit");
            await WaitForTarget(ctx, enemy, tank, 8f);
            var hp = GetCurrentHitPoints(tank);
            await ctx.WaitUntilAsync(() => GetLastAttacker(tank) == enemy && GetCurrentHitPoints(tank) < hp,
                15f, "enemy to land an attack on the new tank");
            await ctx.DelaySecondsAsync(6f);
            ctx.AssertEqual(tank, GetAttackTarget(enemy), "Enemy continues attacking the table leader");
            ctx.Assert(Combat.HasRecentAttackActivity(enemy, 3f), "Enemy continues taking attack rolls after the switch");
            await Activate(ctx, rival, enemy, FeatType.Provoke1);
            await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == rival, 8f, "second tank to take the enemy");
            ctx.Assert(Enmity.GetEnmityTable(enemy)[rival] > Enmity.GetEnmityTable(enemy)[tank], "Swap changes the threat leader");
        }

        [EngineTest("Area taunts recover multiple enemies and replace pending approaches", Category = "TankEnmity", TimeoutSeconds = 60f)]
        public static async Task AreaRecovery(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var anchor = arena.Spawn("nw_bandit001", 8f, 90f);
            var isolated = new EngineTestContext(ctx.TestName, arena.Area, GetLocation(anchor));
            DestroyObject(anchor);
            try
            {
                await AreaRecoveryInArena(isolated);
            }
            finally
            {
                isolated.Cleanup();
            }
        }

        private static async Task AreaRecoveryInArena(EngineTestContext ctx)
        {
            // QuietArena's established walkable combat lane runs north of its anchor.
            using var tankPlayer = await PlayerAbilityFixture.CreateAsync(ctx);
            var tank = tankPlayer.Creature;
            tankPlayer.Update(player => player.Perks[PerkType.Provoke] = 2);
            SetAILevel(tank, AILevel.VeryLow);
            SetPlotFlag(tank, false);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), tank, 120f);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), tank, 120f);
            var rival = Spawn(ctx, 0f, 6f);
            var enemies = new[] { Spawn(ctx, 0f, 2f), Spawn(ctx, 0f, 2.5f) };
            foreach (var enemy in enemies)
            {
                ctx.MakeHostile(enemy);
                SetAILevel(enemy, AILevel.High);
                Enmity.ModifyEnmity(rival, enemy, 100000);
            }
            CreaturePlugin.AddFeat(tank, FeatType.Provoke2);
            await Activate(ctx, tank, enemies[0], FeatType.Provoke2);
            try
            {
                await ctx.WaitUntilAsync(() => enemies.All(enemy => Enmity.GetEnmityTable(enemy).GetValueOrDefault(tank) > 100000), 4f, "area Provoke impact to recover both deficits");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"{error.Message}; denial={Ability.GetLastActivationDenialReason()} busy={Activity.IsBusy(tank)} position={GetPosition(tank)} enemies={string.Join(';', enemies.Select(enemy => $"{enemy:X} at {GetPosition(enemy)} threat {Enmity.GetEnmityTable(enemy).GetValueOrDefault(tank)}"))}", error);
            }
            foreach (var enemy in enemies)
            {
                await WaitForTarget(ctx, enemy, tank, 10f);
                ctx.Assert(Enmity.GetEnmityTable(enemy)[tank] > 100000, "Each deficit is recovered independently");
            }
            await ctx.DelaySecondsAsync(6f);
            ctx.Assert(enemies.All(enemy => GetAttackTarget(enemy) == tank && Combat.HasRecentAttackActivity(enemy, 3f)),
                "Both enemies continue attacking the area taunter");
            var first = Enmity.GetEnmityTable(enemies[0])[tank];
            ctx.Assert(Enmity.TryTaunt(tank, enemies[0], 400), "Repeated taunt succeeds");
            ctx.AssertEqual(first, Enmity.GetEnmityTable(enemies[0])[tank], "Repeated taunt cannot farm an existing lead");
        }

        [EngineTest("Damage and effective healing generate threat exactly once", Category = "TankEnmity", TimeoutSeconds = 45f)]
        public static async Task Accounting(EngineTestContext ctx)
        {
            var tank = Spawn(ctx, 0f);
            var healer = Spawn(ctx, -1f);
            var enemy = Spawn(ctx, 1f);
            ctx.MakeHostile(enemy);
            SetAILevel(enemy, AILevel.High);
            foreach (var actor in new[] { tank, healer, enemy })
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), actor, 120f);
            await ctx.WaitFrameAsync();
            ctx.Assert(StatusEffect.ApplyStatusEffect(tank, tank, new BastionStanceStatusEffect(), 120f), "Tank stance applies");
            Enmity.ClearEnmityTable(enemy);
            var before = GetCurrentHitPoints(enemy);
            await ctx.ExecuteInCreatureContextAsync(tank, () => Ability.ApplyHostileCombatImpact(
                tank, enemy, SkillType.HeavyVibroblade, 100, CombatDamageType.Physical));
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(enemy) < before, 5f, "ability damage event");
            var damage = before - GetCurrentHitPoints(enemy);
            var expected = Enmity.CalculateAdjustedEnmity(100, 20) +
                           Enmity.CalculateAdjustedEnmity(Enmity.CalculateDamageEnmity(damage, 100), 20);
            ctx.AssertEqual(expected, Enmity.GetEnmityTable(enemy)[tank], "One damage contribution plus one hostile ability contribution");

            await ctx.ExecuteInCreatureContextAsync(enemy, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(200), tank));
            var missing = GetMaxHitPoints(tank) - GetCurrentHitPoints(tank);
            await ctx.ExecuteInCreatureContextAsync(healer, () => ApplyEffectToObject(DurationType.Instant, EffectHeal(10000), tank));
            ctx.AssertEqual(missing / 2, Enmity.GetEnmityTable(enemy).GetValueOrDefault(healer), "Overheal grants threat only for restored HP to healer");
            var healThreat = Enmity.GetEnmityTable(enemy).GetValueOrDefault(healer);
            await ctx.ExecuteInCreatureContextAsync(healer, () => ApplyEffectToObject(DurationType.Instant, EffectHeal(10000), tank));
            ctx.AssertEqual(healThreat, Enmity.GetEnmityTable(enemy).GetValueOrDefault(healer), "Full-health healing cannot farm threat");
            ctx.Assert(StatusEffect.ApplyStatusEffect(tank, tank, new PerfectAegisStatusEffect(), 120f), "Independent bonus applies");
            StatusEffect.RemoveOtherStanceStatuses(tank, typeof(DefensiveStanceStatusEffect));
            ctx.Assert(StatusEffect.ApplyStatusEffect(tank, tank, new DefensiveStanceStatusEffect(), 120f), "Replacing tank stance applies");
            ctx.AssertEqual(50, Enmity.ClampEnmityPercentAdjustment(Stat.GetStatAdjustment(tank, StatType.EnmityPercentAdjustment)), "Stance plus Aegis respects general cap");
            StatusEffect.RemoveOtherStanceStatuses(tank, typeof(BerserkerStanceStatusEffect));
            ctx.Assert(StatusEffect.ApplyStatusEffect(tank, tank, new BerserkerStanceStatusEffect(), 120f), "Offensive stance replaces tank stance");
            ctx.AssertEqual(0, Stat.GetStatAdjustment(tank, StatType.DamageEnmityPercentAdjustment), "Stance replacement removes damage threat stat");
            ctx.AssertEqual(25, Stat.GetStatAdjustment(tank, StatType.EnmityPercentAdjustment), "Replacement preserves independent Aegis bonus");
            StatusEffect.RemoveAllStatusEffects(tank);
            ctx.AssertEqual(0, Stat.GetStatAdjustment(tank, StatType.EnmityPercentAdjustment), "Full cleanup clears independent bonus");
        }

        [EngineTest("Threat source bonuses stay on their enemy and hidden or dead leaders are skipped", Category = "TankEnmity", TimeoutSeconds = 45f)]
        public static async Task SourceAndTargetValidity(EngineTestContext ctx)
        {
            var tank = Spawn(ctx, 0f);
            var rival = Spawn(ctx, -1f);
            var first = Spawn(ctx, 1f);
            var second = Spawn(ctx, 1.5f);
            ctx.MakeHostile(first);
            ctx.MakeHostile(second);
            foreach (var actor in new[] { tank, rival, first, second })
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), actor, 120f);
            await ctx.WaitFrameAsync();
            ctx.Assert(StatusEffect.ApplyStatusEffect(tank, first, new ChallengeEnmityStatusEffect(30), 120f), "First tank's source bonus applies");
            ctx.Assert(StatusEffect.ApplyStatusEffect(rival, first, new ChallengeEnmityStatusEffect(20), 120f), "Other tank's source bonus applies independently");
            Enmity.ModifyEnmity(tank, first, 100);
            Enmity.ModifyEnmity(tank, second, 100);
            Enmity.ModifyEnmity(rival, first, 100);
            ctx.AssertEqual(130, Enmity.GetEnmityTable(first)[tank], "Matching enemy and source");
            ctx.AssertEqual(100, Enmity.GetEnmityTable(second)[tank], "Unrelated enemy receives no bonus");
            ctx.AssertEqual(120, Enmity.GetEnmityTable(first)[rival], "Rival uses only own source bonus");
            Enmity.ReduceEnmity(tank, first, -50);
            ctx.AssertEqual(130, Enmity.GetEnmityTable(first)[tank], "Negative reductions cannot increase threat");
            Enmity.ReduceEnmity(tank, first, 50);
            ctx.AssertEqual(65, Enmity.GetEnmityTable(first)[tank], "Reduction ignores generation bonuses");
            ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), rival, 120f);
            ctx.AssertEqual(tank, Enmity.GetHighestEnmityAttackTarget(first), "Hidden leader skipped");
            ctx.Assert(!Enmity.TryTaunt(tank, OBJECT_INVALID, 700), "Invalid target rejected");
            ctx.Assert(!Enmity.TryTaunt(tank, second, 0), "Invalid threat amount rejected");
            using (var player = await PlayerAbilityFixture.CreateAsync(ctx, -2f))
                ctx.Assert(!Enmity.TryTaunt(first, player.Creature, 700), "Taunts cannot force player targeting");
            ApplyEffectToObject(DurationType.Instant, EffectDeath(), rival);
            ctx.AssertEqual(tank, Enmity.GetHighestEnmityAttackTarget(first), "Dead leader skipped and cleaned up");
            Enmity.ClearEnmityTable(first);
            ctx.AssertEqual(0, Enmity.GetEnmityTable(first).Count, "Encounter reset clears table");
            ctx.Assert(!Enmity.GetEnmityTowardsAllEnemies(tank).ContainsKey(first), "Reset clears reverse tracking");
        }

        [EngineTest("Owned tank beasts recover threat independently of their owner", Category = "TankEnmity", TimeoutSeconds = 60f)]
        public static async Task CompanionRecovery(EngineTestContext ctx)
        {
            using var owner = await PlayerAbilityFixture.CreateAsync(ctx);
            var record = new Beast { OwnerPlayerId = owner.Id, Name = "Threat test beast", Type = BeastType.Klorslug, Level = 20 };
            record.Perks[PerkType.Anger] = 1;
            DB.Set(record);
            owner.Update(player => player.ActiveBeastId = record.Id);
            var pet = OBJECT_INVALID;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner.Creature, () => BeastMastery.SpawnBeast(owner.Creature, record.Id, 100));
                await ctx.WaitUntilAsync(() => BeastMastery.IsPlayerBeast(GetAssociate(AssociateType.Henchman, owner.Creature)),
                    5f, "owned tank beast to spawn");
                pet = GetAssociate(AssociateType.Henchman, owner.Creature);
                ctx.Track(pet);
                await ctx.DelaySecondsAsync(4.5f);
                SetAILevel(owner.Creature, AILevel.VeryLow);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), owner.Creature, 120f);
                ctx.SuppressNPCNaturalRegen(pet);
                Stat.SetNPCMaxHitPoints(pet, 10000, true);
                ctx.SetNPCResources(pet, 100, 100);
                var enemy = Spawn(ctx, 1f);
                ctx.MakeHostile(enemy);
                SetAILevel(enemy, AILevel.High);
                Enmity.ModifyEnmity(owner.Creature, enemy, 20000);
                var ownerThreat = 0;
                await ctx.ExecuteInCreatureContextAsync(pet, () =>
                {
                    ownerThreat = Enmity.GetEnmityTable(enemy)[owner.Creature];
                    ctx.Assert(UsePerkFeat.TryUseAbility(pet, enemy, FeatType.Anger1, GetLocation(enemy)), "Beast Anger activates");
                    ctx.AssertEqual(ownerThreat, Enmity.GetEnmityTable(enemy)[owner.Creature], "Taunt does not credit owner at impact");
                });
                await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == pet, 10f, "enemy to switch from owner to tank beast");
                ctx.Assert(Enmity.GetEnmityTable(enemy)[pet] > ownerThreat, "Threat belongs to beast");
                ctx.Assert(!Enmity.TryTaunt(enemy, pet, 700), "Owned companions cannot be forced to target a taunter");
                Enmity.RemoveCreatureEnmity(pet);
                await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == owner.Creature, 10f, "enemy to return to surviving owner after pet cleanup");
                foreach (var actor in new[] { pet, enemy })
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), actor, 60f);
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(owner.Creature, () =>
                {
                    var treat = CreateItemOnObject("pet_treat", owner.Creature, 3);
                    ctx.Track(treat);
                    SetItemStackSize(treat, 3);
                    Enmity.ClearEnmityTable(enemy);
                    Enmity.ModifyEnmity(pet, enemy, 100);
                    Enmity.ModifyEnmity(owner.Creature, enemy, 100);
                    ObjectPlugin.SetCurrentHitPoints(pet, GetMaxHitPoints(pet));
                    var reward = Ability.GetAbilityDetail(FeatType.Reward1);
                    reward.ImpactAction(owner.Creature, owner.Creature, 1, GetLocation(owner.Creature));
                    ctx.AssertEqual(100, Enmity.GetEnmityTable(enemy)[owner.Creature], "Full-health Reward generates no healing or support threat");
                    ObjectPlugin.SetCurrentHitPoints(pet, GetMaxHitPoints(pet) - 100);
                    reward.ImpactAction(owner.Creature, owner.Creature, 1, GetLocation(owner.Creature));
                    ctx.AssertEqual(450, Enmity.GetEnmityTable(enemy)[owner.Creature], "Reward credits 100 effective healing / 2 plus one 300 support bonus");
                    var soothe = Ability.GetAbilityDetail(FeatType.SoothePet);
                    soothe.ImpactAction(owner.Creature, owner.Creature, 1, GetLocation(owner.Creature));
                    ctx.AssertEqual(450, Enmity.GetEnmityTable(enemy)[owner.Creature], "Empty cleanse generates no support threat");
                    ctx.Assert(StatusEffect.ApplyStatusEffect(owner.Creature, pet, new PoisonStatusEffect(), 30f), "Pet poison applies");
                    soothe.ImpactAction(owner.Creature, owner.Creature, 1, GetLocation(owner.Creature));
                    ctx.AssertEqual(950, Enmity.GetEnmityTable(enemy)[owner.Creature], "Successful cleanse grants one support bonus");
                });
            }
            finally
            {
                if (GetIsObjectValid(pet))
                {
                    await ctx.ExecuteInCreatureContextAsync(owner.Creature, () => RemoveHenchman(owner.Creature, pet));
                    DestroyObject(pet);
                }
                DB.Delete<Beast>(record.Id);
            }
        }

        [EngineTest("Owned droid damage credits the droid and cannot force companion targeting", Category = "TankEnmity", TimeoutSeconds = 45f)]
        public static async Task DroidAttribution(EngineTestContext ctx)
        {
            using var owner = await PlayerAbilityFixture.CreateAsync(ctx);
            var droid = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner.Creature, () =>
            {
                var cpu = CreateItemOnObject("d_bl_cpu2_m", owner.Creature);
                var controller = CreateItemOnObject(Droid.DroidControlItemResref, owner.Creature);
                ctx.Track(cpu);
                ctx.Track(controller);
                ctx.Assert(GetIsObjectValid(cpu) && GetIsObjectValid(controller), "Droid controller fixtures exist");
                for (var property = GetFirstItemProperty(cpu); GetIsItemPropertyValid(property); property = GetNextItemProperty(cpu))
                    if (GetItemPropertyType(property) == ItemPropertyType.DroidStat)
                        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat,
                            GetItemPropertySubType(property), GetItemPropertyCostTableValue(property)), controller);
                Droid.SaveConstructedDroid(controller, new ConstructedDroid());
                Droid.SpawnDroid(owner.Creature, controller);
                droid = Droid.GetDroid(owner.Creature);
                ctx.Track(droid);
            });
            ctx.Assert(GetIsObjectValid(droid) && Droid.IsDroid(droid), "Owned droid spawned");
            await ctx.DelaySecondsAsync(4.5f);
            var enemy = Spawn(ctx, 2f);
            ctx.MakeHostile(enemy);
            SetAILevel(enemy, AILevel.High);
            foreach (var actor in new[] { droid, enemy })
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), actor, 60f);
            await ctx.WaitFrameAsync();
            Enmity.ClearEnmityTable(enemy);
            await ctx.ExecuteInCreatureContextAsync(droid, () => Ability.ApplyHostileCombatImpact(
                droid, enemy, SkillType.Vibroblade, 50, CombatDamageType.Physical));
            ctx.AssertEqual(150, Enmity.GetEnmityTable(enemy).GetValueOrDefault(droid), "Damage plus hostile impact belongs to droid exactly once");
            ctx.AssertEqual(0, Enmity.GetEnmityTable(enemy).GetValueOrDefault(owner.Creature), "Droid damage does not credit owner");
            ctx.Assert(!Enmity.TryTaunt(enemy, droid, 700), "Taunts cannot force a controlled droid's target");
        }

        private static async Task WaitForTarget(EngineTestContext ctx, uint enemy, uint target, float seconds)
        {
            try
            {
                await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == target && Combat.HasRecentAttackActivity(enemy, 3f), seconds, "actual NPC attack target to match taunter");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"{error.Message}; enemy={enemy:X} expected={target:X} actual={GetAttackTarget(enemy):X} leader={Enmity.GetHighestEnmityAttackTarget(enemy):X} action={GetCurrentAction(enemy)} busy={Activity.IsBusy(enemy)} leash={AI.IsLeashEvading(enemy)} distance={GetDistanceBetween(enemy, target):F1} table={string.Join(',', Enmity.GetEnmityTable(enemy).Select(entry => $"{entry.Key:X}:{entry.Value}"))}", error);
            }
        }

        private static async Task Activate(EngineTestContext ctx, uint source, uint target, FeatType feat)
        {
            var used = false;
            string denial = null;
            await ctx.ExecuteInCreatureContextAsync(source, () =>
            {
                used = UsePerkFeat.TryUseAbility(source, target, feat, GetLocation(target));
                denial = Ability.GetLastActivationDenialReason();
            });
            ctx.Assert(used, $"{feat} activates: {denial}");
        }

        private static uint Spawn(EngineTestContext ctx, float x, float y = 0f)
        {
            var creature = ctx.SpawnCreature("nw_bandit001", x, y);
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 10000, true);
            SetAILevel(creature, AILevel.VeryLow);
            SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
            ctx.SetNPCResources(creature, 100, 100);
            return creature;
        }
    }
}
