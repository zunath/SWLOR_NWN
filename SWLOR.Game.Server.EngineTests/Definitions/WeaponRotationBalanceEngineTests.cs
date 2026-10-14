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
using Activity = SWLOR.Game.Server.Service.Activity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
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
    private static uint _actor = OBJECT_INVALID;
    private static uint _target = OBJECT_INVALID;
    private static int _attributedDamage;
    private static int _ordinaryHits;

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveDamage()
    {
        if (OBJECT_SELF == _target && GetLastDamager(_target) == _actor)
            _attributedDamage += Math.Max(0, GetTotalDamageDealt());
    }

    [NWNEventHandler(ScriptName.OnSWLORDamage)]
    public static void ObserveOrdinaryHits()
    {
        if (OBJECT_SELF == _actor) _ordinaryHits++;
    }

    [EngineTest("Controlled Pistol critical rotation records actual damage and finite player stamina", Category = "WeaponRotationBalance", TimeoutSeconds = 160f)]
    public static Task Pistol(EngineTestContext ctx) => Run(ctx, PerkCategoryType.PistolGunslinger);

    [EngineTest("Controlled Barrage Matched Rhythm rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 160f)]
    public static Task BarrageMatched(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolBarrage, PerkType.MatchedRhythm);

    [EngineTest("Controlled Barrage Paired Handling rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 160f)]
    public static Task BarrageHandling(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolBarrage, PerkType.PairedHandling);

    [EngineTest("Controlled Crossfire Paired Handling rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 160f)]
    public static Task CrossfireHandling(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolCrossfire, PerkType.PairedHandling);

    [EngineTest("Controlled Crossfire Matched Rhythm rotation records damage and finite stamina at 69 SP", Category = "WeaponRotationBalance", TimeoutSeconds = 160f)]
    public static Task CrossfireMatched(EngineTestContext ctx) => Run(ctx, PerkCategoryType.TwinPistolCrossfire, PerkType.MatchedRhythm);

    private static async Task Run(EngineTestContext ctx, PerkCategoryType category, PerkType handling = PerkType.Invalid)
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

        var main = await ctx.EquipItemAsync(actor, "b_pistol", InventorySlot.RightHand);
        var companion = pistol ? OBJECT_INVALID : await ctx.EquipItemAsync(actor, "tp_comp_proto", InventorySlot.LeftHand);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            foreach (var weapon in new[] { main, companion }.Where(GetIsObjectValid))
                ConfigureWeapon(weapon);
            ctx.AssertEqual(pistol ? 36 : 22, WeaponDamage.GetEffectiveDMG(actor, main), "Single Weapon and Doublehand apply once, while paired guns retain their own DMG");
            ctx.AssertEqual(61, Stat.GetMaxStamina(actor), "The real player pool is 10 base +39 Might +12 reference gear");
            ctx.AssertEqual(pistol ? 0 : 30, Combat.CalculateOffhandAttackDelayReduction(actor), "Armor specialization matches the equipped setup");
            AssertWeaponTiming(ctx, actor, main, companion, pistol);
            var origin = GetPosition(actor);
            var placed = false;
            for (var direction = 0; direction < 16 && !placed; direction++)
            {
                var angle = direction * MathF.PI / 8;
                var unit = new NumericsVector3(MathF.Cos(angle), MathF.Sin(angle), 0);
                var retreat = Location(ctx.Arena, origin - unit * 4, 0);
                if (!string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), retreat, 2, 4))) continue;
                var enemy = origin + unit * 2;
                enemy.Z = GetGroundHeight(Location(ctx.Arena, enemy, 0));
                ObjectPlugin.SetPosition(target, enemy);
                placed = LineOfSightObject(actor, target);
            }
            ctx.Assert(placed, "The common starting position has line of fire and a legal retreat lane");
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
        var feats = coreFeats.Append(capstone).ToArray();
        var next = coreFeats.Select((_, index) => (double)(crossfire ? index * 2 : index)).Append(8d).ToArray();
        var casts = feats.ToDictionary(feat => feat, _ => 0);
        var denials = new Dictionary<string, int>();
        var snapshots = new List<object>();
        var naturalTicks = 0;
        var naturalRestored = 0;
        var nextNatural = 6d;
        var nextAttackResumeCheck = 0d;
        var controllerAttackResumes = 0;
        var windows = new Queue<int>(new[] { 6, 12, 120 });
        var startingHP = GetCurrentHitPoints(target);
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);
        _actor = actor;
        _target = target;
        _attributedDamage = 0;
        _ordinaryHits = 0;
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
                ctx.SeedRandom(20261013);
                clock.Start();
                ActionAttack(target);
            });
            while (windows.Count > 0)
            {
                var elapsed = clock.Elapsed.TotalSeconds;
                if (elapsed >= nextNatural && nextNatural <= 120)
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
                    ctx.Assert(!GetIsDead(target) && GetCurrentHitPoints(target) > 0, "The durable NPC remains alive for the complete observation window");
                    ctx.Assert(_attributedDamage > 0, "Real player-attributed damage is present by the first six-second observation");
                    var observation = new
                    {
                        build = category.ToString(), handling = handling.ToString(), investmentSP = price, seed = 20261013,
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
                        var location = feat == FeatType.PistolRush2 ? RushDestination(actor, target) : GetLocation(target);
                        var accepted = location != null && UsePerkFeat.TryUseAbility(actor,
                            feat == FeatType.PistolRush2 ? OBJECT_INVALID : target, feat, location);
                        if (accepted)
                        {
                            casts[feat]++;
                            next[index] = clock.Elapsed.TotalSeconds + ability.RecastDelay(actor) + .1d;
                        }
                        else
                        {
                            var reason = location == null ? "No safe Rush lane" : Ability.GetLastActivationDenialReason();
                            reason = $"{feat}: {reason}";
                            denials[reason] = denials.GetValueOrDefault(reason) + 1;
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
            ctx.AssertEqual(20, naturalTicks, "All twenty controlled six-second production heartbeat budgets ran");
            ctx.AssertEqual(_attributedDamage, startingHP - GetCurrentHitPoints(target), "No bystander damage or target healing contaminated the observation");
            ctx.SetResultDetail(JsonSerializer.Serialize(new
            {
                method = "One fixed seed with native real-player attacks and actual feats against a stationary 30000-HP NPC with measured defense 94 and evasion 134; no forced hits. Equal 69-SP builds own their capstone mastery; paired variants buy exactly one selected handling trait. Capstone enters the fixed rotation at 8s and thereafter follows actual readiness, cooldown and finite stamina. Production NaturalRegeneration runs every 6s with native heartbeat suppressed. The NPC has no regeneration or incoming pressure. Its HP stays above 70%, keeping High Noon's target-health condition eligible. This does not measure player-target PvP or survival. Actions retry when ready; ability and movement interruption remain real. The controller resumes ordinary attacks after casts/movement settle only when no native attack queue to the target remains, and records those inputs.",
                observations = snapshots
            }));
        }
        finally
        {
            _actor = OBJECT_INVALID;
            _target = OBJECT_INVALID;
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

    private static Location RushDestination(uint actor, uint target)
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
            var location = Location(GetArea(actor), position, 0);
            if (TwinPistolAbility.IsInTravelLine(GetPosition(target), origin, position, 4) &&
                string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), location, 2, 6))) return location;
        }
        return null;
    }

}
