using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using Activity = SWLOR.Game.Server.Service.Activity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NumericsVector3 = System.Numerics.Vector3;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponRotationBalanceEngineTests
{
    private const int TargetMaximumHP = 30000;
    private const int TargetDefense = 94;
    private const int TargetEvasion = 134;
    private const int ItemDelayUnits = 250;
    private const int ItemDelayPropertyCost = 25;
    private const string CorridorAreaResref = "tat_anc_flatlnd2";
    private const string SeedsEnvironmentVariable = "SWLOR_WEAPON_ROTATION_SEEDS";
    private static readonly int[] DefaultSeeds = { 20261013, 20261014, 20261015 };
    private static uint _actor = OBJECT_INVALID;
    private static uint _target = OBJECT_INVALID;
    private static int _attributedDamage;
    private static int _ordinaryHits;
    private static int _ordinaryResolvedDamage;
    private static int _activatedResolvedDamage;
    private static readonly Dictionary<AbilityImpactSummary, int> ObservedImpacts = new();
    private static readonly Dictionary<string, int> ActivatedDamageByAbility = new();

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveDamage()
    {
        if (OBJECT_SELF == _target && GetLastDamager(_target) == _actor)
            _attributedDamage += Math.Max(0, GetTotalDamageDealt());
    }

    [NWNEventHandler(ScriptName.OnSWLORDamage)]
    public static void ObserveOrdinaryHits()
    {
        if (OBJECT_SELF != _actor || StringToObject(EventsPlugin.GetEventData("DEFENDER")) != _target) return;
        // GetDamageRoll suppresses queued replacements before publishing this event.
        // Its DAMAGE field is the landed ordinary roll, including the companion's own roll.
        var damage = int.Parse(EventsPlugin.GetEventData("DAMAGE"));
        if (damage <= 0) return;
        _ordinaryHits++;
        _ordinaryResolvedDamage += damage;
    }

    [EngineTest("Controlled Pistol critical rotation records actual damage and finite player stamina", Category = "WeaponRotationBalance", TimeoutSeconds = 480f)]
    public static Task Pistol(EngineTestContext ctx) => Run(ctx, PerkCategoryType.PistolGunslinger);

    [EngineTest("Controlled Barrage Matched Rhythm rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 480f)]
    public static Task BarrageMatched(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolBarrage, PerkType.MatchedRhythm);

    [EngineTest("Controlled Barrage Paired Handling rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 480f)]
    public static Task BarrageHandling(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolBarrage, PerkType.PairedHandling);

    [EngineTest("Controlled Crossfire Paired Handling rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 480f)]
    public static Task CrossfireHandling(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolCrossfire, PerkType.PairedHandling);

    [EngineTest("Controlled Crossfire Matched Rhythm rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 480f)]
    public static Task CrossfireMatched(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolCrossfire, PerkType.MatchedRhythm);

    [EngineTest("Pistol capstone opening records six and twelve seconds of finite-stamina damage", Category = "WeaponOpeningBalance", TimeoutSeconds = 160f)]
    public static Task PistolOpening(EngineTestContext ctx) => Run(ctx, PerkCategoryType.PistolGunslinger, opening: true);

    [EngineTest("Barrage Matched Rhythm capstone opening records damage at 69 SP", Category = "WeaponOpeningBalance", TimeoutSeconds = 160f)]
    public static Task BarrageMatchedOpening(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolBarrage, PerkType.MatchedRhythm, true);

    [EngineTest("Barrage Paired Handling capstone opening records damage at 69 SP", Category = "WeaponOpeningBalance", TimeoutSeconds = 160f)]
    public static Task BarrageHandlingOpening(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolBarrage, PerkType.PairedHandling, true);

    [EngineTest("Crossfire Paired Handling capstone opening records damage at 69 SP", Category = "WeaponOpeningBalance", TimeoutSeconds = 160f)]
    public static Task CrossfireHandlingOpening(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolCrossfire, PerkType.PairedHandling, true);

    [EngineTest("Crossfire Matched Rhythm capstone opening records damage at 69 SP", Category = "WeaponOpeningBalance", TimeoutSeconds = 160f)]
    public static Task CrossfireMatchedOpening(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolCrossfire, PerkType.MatchedRhythm, true);

    private static async Task Run(EngineTestContext ctx, PerkCategoryType category, PerkType handling = PerkType.Invalid,
        bool opening = false)
    {
        var area = ctx.CreateInstancedArea(CorridorAreaResref);
        SetLocalString(area, "CREATURE_SPAWN_TABLE_ID", string.Empty);
        SetLocalString(area, "RESOURCE_SPAWN_TABLE_ID", string.Empty);
        await ctx.DelaySecondsAsync(1f);
        var center = new NumericsVector3(GetAreaSize(Dimension.Width, area) * 5f,
            GetAreaSize(Dimension.Height, area) * 5f, 0);
        center.Z = GetGroundHeight(Location(area, center, 0));
        var results = new List<JsonElement>();
        foreach (var seed in ReadSeeds())
        {
            var sample = new EngineTestContext($"{ctx.TestName} seed={seed}", area,
                Location(area, center - new NumericsVector3(2, 0, 0), 0));
            using var cancellation = ctx.CancellationToken.Register(sample.CancelTest);
            try
            {
                await RunSeed(sample, category, handling, seed, center, opening);
                results.Add(JsonSerializer.Deserialize<JsonElement>(sample.ResultDetail));
            }
            finally
            {
                sample.Cleanup();
                await ctx.WaitFrameAsync();
            }
        }
        var sustained = results.Select(result => result.GetProperty("observations").EnumerateArray().Last()
            .GetProperty("dps").GetDouble()).ToArray();
        if (opening)
        {
            var sixSeconds = results.Select(result => result.GetProperty("observations").EnumerateArray().First()).ToArray();
            var twelveSeconds = results.Select(result => result.GetProperty("observations").EnumerateArray().Last()).ToArray();
            ctx.SetResultDetail(JsonSerializer.Serialize(new
            {
                seeds = results,
                meanSixSecondDamage = sixSeconds.Average(result => result.GetProperty("attributedDamage").GetInt32()),
                meanTwelveSecondDamage = twelveSeconds.Average(result => result.GetProperty("attributedDamage").GetInt32()),
                meanOpeningDps = sustained.Average(), minimumOpeningDps = sustained.Min(), maximumOpeningDps = sustained.Max(),
                method = "Equal 69-SP native player builds, three fixed seeds by default, with a 12-second input window in the same verified flat corridor. " +
                    "Request the purchased capstone first at zero seconds, then core feats at two, three and four seconds, subject to real timing, readiness and finite stamina. " +
                    "Six/twelve-second snapshots report delivered HP and resolved ordinary/activated damage; an eight-second post-stop drain reconciles exact attribution. " +
                    "Final Salvo opens without prior Momentum. This representative capstone-first rotation does not establish globally optimized maximum burst."
            }));
            return;
        }
        ctx.SetResultDetail(JsonSerializer.Serialize(new
        {
            seeds = results,
            meanSustainedDps = sustained.Average(), minimumSustainedDps = sustained.Min(), maximumSustainedDps = sustained.Max(),
            method = "Equal 69-SP native player builds, three fixed seeds by default, 120s each in a verified flat open corridor. " +
                "Ordinary resolved damage and completed activated impact damage are tracked separately and reconciled to actual HP after a bounded projectile drain. " +
                "Timed windows retain delivered HP, resolution totals, actual XYZ, finite stamina and all movement/cast inputs. No timed repositioning or forced hits."
        }));
    }

    private static async Task RunSeed(EngineTestContext ctx, PerkCategoryType category, PerkType handling, int seed,
        NumericsVector3 center, bool opening)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        var pistol = category == PerkCategoryType.PistolGunslinger;
        var crossfire = category == PerkCategoryType.TwinPistolCrossfire;
        var line = Perk.GetAllPerks().Values.Where(perk => perk.Category == category).ToArray();
        if (!pistol)
        {
            ctx.Assert(handling is PerkType.MatchedRhythm or PerkType.PairedHandling, "The paired build selects one legal handling purchase");
            line = line.Where(perk => perk.Type is not (PerkType.MatchedRhythm or PerkType.PairedHandling))
                .Append(Perk.GetPerkDetails(handling)).ToArray();
        }
        var armor = Perk.GetPerkDetails(pistol ? PerkType.Doublehand : PerkType.DualWield);
        var price = line.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price) +
                    armor.PerkLevels.Values.Sum(level => level.Price);
        ctx.AssertEqual(69, price, "Every complete line with its selected handling purchase and Armor trait has equal investment");

        player.Update(record =>
        {
            ConfigureRecord(record);
            record.Skills[pistol ? SkillType.Pistol : SkillType.TwinPistol].Rank = 50;
            foreach (var perk in line.Append(armor))
            {
                record.Perks[perk.Type] = perk.PerkLevels.Keys.Max();
                foreach (var requirement in perk.PerkLevels.Values.SelectMany(level => level.Requirements)
                             .OfType<PerkRequirementQuest>())
                    record.Quests[requirement.QuestId] = new PlayerQuest { TimesCompleted = 1, DateLastCompleted = DateTime.UtcNow };
            }
        });
        if (!pistol)
        {
            var excluded = handling == PerkType.MatchedRhythm ? PerkType.PairedHandling : PerkType.MatchedRhythm;
            ctx.AssertEqual(3, Perk.GetPerkLevel(actor, handling), "The selected handling purchase has its actual maximum rank");
            ctx.AssertEqual(0, Perk.GetPerkLevel(actor, excluded), "The mutually exclusive handling purchase is absent");
            foreach (var requirement in Perk.GetPerkDetails(handling).PerkLevels.Values.SelectMany(level => level.Requirements)
                         .OfType<PerkRequirementCannotHavePerk>())
                ctx.Assert(string.IsNullOrEmpty(requirement.CheckRequirements(actor)), "The build satisfies actual handling purchase exclusivity");
        }
        foreach (var requirement in line.SelectMany(perk => perk.PerkLevels.Values)
                     .SelectMany(level => level.Requirements).OfType<PerkRequirementQuest>())
            ctx.Assert(string.IsNullOrEmpty(requirement.CheckRequirements(actor)), "The complete build owns its actual mastery unlock");
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            SetEventScript(actor, EventScript.Creature_OnHeartbeat, "");
            SetEventScript(actor, EventScript.Creature_OnNotice, "");
            SetEventScript(actor, EventScript.Creature_OnMeleeAttacked, "");
            SetEventScript(actor, EventScript.Creature_OnDamaged, ScriptName.OnPlayerDamaged);
            ClearAllActions();
            foreach (var ability in Enum.GetValues<AbilityType>().Where(value => value != AbilityType.Invalid))
                CreaturePlugin.SetRawAbilityScore(actor, ability, 26);
            foreach (var feat in line.Append(armor).SelectMany(perk => perk.PerkLevels.Values)
                         .SelectMany(level => level.GrantedFeats).Distinct())
                CreaturePlugin.AddFeat(actor, feat);
        });
        await ctx.ExecuteInCreatureContextAsync(target, () =>
        {
            SetEventScript(target, EventScript.Creature_OnHeartbeat, "");
            SetEventScript(target, EventScript.Creature_OnNotice, "");
            SetEventScript(target, EventScript.Creature_OnMeleeAttacked, "");
            SetEventScript(target, EventScript.Creature_OnEndCombatRound, "");
            SetEventScript(target, EventScript.Creature_OnSpellCastAt, "");
            SetEventScript(target, EventScript.Creature_OnDamaged, ScriptName.OnCreatureDamagedAfter);
            ClearAllActions();
            SetPlotFlag(target, false);
            ctx.Assert(!GetIsPC(target) && !GetPlotFlag(target), "The benchmark target is a damageable NPC, not a player HP override");
            foreach (var ability in Enum.GetValues<AbilityType>().Where(value => value != AbilityType.Invalid))
                CreaturePlugin.SetRawAbilityScore(target, ability, 26);
            ctx.SuppressNPCNaturalRegen(target);
            // Apply the final NPC HP budget only after its spawn initialization and Vitality settle.
            Stat.SetNPCMaxHitPoints(target, TargetMaximumHP, true);
            ctx.AssertEqual(TargetMaximumHP, GetMaxHitPoints(target), "The NPC has its actual native maximum HP budget");
            ctx.AssertEqual(TargetMaximumHP, GetCurrentHitPoints(target), "The NPC begins at its actual native maximum HP");
            // Preserve the former player defender's measured defense and evasion budgets.
            TemporaryStatModifier.Add(target, StatType.PhysicalDefense,
                TargetDefense - Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality), 180f, "rotation-defense");
            TemporaryStatModifier.Add(target, StatType.Evasion,
                TargetEvasion - Stat.GetEvasion(target, SkillType.Invalid), 180f, "rotation-evasion");
            ctx.AssertEqual(TargetDefense, Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality), "The NPC preserves the reference defense budget");
            ctx.AssertEqual(TargetEvasion, Stat.GetEvasion(target, SkillType.Invalid), "The NPC preserves the reference evasion budget");
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), target, 180f);
            SetAILevel(target, AILevel.VeryLow);
            SetCommandable(false, target);
            ctx.MakeHostile(target);
            ctx.Assert(GetIsReactionTypeHostile(target, actor), "The durable NPC is hostile to the real player attacker");
        });

        var main = await ctx.EquipItemAsync(actor, pistol ? "b_pistol" : "b_twinpistol", InventorySlot.RightHand);
        var companion = pistol ? OBJECT_INVALID : await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            foreach (var weapon in new[] { main, companion }.Where(GetIsObjectValid))
                ConfigureWeapon(weapon);
            ctx.AssertEqual(pistol ? 36 : 22, WeaponDamage.GetEffectiveDMG(actor, main), "Single Weapon and Doublehand apply once, while paired guns retain their own DMG");
            ctx.AssertEqual(61, Stat.GetMaxStamina(actor), "The real player pool is 10 base +39 Might +12 reference gear");
            ctx.AssertEqual(pistol ? 0 : 30, Combat.CalculateOffhandAttackDelayReduction(actor), "Armor specialization matches the equipped setup");
            AssertWeaponTiming(ctx, actor, main, companion, pistol);
            VerifyCorridor(ctx, actor, target, center);
        });
        await ctx.WaitFrameAsync();
        _actor = actor;
        _target = target;
        _attributedDamage = 0;
        try
        {
            ctx.Assert(GetMaxHitPoints(target) >= TargetMaximumHP && GetCurrentHitPoints(target) >= TargetMaximumHP,
                "The durable NPC retains its verified native HP before the attribution smoke check");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                ApplyEffectToObject(DurationType.Instant, EffectDamage(1), target));
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) == TargetMaximumHP - 1 && _attributedDamage == 1,
                3f, "one real player-attributed NPC damage event before any timed measurements");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                ApplyEffectToObject(DurationType.Instant, EffectHeal(TargetMaximumHP), target));
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) == TargetMaximumHP, 3f,
                "the NPC to return to its verified native HP budget after the attribution smoke check");
        }
        finally
        {
            _actor = OBJECT_INVALID;
            _target = OBJECT_INVALID;
            _attributedDamage = 0;
        }
        var stance = pistol ? FeatType.GamblerStance1 : crossfire ? FeatType.CrossfireStance : FeatType.AkimboStance;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
            ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, stance, GetLocation(actor)), "The purchased stance activates through its real feat"));
        await ctx.WaitUntilAsync(() => pistol ? StatusEffect.HasStatusEffect<GamblerStanceStatusEffect>(actor) :
            crossfire ? StatusEffect.HasStatusEffect<CrossfireStanceStatusEffect>(actor) :
            StatusEffect.HasStatusEffect<AkimboStanceStatusEffect>(actor), 6f, "the stance's normal activation");
        await ctx.ExecuteInCreatureContextAsync(actor, () => AssertWeaponTiming(ctx, actor, main, companion, pistol));

        var capstone = pistol ? FeatType.DeadMansHand1 : crossfire ? FeatType.FinalSalvo : FeatType.TwinSuns;
        var coreFeats = pistol ? new[] { FeatType.DoubleShot3, FeatType.QuickDraw4, FeatType.FanTheHammer2 } :
            crossfire ? new[] { FeatType.BlasterSlide4, FeatType.PistolRush2, FeatType.GrappleShot3 } :
            new[] { FeatType.FocusedBurst3, FeatType.PairedVolley4, FeatType.ControlledBurst2 };
        var feats = opening ? new[] { capstone }.Concat(coreFeats).ToArray() : coreFeats.Append(capstone).ToArray();
        var next = opening ? new[] { 0d, 2d, 3d, 4d } :
            coreFeats.Select((_, index) => (double)(crossfire ? index * 2 : index)).Append(8d).ToArray();
        var casts = feats.ToDictionary(feat => feat, _ => 0);
        var denials = new Dictionary<string, int>();
        var snapshots = new List<object>();
        var movements = new List<object>();
        (FeatType Feat, NumericsVector3 Origin, NumericsVector3 Target, NumericsVector3 Cursor,
            double AcceptedAt, bool SawMoving)? pendingMovement = null;
        var naturalTicks = 0;
        var naturalRestored = 0;
        var nextNatural = 6d;
        var nextAttackResumeCheck = 0d;
        var controllerAttackResumes = 0;
        var inputSeconds = opening ? 12 : 120;
        var windows = new Queue<int>(opening ? new[] { 6, 12 } : new[] { 6, 12, 120 });
        var startingHP = GetCurrentHitPoints(target);
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);
        _actor = actor;
        _target = target;
        _attributedDamage = 0;
        _ordinaryHits = 0;
        _ordinaryResolvedDamage = _activatedResolvedDamage = 0;
        ObservedImpacts.Clear();
        ActivatedDamageByAbility.Clear();
        Ability.ClearLastCompletedAbilityImpactSummary(actor);
        var clock = new Stopwatch();
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                SetEventScript(actor, EventScript.Creature_OnHeartbeat, "");
                SetLocalInt(actor, Stat.SuppressNaturalRegenVariable, 1);
                SetLocalInt(actor, "NATURAL_REGENERATION_TICK", 0);
                SetLocalInt(actor, "NATURAL_STAMINA_REGEN_REMAINDER", 0);
                player.Update(record => record.Stamina = Stat.GetMaxStamina(actor));
                ctx.SeedRandom(seed);
                clock.Start();
                ActionAttack(target);
            });
            while (windows.Count > 0)
            {
                var elapsed = clock.Elapsed.TotalSeconds;
                if (opening) ctx.Assert(elapsed < inputSeconds + 8, "The opening controller stays within its bounded input budget");
                SampleCompletedAbilityDamage(actor);
                if (pendingMovement is { } movement)
                {
                    if (WeaponRepositioning.IsMoving(actor))
                    {
                        movement.SawMoving = true;
                        pendingMovement = movement;
                    }
                    else if (!Activity.IsBusy(actor) && elapsed >= movement.AcceptedAt + .25d)
                    {
                        var end = GetPosition(actor);
                        var observation = new
                        {
                            seed, feat = movement.Feat.ToString(), acceptedAt = movement.AcceptedAt,
                            finishedAt = elapsed, sawNativeMovement = movement.SawMoving,
                            origin = XYZ(movement.Origin), targetAtActivation = XYZ(movement.Target), cursor = XYZ(movement.Cursor),
                            actualEnd = XYZ(end), distanceMoved = NumericsVector3.Distance(movement.Origin, end),
                            targetNow = XYZ(GetPosition(target)), sameFloor = Math.Abs(end.Z - center.Z) <= .25f
                        };
                        ctx.Assert(observation.sameFloor, "Real movement remains on the controlled floor");
                        movements.Add(observation);
                        ctx.Log("Movement " + JsonSerializer.Serialize(observation));
                        pendingMovement = null;
                    }
                }
                if (elapsed >= nextNatural && nextNatural <= inputSeconds)
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        var before = Stat.GetCurrentStamina(actor);
                        SetLocalInt(actor, Stat.SuppressNaturalRegenVariable, 0);
                        try { NaturalRegeneration.ProcessRegeneration(); }
                        finally { SetLocalInt(actor, Stat.SuppressNaturalRegenVariable, 1); }
                        naturalRestored += Math.Max(0, Stat.GetCurrentStamina(actor) - before);
                        naturalTicks++;
                    });
                    nextNatural += 6;
                }
                if (elapsed >= windows.Peek())
                {
                    var requested = windows.Dequeue();
                    ctx.Assert(Math.Abs(GetPosition(actor).Z - center.Z) <= .25f && Math.Abs(GetPosition(target).Z - center.Z) <= .25f,
                        "Timed attacker and target positions remain on the same controlled floor");
                    ctx.Assert(!GetIsDead(target) && GetCurrentHitPoints(target) > 0, "The durable NPC remains alive for the complete observation window");
                    ctx.Assert(_attributedDamage > 0, "Real player-attributed damage is present by the first six-second observation");
                    var observation = new
                    {
                        build = category.ToString(), handling = handling.ToString(), investmentSP = price, seed,
                        armorTrait = armor.Type.ToString(), skillRank = 50, attributeScores = 26,
                        itemDamage = 22, itemDelay = ItemDelayUnits, itemDelayUnit = "custom delay units",
                        itemDelayPropertyCost = ItemDelayPropertyCost, equippedAttackWeapons = pistol ? 1 : 2,
                        rawAttackDelayMilliseconds = Combat.CalculateAttackDelay(actor),
                        effectiveAttackDelayMilliseconds = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(actor)),
                        capstone = capstone.ToString(), acceptedCapstoneCasts = casts[capstone],
                        requestedSeconds = requested, observedSeconds = clock.Elapsed.TotalSeconds,
                        sourceAttack = Stat.GetAttack(actor, AbilityType.Perception, pistol ? SkillType.Pistol : SkillType.TwinPistol),
                        targetDefense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality),
                        sourceAccuracy = Stat.GetAccuracy(actor, main, AbilityType.Agility, pistol ? SkillType.Pistol : SkillType.TwinPistol),
                        targetEvasion = Stat.GetEvasion(target, SkillType.Invalid),
                        targetKind = "NPC", targetBlueprint = GetResRef(target),
                        targetMaximumHP = GetMaxHitPoints(target), targetCurrentHP = GetCurrentHitPoints(target),
                        hpDamage = startingHP - GetCurrentHitPoints(target), attributedDamage = _attributedDamage,
                        ordinaryResolvedDamage = _ordinaryResolvedDamage, activatedResolvedDamage = _activatedResolvedDamage,
                        resolvedMinusDelivered = _ordinaryResolvedDamage + _activatedResolvedDamage - _attributedDamage,
                        activatedDamageByAbility = new Dictionary<string, int>(ActivatedDamageByAbility),
                        actorPosition = XYZ(GetPosition(actor)), targetPosition = XYZ(GetPosition(target)),
                        dps = _attributedDamage / clock.Elapsed.TotalSeconds,
                        stamina = Stat.GetCurrentStamina(actor), maximumStamina = Stat.GetMaxStamina(actor),
                        naturalTicks, naturalRestored, ordinaryHits = _ordinaryHits,
                        controllerAttackResumes,
                        acceptedCasts = casts.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
                        denials = new Dictionary<string, int>(denials),
                        momentum = StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0,
                        haste = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent)
                    };
                    snapshots.Add(observation);
                    ctx.Log(JsonSerializer.Serialize(observation));
                    if (windows.Count == 0) break;
                }
                for (var index = 0; index < feats.Length; index++)
                {
                    if (elapsed < next[index] || Activity.IsBusy(actor) || WeaponRepositioning.IsMoving(actor)) continue;
                    var feat = feats[index];
                    var ability = Ability.GetAbilityDetail(feat);
                    if (Recast.IsOnRecastDelay(actor, ability.RecastGroup).Item1) continue;
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        var origin = GetPosition(actor);
                        var targetPosition = GetPosition(target);
                        var location = feat == FeatType.PistolRush2 ? RushDestination(actor, target, center) : GetLocation(target);
                        var accepted = location != null && UsePerkFeat.TryUseAbility(actor,
                            feat == FeatType.PistolRush2 ? OBJECT_INVALID : target, feat, location);
                        SampleCompletedAbilityDamage(actor);
                        if (accepted)
                        {
                            casts[feat]++;
                            next[index] = clock.Elapsed.TotalSeconds + ability.RecastDelay(actor) + .1d;
                            if (feat is FeatType.BlasterSlide4 or FeatType.PistolRush2 or FeatType.GrappleShot3)
                                pendingMovement = (feat, origin, targetPosition, GetPositionFromLocation(location), elapsed, false);
                        }
                        else
                        {
                            var reason = location == null ? "No safe Rush lane" : Ability.GetLastActivationDenialReason();
                            reason = $"{feat}: {reason}";
                            denials[reason] = denials.GetValueOrDefault(reason) + 1;
                            if (denials[reason] == 1)
                                ctx.Log(JsonSerializer.Serialize(new { seed, denied = reason, elapsed,
                                    actorPosition = XYZ(origin), targetPosition = XYZ(targetPosition),
                                    cursor = location == null ? null : XYZ(GetPositionFromLocation(location)) }));
                            next[index] = clock.Elapsed.TotalSeconds + 1d;
                        }
                    });
                }
                if (Activity.IsBusy(actor) || WeaponRepositioning.IsMoving(actor))
                {
                    // Let the normal post-cast/movement resume callback settle first.
                    nextAttackResumeCheck = clock.Elapsed.TotalSeconds + .25d;
                }
                else if (clock.Elapsed.TotalSeconds >= nextAttackResumeCheck)
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSObject();
                        if (!WeaponAttackAnimation.HasQueuedAttack(native, target))
                        {
                            ActionAttack(target);
                            controllerAttackResumes++;
                        }
                    });
                }
                await ctx.DelaySecondsAsync(.1f);
            }
            ctx.Assert(_attributedDamage > 0 && _ordinaryHits > 0 && casts.Values.Sum() > 0, "Real ordinary attacks and paid ability activations produced measurable results");
            ctx.Assert(casts[capstone] > 0, "The purchased capstone actually entered the paid rotation");
            ctx.AssertEqual(inputSeconds / 6, naturalTicks, "All controlled six-second production heartbeat budgets ran");
            ctx.AssertEqual(_attributedDamage, startingHP - GetCurrentHitPoints(target), "No bystander damage or target healing contaminated the observation");
            await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
            var deliveredAtStop = _attributedDamage;
            var drain = Stopwatch.StartNew();
            while (drain.Elapsed.TotalSeconds < 8)
            {
                SampleCompletedAbilityDamage(actor);
                await ctx.DelaySecondsAsync(.1f);
            }
            SampleCompletedAbilityDamage(actor);
            ctx.AssertEqual(_attributedDamage, _ordinaryResolvedDamage + _activatedResolvedDamage,
                "After bounded in-flight drain, ordinary native rolls plus completed activated impacts must account for every delivered HP point");
            ctx.AssertEqual(_attributedDamage, startingHP - GetCurrentHitPoints(target), "Settled event attribution matches actual target HP");
            ctx.SetResultDetail(JsonSerializer.Serialize(new
            {
                seed, observations = snapshots, movements,
                settled = new { deliveredDamage = _attributedDamage, ordinaryDamage = _ordinaryResolvedDamage,
                    activatedDamage = _activatedResolvedDamage, tailDamageAfterStop = _attributedDamage - deliveredAtStop,
                    drainSeconds = drain.Elapsed.TotalSeconds, activatedDamageByAbility = new Dictionary<string, int>(ActivatedDamageByAbility) },
                method = (opening ? "Representative capstone-first 12-second opening: request capstone at zero seconds, core feats at two/three/four, with zero initial Momentum. " : string.Empty) +
                    "Real native player inputs against a stationary 30000-HP NPC, defense 94/evasion 134, verified flat corridor and equal 69-SP builds. " +
                    "Timed windows are actual delivered HP; ordinary/activated fields are resolved damage and may include projectiles in flight. " +
                    "Final attribution includes an 8-second post-stop drain and must reconcile exactly. No timed teleports, forced hits, incoming pressure, NPC healing, or stamina refill. " +
                    "High Noon remains eligible while target HP stays above 70%; this does not measure PvP or survival."
            }));
        }
        finally
        {
            _actor = OBJECT_INVALID;
            _target = OBJECT_INVALID;
            ObservedImpacts.Clear();
            ActivatedDamageByAbility.Clear();
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            TwinPistolAbility.Cancel(actor);
            UsePerkFeat.ClearQueuedAbility(actor);
            SetLocalInt(actor, Stat.SuppressNaturalRegenVariable, 1);
            if (GetIsObjectValid(target))
            {
                TemporaryStatModifier.Clear(target);
                SetCommandable(true, target);
            }
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
        }
    }

    private static int[] ReadSeeds()
    {
        var configured = Environment.GetEnvironmentVariable(SeedsEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured)) return DefaultSeeds;
        var parts = configured.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 3 || parts.Any(part => !int.TryParse(part, out var value) || value <= 0))
            throw new InvalidOperationException($"{SeedsEnvironmentVariable} must contain one to three positive integer seeds, separated by commas.");
        var seeds = parts.Select(int.Parse).ToArray();
        if (seeds.Distinct().Count() != seeds.Length)
            throw new InvalidOperationException($"{SeedsEnvironmentVariable} cannot repeat a seed.");
        return seeds;
    }

    private static object XYZ(NumericsVector3 position) => new { x = position.X, y = position.Y, z = position.Z };

    private static void SampleCompletedAbilityDamage(uint actor)
    {
        var latest = Ability.GetLastCompletedAbilityImpactSummary(actor);
        if (latest != null) ObservedImpacts.TryAdd(latest, 0);
        // Delayed pulses may continue updating a completed summary. Keep reference identity
        // and count only new damage; do not count a summary again on every polling frame.
        foreach (var (impact, observed) in ObservedImpacts.ToArray())
        {
            var damage = impact.AttributedDamage - observed;
            if (damage <= 0) continue;
            _activatedResolvedDamage += damage;
            var name = impact.Ability?.Name ?? "Unknown activated impact";
            ActivatedDamageByAbility[name] = ActivatedDamageByAbility.GetValueOrDefault(name) + damage;
            ObservedImpacts[impact] = impact.AttributedDamage;
        }
    }

    private static void VerifyCorridor(EngineTestContext ctx, uint actor, uint target, NumericsVector3 center)
    {
        ctx.Assert(float.IsFinite(center.Z), "The open corridor has a finite native ground height");
        // Move the stationary target out of the preflight collision rays, before timing begins.
        ObjectPlugin.SetPosition(target, center + new NumericsVector3(0, 14, 0));
        for (var x = -10; x <= 10; x += 2)
        for (var y = -10; y <= 10; y += 2)
        {
            var position = center + new NumericsVector3(x, y, 0);
            var location = Location(ctx.Arena, position, 0);
            var height = GetGroundHeight(location);
            var material = GetSurfaceMaterial(location);
            ctx.Assert(float.IsFinite(height) && Math.Abs(height - center.Z) <= .25f,
                $"The entire corridor stays on one floor: sample={position}, nativeHeight={height}");
            ctx.Assert(material >= 0 && int.TryParse(Get2DAString("surfacemat", "Walk", material), out var walkable) && walkable == 1,
                $"Every sampled corridor point is walkable: {position}");
        }
        var nativeArea = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(ctx.Arena).AsNWSArea();
        var space = Math.Max(.1f, CreaturePlugin.GetPersonalSpace(actor));
        var heightBudget = Math.Max(.5f, CreaturePlugin.GetHeight(actor));
        for (var offset = -8; offset <= 8; offset += 4)
        {
            ctx.Assert(nativeArea.TestDirectLine(center.X - 10, center.Y + offset,
                center.X + 10, center.Y + offset, space, heightBudget, 0) == 1, "The full horizontal corridor is clear under native collision");
            ctx.Assert(nativeArea.TestDirectLine(center.X + offset, center.Y - 10,
                center.X + offset, center.Y + 10, space, heightBudget, 0) == 1, "The full vertical corridor is clear under native collision");
        }
        ObjectPlugin.SetPosition(target, center);
        var actorPosition = GetPosition(actor);
        var targetPosition = GetPosition(target);
        ctx.Assert(Math.Abs(actorPosition.Z - targetPosition.Z) <= .25f && LineOfSightObject(actor, target),
            "The starting attacker and target share a floor and actual line of fire");
        var retreat = actorPosition + NumericsVector3.Normalize(actorPosition - targetPosition) * 4;
        ctx.AssertEqual(string.Empty, WeaponRepositioning.Validate(actor, GetLocation(actor), Location(ctx.Arena, retreat, 0), 2, 4),
            "The initial Slide has a legal native movement corridor");
        ctx.Log(JsonSerializer.Serialize(new { corridorArea = CorridorAreaResref, verifiedWidthMeters = 20,
            verifiedHeightMeters = 20, center = XYZ(center), actorPosition = XYZ(actorPosition), targetPosition = XYZ(targetPosition) }));
    }

    private static void ConfigureRecord(Player record)
    {
        record.Perks.Clear();
        foreach (var skill in record.Skills.Values) skill.Rank = 0;
        record.Skills[SkillType.Armor].Rank = 50;
        foreach (var stat in record.BaseStats.Keys.ToArray()) record.BaseStats[stat] = 26;
        foreach (var type in record.Defenses.Keys.ToArray()) record.Defenses[type] = 0;
        foreach (var type in record.Resistances.Keys.ToArray()) record.Resistances[type] = 0;
        record.Attack = 0;
        record.Accuracy = 0;
        record.Evasion = 0;
        record.ForceAttack = 0;
        record.MaxStamina = 22;
        record.Stamina = 61;
        record.STMRegen = 5;
    }

    private static void ConfigureWeapon(uint weapon)
    {
        var properties = new List<ItemProperty>();
        for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
            if (GetItemPropertyType(property) is not ItemPropertyType.UnlimitedAmmunition and not ItemPropertyType.OnHitCastSpell)
                properties.Add(property);
        foreach (var property in properties) RemoveItemProperty(weapon, property);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 22), weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, ItemDelayPropertyCost), weapon);
    }

    private static void AssertWeaponTiming(EngineTestContext ctx, uint actor, uint main, uint companion, bool pistol)
    {
        foreach (var weapon in new[] { main, companion }.Where(GetIsObjectValid))
        {
            var propertyCost = 0;
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                if (GetItemPropertyType(property) == ItemPropertyType.Delay)
                    propertyCost += GetItemPropertyCostTableValue(property);
            ctx.AssertEqual(ItemDelayPropertyCost, propertyCost, "Each equipped weapon has native Delay property cost 25");
            ctx.AssertEqual(ItemDelayUnits, propertyCost * 10, "Each equipped weapon has 250 custom delay units after production cost-index conversion");
        }
        var expectedRawDelay = Combat.CalculateAttackDelayMilliseconds(ItemDelayUnits, pistol ? 0 : ItemDelayUnits,
            Combat.CalculateAttackDelayReduction(actor), Combat.CalculateOffhandAttackDelayReduction(actor));
        var actualRawDelay = Combat.CalculateAttackDelay(actor);
        ctx.AssertEqual(expectedRawDelay, actualRawDelay, "Equipped native delay matches the intended weapon and current Haste budgets");
        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(actualRawDelay);
        ctx.AssertEqual(Combat.CalculateEffectiveAttackDelay(expectedRawDelay), effectiveDelay,
            "The engine cycle uses the intended effective attack delay before timing begins");
        ctx.Log($"Verified weapon timing: propertyCost={ItemDelayPropertyCost}, itemDelayUnits={ItemDelayUnits}, " +
                $"rawMilliseconds={actualRawDelay}, effectiveMilliseconds={effectiveDelay}, haste={Combat.CalculateAttackDelayReduction(actor)}, " +
                $"offhandReduction={Combat.CalculateOffhandAttackDelayReduction(actor)}");
    }

    private static Location RushDestination(uint actor, uint target, NumericsVector3 center)
    {
        var origin = GetPosition(actor);
        var toward = GetPosition(target) - origin;
        toward.Z = 0;
        if (toward.LengthSquared() < .01f) return null;
        toward = NumericsVector3.Normalize(toward);
        foreach (var length in new[] { 5.9f, 5f, 4f, 3f })
        foreach (var angle in new[] { .3f, -.3f, .4f, -.4f })
        {
            var direction = new NumericsVector3(toward.X * MathF.Cos(angle) - toward.Y * MathF.Sin(angle),
                toward.X * MathF.Sin(angle) + toward.Y * MathF.Cos(angle), 0);
            var position = origin + direction * length;
            position.Z = GetGroundHeight(Location(GetArea(actor), position, 0));
            if (Math.Abs(position.Z - center.Z) > .25f || Math.Abs(position.X - center.X) > 10 || Math.Abs(position.Y - center.Y) > 10) continue;
            var location = Location(GetArea(actor), position, 0);
            if (TwinPistolAbility.IsInTravelLine(GetPosition(target), origin, position, 4) &&
                string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), location, 2, 6))) return location;
        }
        return null;
    }

}
