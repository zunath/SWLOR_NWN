using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PowerGlovePairEngineTests
{
    private static uint _observedAttacker = OBJECT_INVALID;
    private static readonly List<(uint Weapon, DateTime Time)> Hits = new();

    [EngineTest("One owned Power Glove and its cosmetic hand retain authored strike and capstone playback", Category = "PowerGloveAnimationRoute", TimeoutSeconds = 35f)]
    public static async Task PairedHandsRetainAuthoredAnimations(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var actor = player.Creature;
        var main = await EquipMain(ctx, actor);
        ctx.Track(main);
        var companion = await WaitForPair(ctx, actor, main);
        ctx.AssertEqual(BaseItem.PowerGlove, GetBaseItemType(main), "The equipped main hand is the real native Power Glove");
        AssertCosmeticCompanion(ctx, companion);
        ctx.Assert(AbilityAnimationEquipment.IsCompatible(AnimationEquipmentRequirement.Unarmed, actor),
            "Both native base541 and cosmetic base543 resolve as an Unarmed grip");
        foreach (var feat in new[] { FeatType.Jab1, FeatType.PerfectForm1, FeatType.Haymaker1 })
        {
            var ability = Service.Ability.GetAbilityDetail(feat);
            var clip = AbilityAnimationBinding.ImpactClip(ability, actor);
            ctx.Assert(clip != null && ReferenceEquals(ability.AuthoredImpactAnimation, clip),
                $"The actual equipped pair retains {feat}'s authored impact clip");
            string token = null;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                token = NamedAnimation.Play(actor, clip, equipmentRequirement: ability.AnimationEquipmentRequirement));
            ctx.Assert(!string.IsNullOrEmpty(token), $"The live equipment check permits {feat}'s named playback");
            await ctx.DelaySecondsAsync(clip.Duration + NamedAnimationPlayback.ExitGraceSeconds + .2f);
            ctx.Assert(!NamedAnimation.StopIfCurrent(actor, token), $"{feat}'s playback releases its ownership after the authored exit");
            ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Playback preserves the original real glove");
            ctx.AssertEqual(companion, GetItemInSlot(InventorySlot.LeftHand, actor), "Playback preserves the same generated hand");
            ctx.Log($"{feat}: live base541/543 route selected {clip.Name}, duration {clip.Duration}s, then released playback.");
        }
    }

    [EngineTest("One owned Power Glove generates a cosmetic hand with no duplicate stats or economy item", Category = "PowerGlovePair", TimeoutSeconds = 45f)]
    public static async Task OneOwnedItemAndOneStatProfile(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var actor = player.Creature;
        var main = await CreateCarriedItem(ctx, actor, "t_pglove");
        var standardOnHitProperty = string.Empty;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, 7), main);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Stamina, -1, 9), main);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 13), main);
            standardOnHitProperty = PropertySignature(ItemPropertyOnHitCastSpell(OnHitCastSpellType.ONHIT_UNIQUEPOWER, 40));
        });
        var baseline = DB.Get<Player>(player.Id);
        var hp = baseline.MaxHP;
        var stamina = baseline.MaxStamina;
        var originalProperties = Properties(main);
        ctx.Assert(!originalProperties.Split(';').Contains(standardOnHitProperty),
            "The fixture starts with authored upgrades before normal equip adds its on-hit transport");
        var equippedProperties = string.Join(";", originalProperties.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Append(standardOnHitProperty).OrderBy(property => property));
        var identity = GetObjectUUID(main);
        for (var cycle = 0; cycle < 2; cycle++)
        {
            await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            var companion = await WaitForPair(ctx, actor, main);
            AssertCosmeticCompanion(ctx, companion);
            AssertOwnedItems(ctx, actor, main, 1);
            ctx.Assert(!Item.IsEconomyRestricted(main), "The real glove remains the one usable economy item");
            ctx.AssertEqual(equippedProperties, Properties(main), "Equip preserves every real upgrade and adds exactly one standard on-hit transport property");
            var equipped = DB.Get<Player>(player.Id);
            ctx.AssertEqual(hp + 7, equipped.MaxHP, "The real glove's HP property contributes exactly once");
            ctx.AssertEqual(stamina + 9, equipped.MaxStamina, "The real glove's stamina property contributes exactly once");
            ctx.Assert(EquipmentPredicates.HasDualWield(actor), "The owned glove strikes with both hands through Dual Wield");
            ctx.Assert(EquipmentPredicates.HasUnarmedGrip(actor), "The generated hand retains Unarmed technique eligibility");
            for (var pass = 0; pass < 3; pass++)
                await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.EnsurePair(actor));
            ctx.AssertEqual(companion, GetItemInSlot(InventorySlot.LeftHand, actor), "Repeated reconciliation retains one companion");
            AssertOwnedItems(ctx, actor, main, 1);
            if (cycle == 0)
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.ReleaseCompanions(actor));
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(companion), 6f, "logout cleanup to destroy only the cosmetic hand");
                ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Logout cleanup preserves the equipped real glove");
                ctx.AssertEqual(hp + 7, DB.Get<Player>(player.Id).MaxHP, "Cosmetic logout cleanup does not subtract real stats");
                AssertOwnedItems(ctx, actor, main, 0);
                await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.RestorePair(actor));
                companion = await WaitForPair(ctx, actor, main);
                AssertCosmeticCompanion(ctx, companion);
                ctx.AssertEqual(hp + 7, DB.Get<Player>(player.Id).MaxHP, "Rebuilding the cosmetic hand does not add real stats again");
                ctx.AssertEqual(stamina + 9, DB.Get<Player>(player.Id).MaxStamina, "Rebuilding retains exactly one stamina contribution");
            }
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(cycle == 0 ? main : companion));
            await WaitForRemovedPair(ctx, actor, main, companion);
            var removed = DB.Get<Player>(player.Id);
            ctx.AssertEqual(hp, removed.MaxHP, "Either-hand removal subtracts the real HP property once");
            ctx.AssertEqual(stamina, removed.MaxStamina, "Either-hand removal subtracts the real stamina property once");
            ctx.AssertEqual(identity, GetObjectUUID(main), "Either-hand removal retains the same real item identity");
            ctx.AssertEqual(equippedProperties, Properties(main), "Either-hand removal preserves enhancements and the one standard on-hit transport property");
            AssertOwnedItems(ctx, actor, main, 0);
        }
    }

    [EngineTest("Power Glove replacement and transfer attempts preserve one owned pair and never retain a spare", Category = "PowerGlovePair", TimeoutSeconds = 55f)]
    public static async Task ReplacementsAndCompanionEscape(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => { foreach (var skill in record.Skills.Values) skill.Rank = 50; });
        var actor = player.Creature;
        var main = await PowerGlovePairEngineTests.EquipMain(ctx, actor);
        var companion = await WaitForPair(ctx, actor, main);
        var replacement = await CreateCarriedItem(ctx, actor, "b_pglove");
        var sword = await CreateCarriedItem(ctx, actor, "b_longsword");
        foreach (var (item, slot) in new[]
        {
            (replacement, InventorySlot.RightHand), (replacement, InventorySlot.LeftHand), (sword, InventorySlot.LeftHand)
        })
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionEquipItem(item, slot));
            await ctx.DelaySecondsAsync(.5f);
            ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Rejected replacement preserves the real main glove");
            ctx.AssertEqual(companion, GetItemInSlot(InventorySlot.LeftHand, actor), "Rejected replacement preserves the cosmetic hand");
            ctx.Assert(PowerGloveEquipment.IsLinkedPair(actor, main, companion), "Rejected replacement retains the exact linked pair");
        }
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(companion));
        await WaitForRemovedPair(ctx, actor, main, companion);
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionEquipItem(replacement, InventorySlot.LeftHand));
        await ctx.DelaySecondsAsync(.5f);
        ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, actor), "A real second glove cannot equip independently in the offhand");
        ctx.AssertEqual(actor, GetItemPossessor(replacement), "Rejected independent equip preserves the purchased item");
        foreach (var real in new[] { replacement, main })
        {
            await EquipExisting(ctx, actor, real, InventorySlot.RightHand);
            companion = await WaitForPair(ctx, actor, real);
            ctx.AssertEqual(1, OwnedItems(actor).Count(PowerGloveEquipment.IsGeneratedCompanion), "A replacement creates only one cosmetic companion");
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(real));
            await WaitForRemovedPair(ctx, actor, real, companion);
        }
        ctx.AssertEqual(2, OwnedItems(actor).Count(PowerGloveEquipment.IsPairItem), "Swaps preserve exactly the two acquired real gloves");
        await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
        companion = await WaitForPair(ctx, actor, main);
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionPutDownItem(companion));
        await ctx.DelaySecondsAsync(.5f);
        ctx.Assert(!GetIsObjectValid(companion) || GetItemPossessor(companion) == actor, "Drop cannot leave a collectible cosmetic glove");
        if (!GetIsObjectValid(companion))
        {
            await WaitForRemovedPair(ctx, actor, main, companion);
            await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            companion = await WaitForPair(ctx, actor, main);
        }
        var recipient = ctx.SpawnCreature("civilian", 5f);
        await ctx.ExecuteInCreatureContextAsync(actor, () => ItemPlugin.MoveTo(companion, recipient, true));
        await ctx.WaitUntilAsync(() => !GetIsObjectValid(companion), 6f, "a transferred cosmetic glove to be destroyed");
        await WaitForRemovedPair(ctx, actor, main, companion);
        ctx.Assert(!OwnedItems(recipient).Any(PowerGloveEquipment.IsGeneratedCompanion), "The recipient retains no generated glove");
        ctx.AssertEqual(2, OwnedItems(actor).Count(PowerGloveEquipment.IsPairItem), "Escape cleanup preserves the two real gloves");
    }

    [EngineTest("Power Gloves receive no Single Weapon or Doublehand bonus and both hands roll the owned glove's DMG", Category = "PowerGlovePair", TimeoutSeconds = 35f)]
    public static async Task PairedHandsUseOwnedGloveWithoutSingleWeaponBudget(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var actor = player.Creature;
        var main = await PowerGlovePairEngineTests.EquipMain(ctx, actor);
        var companion = await WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", 1f);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(main); GetIsItemPropertyValid(property); property = GetNextItemProperty(main))
                if (GetItemPropertyType(property) == ItemPropertyType.DMG) properties.Add(property);
            foreach (var property in properties) RemoveItemProperty(main, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 24), main);
        });
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ctx.AssertEqual(24, Item.GetDMG(main), "The only real item owns the raw DMG rating");
            ctx.Assert(!EquipmentPredicates.HasSingleWeapon(actor), "A Power Glove pair is not a single weapon");
            ctx.AssertEqual(main, EquipmentPredicates.GetOffhandAttackWeapon(actor), "The owned glove supplies the off-hand roll");
            player.Update(record => record.Perks[PerkType.Doublehand] = 0);
            ctx.AssertEqual(24, WeaponDamage.GetEffectiveDMG(actor, main), "No natural Single Weapon bonus applies");
            var mainHand = Sample(0);
            player.Update(record => record.Perks[PerkType.Doublehand] = 3);
            ctx.AssertEqual(24, WeaponDamage.GetEffectiveDMG(actor, main), "Doublehand III does not apply to Power Gloves");
            ctx.AssertEqual(mainHand, Sample(0), "Native main-hand damage ignores Doublehand");
            var offHand = Sample(1);
            ctx.Assert(mainHand > 0, "The owned glove deals native ordinary damage");
            ctx.AssertEqual(mainHand, offHand, "The native off-hand roll uses the owned glove, never the cosmetic carrier");
            ctx.AssertEqual(48, WeaponDamage.GetAbilityDMG(actor, main, SkillType.Unarmed), "Unarmed abilities count both gloves' DMG");
            ctx.AssertEqual(48, Combat.GetCombatImpactWeaponDamage(actor, SkillType.Unarmed, abilityImpact: true), "Ability impacts add both gloves' DMG");
            ctx.AssertEqual(24, Combat.GetCombatImpactWeaponDamage(actor, SkillType.Unarmed), "Non-ability uses keep one glove's DMG");
            ctx.AssertEqual(24, WeaponDamage.GetAbilityDMG(actor, main, SkillType.Katar), "Only Unarmed abilities count the pair");
            ctx.AssertEqual(24, Item.GetDMG(main), "Damage resolution never rewrites the real item's raw rating");
            AssertCosmeticCompanion(ctx, companion);
            ctx.Log($"256 matched-seed native rolls per hand: main {mainHand:0.00}, off hand {offHand:0.00}; raw DMG 24 with no Single Weapon bonus.");
        });

        double Sample(int offHand)
        {
            var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
            var defender = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
            native.m_pcCombatRound.StartCombatRound(target);
            ctx.SeedRandom(12345);
            return Enumerable.Range(0, 256).Average(_ => native.m_pStats.GetDamageRoll(defender, offHand, 0, 0, 0, 0));
        }
    }

    [EngineTest("Power Glove pair resolves a main-hand and an off-hand roll each cycle, both from the owned glove", Category = "PowerGlovePair", TimeoutSeconds = 35f)]
    public static async Task PairedHandsAttackEachCycle(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian", -.5f);
        var target = ctx.SpawnCreature("nw_rat001", 1f);
        var main = await PowerGlovePairEngineTests.EquipMain(ctx, actor);
        var companion = await WaitForPair(ctx, actor, main);
        var effectiveDelay = 0;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
                ctx.MakeHostile(target);
                AssertCosmeticCompanion(ctx, companion);
                ctx.Assert(EquipmentPredicates.HasDualWield(actor), "The owned glove schedules an off-hand roll");
                var gloveDelay = (int)WeaponDelay.GetWeaponDelay(BaseItem.PowerGlove) * 10;
                ctx.AssertEqual(Combat.CalculateAttackDelayMilliseconds(gloveDelay, gloveDelay, 0, 0), Combat.CalculateAttackDelay(actor),
                    "The pair uses the paired delay of two gloves, like two katars");
                effectiveDelay = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(actor));
                ctx.Assert(effectiveDelay > 1000, "The fixture has separately observable attack cycles");
            });
            _observedAttacker = actor;
            Hits.Clear();
            Combat.SetAutoAttackHitResolutionOverride(true);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => Hits.Count > 0, 6f, "the first main-hand roll");
            var round = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcCombatRound;
            ctx.AssertEqual(1, round.m_nOffHandAttacks, "The native combat round allocates one off-hand roll to the pair");
            await ctx.WaitUntilAsync(() => Hits.Count >= 4, 20f, "two full cycles of paired rolls");
            var observed = Hits.ToArray();
            ctx.Assert(observed.All(hit => hit.Weapon == main), "Every damage event comes from the owned glove, never the companion");
            for (var index = 1; index < 4; index += 2)
                ctx.Assert((observed[index].Time - observed[index - 1].Time).TotalMilliseconds < effectiveDelay - 100,
                    "The off-hand roll lands within its main hand's cycle");
            ctx.Assert((observed[2].Time - observed[0].Time).TotalMilliseconds >= effectiveDelay - 100,
                "The next main-hand roll waits for the full paired cycle");
            ctx.Log($"Observed {observed.Length} damage events from real item {main:X8}, none from cosmetic {companion:X8}; paired cycle {effectiveDelay}ms.");
        }
        finally
        {
            AssignCommand(actor, () => ClearAllActions());
            WeaponAttackCycle.CancelPendingHand(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            _observedAttacker = OBJECT_INVALID;
            Hits.Clear();
        }
    }

    [EngineTest("Power Glove creation and either-hand removal use bag space when the main inventory is full", Category = "PowerGlovePair", TimeoutSeconds = 50f)]
    public static async Task FullInventoryWithBag(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var actor = player.Creature;
        var main = await PowerGlovePairEngineTests.EquipMain(ctx, actor);
        var companion = await WaitForPair(ctx, actor, main);
        var identity = GetObjectUUID(main);
        var properties = Properties(main);
        var equippedHP = DB.Get<Player>(player.Id).MaxHP;
        var escrow = ctx.SpawnCreature("civilian", 6f);
        SetCommandable(false, escrow);
        var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcItemRepository;
        var width = repository.m_nWidth;
        var height = repository.m_nHeight;
        var boundary = repository.m_nBoundary;
        var scalable = repository.m_bScalable;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                foreach (var item in OwnedItems(actor).Where(item =>
                             GetItemPossessor(item, true) == actor && Item.GetItemSlot(actor, item) == InventorySlot.Invalid).ToArray())
                    ctx.Assert(ItemPlugin.MoveTo(item, escrow, true), "Setup preserves carried objects in test-owned storage");
            });
            var bag = await CreateCarriedItem(ctx, actor, "bag_b");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                repository.m_nWidth = 2;
                repository.m_nHeight = 2;
                repository.m_nBoundary = 4;
                repository.m_bScalable = 0;
                var probe = CreateObject(ObjectType.Item, "sardine_ball", GetLocation(actor));
                ctx.Track(probe);
                ctx.Assert(!ItemPlugin.MoveTo(probe, actor, true), "The bag fills every top-level cell, including space for the one-cell companion");
                DestroyObject(probe);
                PowerGloveEquipment.ReleaseCompanions(actor);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(companion), 6f, "the old cosmetic hand to be released before bag-only reconstruction");
            await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.EnsurePair(actor));
            companion = await WaitForPair(ctx, actor, main);
            ctx.AssertEqual(equippedHP, DB.Get<Player>(player.Id).MaxHP, "Bag-only cosmetic reconstruction does not add real stats again");
            foreach (var removeMain in new[] { false, true })
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(removeMain ? main : companion));
                await WaitForRemovedPair(ctx, actor, main, companion);
                ctx.AssertEqual(bag, GetItemPossessor(main, true), "Removing either hand puts the real glove in the existing owned bag");
                ctx.AssertEqual(identity, GetObjectUUID(main), "Bag recovery retains the exact real item identity");
                ctx.AssertEqual(properties, Properties(main), "Bag recovery preserves all real item properties");
                AssertOwnedItems(ctx, actor, main, 0);
                if (removeMain) continue;
                await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
                companion = await WaitForPair(ctx, actor, main);
                ctx.AssertEqual(equippedHP, DB.Get<Player>(player.Id).MaxHP, "Re-equipping from the bag applies the same one-item stat profile");
            }
        }
        finally
        {
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
            SetCommandable(true, escrow);
        }
    }

    [EngineTest("Power Glove failed companion creation with full storage retains the real item and blocks ordinary attacks", Category = "PowerGlovePair", TimeoutSeconds = 60f)]
    public static async Task FailedCreationWithFullStorageCannotAttack(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var actor = player.Creature;
        var main = await CreateCarriedItem(ctx, actor, "t_pglove");
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, 7), main);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Stamina, -1, 9), main);
        });
        var baseline = DB.Get<Player>(player.Id);
        var baselineHP = baseline.MaxHP;
        var baselineStamina = baseline.MaxStamina;
        await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
        var companion = await WaitForPair(ctx, actor, main);
        var identity = GetObjectUUID(main);
        var properties = Properties(main);
        var target = ctx.SpawnCreature("nw_rat001", 1f);
        var escrow = ctx.SpawnCreature("civilian", 6f);
        SetCommandable(false, escrow);
        var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcItemRepository;
        var width = repository.m_nWidth;
        var height = repository.m_nHeight;
        var boundary = repository.m_nBoundary;
        var scalable = repository.m_bScalable;
        var filler = OBJECT_INVALID;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
                ctx.MakeHostile(target);
            });
            _observedAttacker = actor;
            Hits.Clear();
            Combat.SetAutoAttackHitResolutionOverride(true);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => Hits.Count > 0, 8f, "a real ordinary attack before forcing the pairing failure");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                WeaponAttackCycle.CancelPendingHand(actor);
                foreach (var item in OwnedItems(actor).Where(item =>
                             GetItemPossessor(item, true) == actor && Item.GetItemSlot(actor, item) == InventorySlot.Invalid).ToArray())
                    ctx.Assert(ItemPlugin.MoveTo(item, escrow, true), "Setup preserves all carried objects outside the packed inventory");
                filler = CreateItemOnObject("sardine_ball", actor);
                ctx.Track(filler);
                ctx.Assert(GetIsObjectValid(filler) && GetItemPossessor(filler, true) == actor, "One real item fills the sole inventory cell");
                repository.m_nWidth = 1;
                repository.m_nHeight = 1;
                repository.m_nBoundary = 1;
                repository.m_bScalable = 0;
                PowerGloveEquipment.ReleaseCompanions(actor);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(companion), 6f, "the original cosmetic hand to be destroyed");
            foreach (var storageMissing in new[] { false, true })
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
                    ctx.Assert(GetIsObjectValid(storage), "The normal temporary storage exists before injecting creation failure");
                    try
                    {
                        if (storageMissing)
                        {
                            SetTag(storage, "PG_TEST_STORAGE");
                            ctx.Assert(!GetIsObjectValid(GetObjectByTag("TEMP_ITEM_STORAGE")), "The failure fixture makes companion creation unavailable synchronously");
                        }
                        PowerGloveEquipment.EnsurePair(actor);
                    }
                    finally
                    {
                        if (storageMissing) SetTag(storage, "TEMP_ITEM_STORAGE");
                    }
                });
                await ctx.WaitFrameAsync();
                await ctx.WaitUntilAsync(() => !OwnedItems(GetObjectByTag("TEMP_ITEM_STORAGE")).Any(item =>
                        PowerGloveEquipment.IsGeneratedCompanion(item) && GetLocalObject(item, PowerGloveEquipment.OwnerVariable) == actor),
                    3f, "failed companion creation to dispose of every owner-linked cosmetic in temporary storage");
                ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Failed safe return retains the exact real item in its equipped slot");
                ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, actor), "Failed creation leaves no cosmetic equipped object");
                ctx.Assert(!PowerGloveEquipment.IsActivePair(actor) && !EquipmentPredicates.HasUnarmedGrip(actor), "An incomplete pair cannot supply Unarmed techniques");
                ctx.AssertEqual(identity, GetObjectUUID(main), "Repeated failure preserves the real item UUID");
                ctx.AssertEqual(properties, Properties(main), "Repeated failure preserves every real enhancement");
                ctx.AssertEqual(actor, GetItemPossessor(filler), "Failed recovery preserves the original full inventory contents");
                ctx.AssertEqual(baselineHP + 7, DB.Get<Player>(player.Id).MaxHP, "The still-equipped real item retains exactly one HP contribution");
                ctx.AssertEqual(baselineStamina + 9, DB.Get<Player>(player.Id).MaxStamina, "The still-equipped real item retains exactly one stamina contribution");
                AssertOwnedItems(ctx, actor, main, 0);
            }
            Hits.Clear();
            var targetHP = GetCurrentHitPoints(target);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.DelaySecondsAsync(6f);
            ctx.AssertEqual(0, Hits.Count, "An actual ordinary attack command emits no damage events while its required companion is missing");
            ctx.AssertEqual(targetHP, GetCurrentHitPoints(target), "The incomplete pair deals no ordinary native damage");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                WeaponAttackCycle.CancelPendingHand(actor);
                repository.m_nWidth = width;
                repository.m_nHeight = height;
                repository.m_nBoundary = boundary;
                repository.m_bScalable = scalable;
                ctx.Assert(ItemPlugin.MoveTo(filler, escrow, true), "Restoring space preserves the original filler");
                PowerGloveEquipment.EnsurePair(actor);
            });
            companion = await WaitForPair(ctx, actor, main);
            AssertOwnedItems(ctx, actor, main, 1);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => Hits.Count > 0 && GetCurrentHitPoints(target) < targetHP, 8f,
                "ordinary damage to resume only after the same real item regains its cosmetic hand");
            ctx.Assert(Hits.All(hit => hit.Weapon == main), "Recovery attacks still use only the original real glove");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                WeaponAttackCycle.CancelPendingHand(actor);
                ActionUnequipItem(companion);
            });
            await WaitForRemovedPair(ctx, actor, main, companion);
            ctx.AssertEqual(baselineHP, DB.Get<Player>(player.Id).MaxHP, "Successful final removal subtracts the real HP contribution once");
            ctx.AssertEqual(baselineStamina, DB.Get<Player>(player.Id).MaxStamina, "Successful final removal subtracts the real stamina contribution once");
            ctx.AssertEqual(identity, GetObjectUUID(main), "Failure and recovery never replace or duplicate the owned item");
            AssertOwnedItems(ctx, actor, main, 0);
        }
        finally
        {
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
            SetCommandable(true, escrow);
            AssignCommand(actor, () => ClearAllActions());
            WeaponAttackCycle.CancelPendingHand(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            _observedAttacker = OBJECT_INVALID;
            Hits.Clear();
        }
    }

    [EngineTest("NPC Power Glove removal preserves packed inventory and uses an owned bag for either hand", Category = "PowerGlovePair", TimeoutSeconds = 45f)]
    public static async Task NpcFullInventoryAndBagRemoval(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        var main = await EquipMain(ctx, actor);
        ctx.Track(main);
        var companion = await WaitForPair(ctx, actor, main);
        var identity = GetObjectUUID(main);
        var properties = Properties(main);
        var escrow = ctx.SpawnCreature("civilian", 6f);
        SetCommandable(false, escrow);
        var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_pcItemRepository;
        var width = repository.m_nWidth;
        var height = repository.m_nHeight;
        var boundary = repository.m_nBoundary;
        var scalable = repository.m_bScalable;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(!GetIsPC(actor), "The storage regression exercises a real NPC owner");
                foreach (var item in OwnedItems(actor).Where(item =>
                             GetItemPossessor(item, true) == actor && Item.GetItemSlot(actor, item) == InventorySlot.Invalid).ToArray())
                    ctx.Assert(ItemPlugin.MoveTo(item, escrow, true), "Setup retains carried objects in test-owned storage");
            });
            var filler = await CreateCarriedItem(ctx, actor, "sardine_ball");
            ctx.Track(filler);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                repository.m_nWidth = 1;
                repository.m_nHeight = 1;
                repository.m_nBoundary = 1;
                repository.m_bScalable = 0;
                foreach (var hand in new[] { main, companion })
                {
                    CreaturePlugin.RunUnequip(actor, hand);
                    ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "No-space removal leaves the real item equipped");
                    ctx.AssertEqual(companion, GetItemInSlot(InventorySlot.LeftHand, actor), "No-space removal retains the linked cosmetic hand");
                    ctx.Assert(PowerGloveEquipment.IsActivePair(actor), "Rejected NPC removal leaves the existing pair usable");
                    ctx.AssertEqual(actor, GetItemPossessor(main), "The real NPC glove never drops or transfers before validation");
                    ctx.AssertEqual(actor, GetItemPossessor(filler, true), "Rejected removal preserves packed inventory contents");
                    ctx.AssertEqual(identity, GetObjectUUID(main), "Rejected NPC removal retains the original item UUID");
                    ctx.AssertEqual(properties, Properties(main), "Rejected NPC removal retains all real upgrades");
                    AssertOwnedItems(ctx, actor, main, 1);
                }
                repository.m_nWidth = 2;
                repository.m_nHeight = 2;
                repository.m_nBoundary = 4;
                ctx.Assert(ItemPlugin.MoveTo(filler, escrow, true), "Opening space retains the packed filler in owned storage");
            });
            var bag = await CreateCarriedItem(ctx, actor, "bag_b");
            ctx.Track(bag);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                var probe = CreateObject(ObjectType.Item, "sardine_ball", GetLocation(actor));
                ctx.Track(probe);
                ctx.Assert(!ItemPlugin.MoveTo(probe, actor, true), "The existing bag fills all top-level NPC inventory cells");
                DestroyObject(probe);
            });
            foreach (var removeMain in new[] { false, true })
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(removeMain ? main : companion));
                await WaitForRemovedPair(ctx, actor, main, companion);
                ctx.AssertEqual(bag, GetItemPossessor(main, true), "Either-hand NPC removal reserves the existing owned bag");
                ctx.AssertEqual(actor, GetItemPossessor(bag), "The recovery destination remains owned by the same NPC");
                ctx.AssertEqual(identity, GetObjectUUID(main), "Bag recovery never replaces the real NPC glove");
                ctx.AssertEqual(properties, Properties(main), "Bag recovery preserves the real weapon properties");
                ctx.AssertEqual(escrow, GetItemPossessor(filler), "Bag recovery retains the original packed filler");
                AssertOwnedItems(ctx, actor, main, 0);
                if (removeMain) continue;
                await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
                companion = await WaitForPair(ctx, actor, main);
            }
        }
        finally
        {
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
            SetCommandable(true, escrow);
        }
    }

    [EngineTest("Destroyed NPC Power Glove owners release cached pairs and orphan companions", Category = "PowerGlovePair", TimeoutSeconds = 45f)]
    public static async Task DestroyedNpcOwnerReleasesPairState(EngineTestContext ctx)
    {
        var pairs = (Dictionary<uint, (uint Main, uint Companion)>)typeof(PowerGloveEquipment)
            .GetField("Pairs", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var keeper = ctx.SpawnCreature("civilian", 4f);
        var keeperMain = await EquipMain(ctx, keeper);
        ctx.Track(keeperMain);
        var keeperCompanion = await WaitForPair(ctx, keeper, keeperMain);
        for (var cycle = 0; cycle < 2; cycle++)
        {
            var actor = ctx.SpawnCreature("civilian");
            var main = await EquipMain(ctx, actor);
            ctx.Track(main);
            var companion = await WaitForPair(ctx, actor, main);
            ctx.Assert(pairs.TryGetValue(actor, out var pair) && pair == (main, companion), "A native NPC equip establishes its exact cached pair");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                DestroyObject(actor);
                ctx.Assert(!pairs.ContainsKey(actor), "The actual destruction event clears the owner ID synchronously");
                if (GetIsObjectValid(main))
                    ctx.AssertEqual(OBJECT_INVALID, GetLocalObject(main, PowerGloveEquipment.CompanionVariable), "Destroyed-owner cleanup releases the real glove's cosmetic link");
                ctx.AssertEqual((keeperMain, keeperCompanion), pairs[keeper], "Destroying one owner never clears another owner's pair");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(actor) && !GetIsObjectValid(companion), 6f,
                "the destroyed NPC and its generated companion to leave the engine");
            ctx.Assert(!pairs.ContainsKey(actor), "No destroyed-owner cache entry survives native destruction");
            ctx.Assert(PowerGloveEquipment.IsActivePair(keeper), "An unrelated live owner keeps its original active pair");
            ctx.AssertEqual(keeperMain, GetItemInSlot(InventorySlot.RightHand, keeper), "Cleanup preserves the other real weapon identity");
            ctx.AssertEqual(keeperCompanion, GetItemInSlot(InventorySlot.LeftHand, keeper), "Cleanup preserves the other cosmetic identity");
            ctx.Log($"Destroyed NPC cycle {cycle + 1}: owner {actor:X8} cache removed and companion {companion:X8} released.");
        }
    }

    [NWNEventHandler(ScriptName.OnSWLORDamage)]
    public static void ObserveDamage()
    {
        if (OBJECT_SELF == _observedAttacker)
            Hits.Add((StringToObject(EventsPlugin.GetEventData("WEAPON")), DateTime.UtcNow));
    }

    internal static async Task<uint> WaitForPair(EngineTestContext ctx, uint actor, uint main)
    {
        try
        {
            await ctx.WaitUntilAsync(() => PowerGloveEquipment.IsActivePair(actor), 6f,
                "the one real Power Glove to create its cosmetic companion through equipment events");
        }
        catch
        {
            await LogEquipFailure(ctx, actor);
            throw;
        }
        var companion = GetItemInSlot(InventorySlot.LeftHand, actor);
        ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "The originally owned glove remains in the main hand");
        ctx.Assert(PowerGloveEquipment.IsLinkedPair(actor, main, companion), "The cosmetic hand is linked to this exact main item and owner");
        return companion;
    }

    internal static async Task<uint> EquipMain(EngineTestContext ctx, uint actor)
    {
        try { return await ctx.EquipItemAsync(actor, "t_pglove", InventorySlot.RightHand); }
        catch
        {
            await LogEquipFailure(ctx, actor);
            throw;
        }
    }

    private static async Task LogEquipFailure(EngineTestContext ctx, uint actor)
    {
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            var main = GetItemInSlot(InventorySlot.RightHand, actor);
            var off = GetItemInSlot(InventorySlot.LeftHand, actor);
            ctx.Log($"Power Glove equip failure: actor={actor:X8}, PC={GetIsPC(actor)}, right={main:X8}, left={off:X8}, changing={PowerGloveEquipment.IsChanging(actor)}, active={PowerGloveEquipment.IsActivePair(actor)}, storage={GetObjectByTag("TEMP_ITEM_STORAGE"):X8}.");
            foreach (var column in new[] { "EquipableSlots", "WeaponWield", "WeaponType", "NumDice", "DieToRoll", "ReqFeat0" })
                ctx.Log($"Native base543 {column}={Get2DAString("baseitems", column, (int)BaseItem.CompanionPowerGlove)}.");
            foreach (var item in OwnedItems(actor).Where(item => PowerGloveEquipment.IsPairItem(item) || PowerGloveEquipment.IsGeneratedCompanion(item)))
                ctx.Log($"Owned glove={item:X8}, base={(int)GetBaseItemType(item)}, directOwner={GetItemPossessor(item, true):X8}, owner={GetItemPossessor(item):X8}, slot={Item.GetItemSlot(actor, item)}, companion={GetLocalObject(item, PowerGloveEquipment.CompanionVariable):X8}, main={GetLocalObject(item, PowerGloveEquipment.MainWeaponVariable):X8}, linkedOwner={GetLocalObject(item, PowerGloveEquipment.OwnerVariable):X8}, useError={Item.GetCreatureItemUseError(actor, item)}, properties={Properties(item)}.");
        });
    }

    internal static async Task WaitForRemovedPair(EngineTestContext ctx, uint actor, uint main, uint companion)
    {
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, actor) != main && !GetIsObjectValid(companion),
            6f, "the real glove to return to inventory and its cosmetic companion to be destroyed");
        ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, actor), "Whole-pair removal empties the cosmetic hand");
        ctx.AssertEqual(actor, GetItemPossessor(main), "The original real glove remains owned");
        ctx.AssertEqual(1, GetItemStackSize(main), "The real glove remains one item");
        ctx.Assert(!PowerGloveEquipment.IsActivePair(actor), "Inventory gloves have no active pair");
    }

    internal static async Task<uint> CreateCarriedItem(EngineTestContext ctx, uint actor, string resref)
    {
        var item = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(actor, () => item = CreateItemOnObject(resref, actor));
        await ctx.WaitUntilAsync(() => GetIsObjectValid(item) && GetItemPossessor(item, true) == actor, 5f, $"owned {resref} to enter inventory");
        await ctx.WaitFrameAsync();
        return item;
    }

    internal static async Task EquipExisting(EngineTestContext ctx, uint actor, uint item, InventorySlot slot)
    {
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            ActionEquipItem(item, slot);
        });
        try
        {
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, actor) == item, 6f, $"real item {item:X8} to equip in {slot}");
        }
        catch
        {
            await LogEquipFailure(ctx, actor);
            throw;
        }
    }

    internal static HashSet<uint> OwnedItems(uint actor)
    {
        var items = new HashSet<uint>();
        AddContents(actor);
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            AddItem(GetItemInSlot((InventorySlot)slot, actor));
        return items;

        void AddContents(uint owner)
        {
            for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                AddItem(item);
        }
        void AddItem(uint item)
        {
            if (!GetIsObjectValid(item) || !items.Add(item)) return;
            if (GetHasInventory(item)) AddContents(item);
        }
    }

    internal static void AssertOwnedItems(EngineTestContext ctx, uint actor, uint main, int companions)
    {
        var items = OwnedItems(actor);
        ctx.AssertEqual(1, items.Count(PowerGloveEquipment.IsPairItem), "Exactly one purchased or crafted Power Glove is owned");
        ctx.AssertEqual(companions, items.Count(PowerGloveEquipment.IsGeneratedCompanion), "Only the equipped cosmetic companion may exist");
        ctx.Assert(items.Contains(main), "The exact real item remains in owned equipment or storage");
        ctx.AssertEqual(actor, GetItemPossessor(main), "The real item retains its owner");
        ctx.AssertEqual(1, GetItemStackSize(main), "The real item is never duplicated into a stack");
    }

    internal static void AssertCosmeticCompanion(EngineTestContext ctx, uint companion)
    {
        ctx.Assert(PowerGloveEquipment.IsGeneratedCompanion(companion), "The other hand is generated");
        ctx.AssertEqual(BaseItem.CompanionPowerGlove, GetBaseItemType(companion), "The other hand uses the nonweapon cosmetic base item");
        ctx.Assert(!GetIsItemPropertyValid(GetFirstItemProperty(companion)), "The cosmetic hand has zero DMG, ACC, global stat, enhancement or on-hit properties");
        ctx.Assert(!Item.IsAttackWeaponType(GetBaseItemType(companion)), "The cosmetic base is excluded from attack weapon classification");
        ctx.Assert(Item.IsEconomyRestricted(companion), "The cosmetic item is excluded by the shared economy classifier");
        ctx.Assert(!GetDroppableFlag(companion), "The cosmetic glove cannot become death loot");
        ctx.Assert(GetPlotFlag(companion), "The native plot flag restricts removing the generated item");
        ctx.AssertEqual(0, GetWeight(companion), "The cosmetic glove adds no carried weight");
        ctx.AssertEqual(0, GetGoldPieceValue(companion), "The cosmetic glove adds no sale value");
    }

    internal static string Properties(uint item)
    {
        var properties = new List<string>();
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            properties.Add(PropertySignature(property));
        return string.Join(";", properties.OrderBy(property => property));
    }

    private static string PropertySignature(SWLOR.NWN.API.Engine.ItemProperty property) =>
        $"{(int)GetItemPropertyType(property)}:{unchecked((ushort)GetItemPropertySubType(property))}:{unchecked((ushort)GetItemPropertyCostTableValue(property))}";
}
