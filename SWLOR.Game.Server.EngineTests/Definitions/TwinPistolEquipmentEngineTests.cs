using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolEquipmentEngineTests
{
    [EngineTest("Twin Pistol placed stores sell owned pairs and no independent companion guns", Category = "TwinPistolEquipment", TimeoutSeconds = 30f)]
    public static async Task PlacedStoreOwnership(EngineTestContext ctx)
    {
        var observer = ctx.SpawnCreature("civilian");
        var pairs = 0;
        await ctx.ExecuteInCreatureContextAsync(observer, () =>
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            for (var store = GetFirstObjectInArea(area, ObjectType.Store); GetIsObjectValid(store);
                 store = GetNextObjectInArea(area, ObjectType.Store))
            for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
            {
                ctx.Assert(GetBaseItemType(item) != BaseItem.CompanionPistol,
                    $"Placed store {GetResRef(store)} must not sell the retired independent off-hand gun {GetResRef(item)}");
                if (TwinPistolEquipment.IsPairItem(item)) pairs++;
            }
        });
        ctx.Assert(pairs >= 14, $"All fourteen authored starter placements contain owned Twin Pistols (found {pairs})");
        ctx.SetResultDetail($"Verified {pairs} native placed-store owned pairs and no independent companion stock.");
    }

    [EngineTest("Twin Pistol equips one owned item and creates a linked temporary spare", Category = "TwinPistolEquipment", TimeoutSeconds = 30f)]
    public static async Task SingleOwnedItem(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 0;
            record.Skills[SkillType.Pistol].Rank = 0;
        });
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await WaitForPair(ctx, actor, main);
        ctx.Assert(TwinPistolEquipment.IsPairItem(main), "The obtained item is the owned Twin Pistol pair");
        ctx.Assert(TwinPistolEquipment.IsGeneratedCompanion(spare), "Only the off-hand item is generated");
        ctx.Assert(TwinPistolEquipment.IsLinkedPair(actor, main, spare), "The generated spare belongs to this exact main and actor");
        ctx.AssertEqual(SkillType.TwinPistol, Skill.GetWeaponSkill(actor, main), "The owned item uses Twin Pistol without Pistol investment");
        ctx.AssertEqual(SkillType.TwinPistol, Skill.GetWeaponSkill(actor, spare), "The simulated weapon uses the same skill");
        ctx.Assert(!GetDroppableFlag(spare), "The temporary gun is not death loot");
        ctx.Assert(GetPlotFlag(spare), "The temporary gun has the native plot flag used to restrict inventory removal");
        for (var property = GetFirstItemProperty(spare); GetIsItemPropertyValid(property); property = GetNextItemProperty(spare))
            ctx.Assert(TwinPistolEquipment.IsMirroredProperty(GetItemPropertyType(property)),
                "The temporary gun contains only the real item's attack profile, with no independent enhancement slots or global bonuses");
        ctx.Assert(!EquipmentPredicates.HasSingleWeapon(actor), "A pair does not grant Single Weapon");
        ctx.Assert(Combat.HasEquippedWeaponForAbility(actor), "The linked pair is ready for weapon abilities");
        AssertOwnedItems(ctx, actor, main, 1);

        for (var repeat = 0; repeat < 3; repeat++)
            await ctx.ExecuteInCreatureContextAsync(actor, () => TwinPistolEquipment.EnsurePair(actor));
        ctx.AssertEqual(spare, GetItemInSlot(InventorySlot.LeftHand, actor), "Repeated reconciliation keeps the current spare");
        AssertOwnedItems(ctx, actor, main, 1);
    }

    [EngineTest("Twin Pistol unequipping either hand returns the whole pair and destroys the spare", Category = "TwinPistolEquipment", TimeoutSeconds = 45f)]
    public static async Task EitherHandRemoval(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        // Pistol investment must not let the real Twin Pistol item remain half-equipped.
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 25;
            record.Skills[SkillType.Pistol].Rank = 50;
        });
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var properties = Properties(main);
        for (var cycle = 0; cycle < 4; cycle++)
        {
            var spare = await WaitForPair(ctx, actor, main);
            var requested = cycle % 2 == 0 ? main : spare;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                ActionUnequipItem(requested);
            });
            await WaitForRemovedPair(ctx, actor, main, spare);
            ctx.AssertEqual(properties, Properties(main), "Removing either hand preserves all owned item properties");
            AssertOwnedItems(ctx, actor, main, 0);
            if (cycle < 3) await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
        }
    }

    [EngineTest("Twin Pistol whole-pair removal applies equipment stat deltas exactly once", Category = "TwinPistolEquipment", TimeoutSeconds = 45f)]
    public static async Task WholePairStatDeltas(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var actor = player.Creature;
        var main = await CreateCarriedItem(ctx, actor, "b_twinpistol");
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, 7), main);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Stamina, -1, 9), main);
        });
        var baseline = DB.Get<Player>(player.Id);
        var hp = baseline.MaxHP;
        var stamina = baseline.MaxStamina;
        for (var cycle = 0; cycle < 4; cycle++)
        {
            await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            var spare = await WaitForPair(ctx, actor, main);
            var equipped = DB.Get<Player>(player.Id);
            ctx.AssertEqual(hp + 7, equipped.MaxHP, "The real item's HP bonus is added exactly once");
            ctx.AssertEqual(stamina + 9, equipped.MaxStamina, "The real item's Stamina bonus is added exactly once");
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(cycle % 2 == 0 ? spare : main));
            await WaitForRemovedPair(ctx, actor, main, spare);
            var removed = DB.Get<Player>(player.Id);
            ctx.AssertEqual(hp, removed.MaxHP, "Either-hand removal subtracts the real item's HP bonus exactly once");
            ctx.AssertEqual(stamina, removed.MaxStamina, "Either-hand removal subtracts the real item's Stamina bonus exactly once");
        }
    }

    [EngineTest("Twin Pistol replacement attempts preserve both hands until the pair is removed", Category = "TwinPistolEquipment", TimeoutSeconds = 55f)]
    public static async Task ReplacementAndRapidSwaps(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            foreach (var skill in record.Skills.Values) skill.Rank = 50;
        });
        var actor = player.Creature;
        var first = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await WaitForPair(ctx, actor, first);
        var second = await CreateCarriedItem(ctx, actor, "b_twinpistol");
        var shield = await CreateCarriedItem(ctx, actor, "bm_shield");
        foreach (var (replacement, slot) in new[] { (second, InventorySlot.RightHand), (shield, InventorySlot.LeftHand) })
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionEquipItem(replacement, slot));
            await ctx.DelaySecondsAsync(.5f);
            ctx.AssertEqual(first, GetItemInSlot(InventorySlot.RightHand, actor), "The existing no-swap rule preserves the real main");
            ctx.AssertEqual(spare, GetItemInSlot(InventorySlot.LeftHand, actor), "A rejected replacement preserves the linked spare");
            ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "Rejected replacements leave a usable complete pair");
        }
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(spare));
        await WaitForRemovedPair(ctx, actor, first, spare);
        await EquipExisting(ctx, actor, shield, InventorySlot.LeftHand);
        ctx.Assert(!TwinPistolEquipment.IsActivePair(actor), "A later shield replacement has no leftover paired context");
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionEquipItem(second, InventorySlot.RightHand));
        await ctx.DelaySecondsAsync(.5f);
        ctx.AssertEqual(shield, GetItemInSlot(InventorySlot.LeftHand, actor), "Equipping a pair cannot silently discard an owned shield");
        ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, actor)), "An occupied off hand prevents pair equip");
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(shield));
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.LeftHand, actor) != shield, 5f, "the shield to leave the hand");

        foreach (var main in new[] { first, second, first, second })
        {
            await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            spare = await WaitForPair(ctx, actor, main);
            ctx.AssertEqual(1, OwnedItems(actor).Count(TwinPistolEquipment.IsGeneratedCompanion), "Each rapid equip leaves exactly one temporary gun");
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(spare));
            await WaitForRemovedPair(ctx, actor, main, spare);
            ctx.AssertEqual(0, OwnedItems(actor).Count(TwinPistolEquipment.IsGeneratedCompanion), "Each rapid removal destroys the temporary gun");
        }
        ctx.AssertEqual(2, OwnedItems(actor).Count(TwinPistolEquipment.IsPairItem), "Transitions retain exactly the two originally acquired real items");
    }

    [EngineTest("Twin Pistol drop and transfer attempts cannot turn the temporary spare into an owned item", Category = "TwinPistolEquipment", TimeoutSeconds = 35f)]
    public static async Task TemporarySpareCannotEscape(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var actor = player.Creature;
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var spare = await WaitForPair(ctx, actor, main);
        var recipient = ctx.SpawnCreature("civilian", 5f);
        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionPutDownItem(spare));
        await ctx.DelaySecondsAsync(.5f);
        ctx.Assert(!GetIsObjectValid(spare) || GetItemPossessor(spare) == actor, "Native drop cannot leave a collectible spare on the ground");
        if (!GetIsObjectValid(spare))
        {
            await WaitForRemovedPair(ctx, actor, main, spare);
            await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            spare = await WaitForPair(ctx, actor, main);
        }
        await ctx.ExecuteInCreatureContextAsync(actor, () => ItemPlugin.MoveTo(spare, recipient, true));
        await ctx.WaitUntilAsync(() => !GetIsObjectValid(spare), 5f, "a forced transfer to destroy the temporary spare");
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, actor) != main, 5f, "losing the spare to return the real item");
        ctx.AssertEqual(actor, GetItemPossessor(main), "A spare transfer preserves the real item's owner");
        ctx.Assert(!OwnedItems(recipient).Any(TwinPistolEquipment.IsGeneratedCompanion), "The recipient retains no temporary gun");
        AssertOwnedItems(ctx, actor, main, 0);
    }

    [EngineTest("Twin Pistol generated Basic and Veteran guns retain native attribute damage caps", Category = "TwinPistolEquipment", TimeoutSeconds = 65f)]
    public static async Task GeneratedWeaponDamageCaps(EngineTestContext ctx)
    {
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            foreach (var (resref, requiredRank, cap) in new[] { ("b_twinpistol", 0, 1), ("vet_twinpistol", 15, 2) })
            {
                using var player = await PlayerAbilityFixture.CreateAsync(ctx);
                player.Update(record =>
                {
                    record.Perks.Clear();
                    record.Skills[SkillType.TwinPistol].Rank = 50;
                });
                var actor = player.Creature;
                var main = await ctx.EquipItemAsync(actor, resref, InventorySlot.RightHand);
                var spare = await WaitForPair(ctx, actor, main);
                var target = ctx.SpawnCreature("nw_rat001", 1f);
                await ctx.DelaySecondsAsync(1f);
                try
                {
                    foreach (var (attackStat, defenseStat) in new[] { (50, 6), (6, 50) })
                    {
                        AssertMatchingRequirementsAndCaps(ctx, main, spare, requiredRank, cap);
                        int[] mainDamage = null;
                        int[] spareDamage = null;
                        await ctx.ExecuteInCreatureContextAsync(actor, () =>
                        {
                            ClearAllActions();
                            var profile = WeaponShotProfile.Capture(actor, main, SkillType.TwinPistol);
                            ctx.AssertEqual(CombatDamageType.Physical, profile.DamageType, "The authored tier uses physical ordinary damage");
                            ctx.AssertEqual(profile.DamageAbility, WeaponShotProfile.Capture(actor, spare, SkillType.TwinPistol).DamageAbility,
                                "Both hands use the same damage attribute");
                            CreaturePlugin.SetRawAbilityScore(actor, profile.DamageAbility, attackStat);
                            CreaturePlugin.SetRawAbilityScore(target, AbilityType.Vitality, defenseStat);
                            SetPlotFlag(target, false);
                            ctx.SuppressNPCNaturalRegen(target);
                            Stat.SetNPCMaxHitPoints(target, 20000, true);

                            mainDamage = SampleOrdinaryDamage(ctx, actor, target, main, false);
                            spareDamage = SampleOrdinaryDamage(ctx, actor, target, spare, true);
                            ctx.Assert(mainDamage.SequenceEqual(spareDamage),
                                $"{resref} native main and simulated spare have identical seeded noncritical ordinary rolls at {attackStat} versus {defenseStat}");
                            ctx.Assert(mainDamage.Average() > 1, "Capped ordinary damage remains above the minimum damage floor");

                            // Reproduce the missing-property defect on this disposable spare only.
                            var requirements = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                            for (var ip = GetFirstItemProperty(spare); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(spare))
                                if (GetItemPropertyType(ip) == ItemPropertyType.RequiresSkill) requirements.Add(ip);
                            foreach (var ip in requirements) RemoveItemProperty(spare, ip);
                        });
                        // Native passive properties are removed after the current script callback.
                        await ctx.WaitUntilAsync(() => NativeWeaponDeltaCap(ctx, spare) == 0, 3f,
                            "the disposable spare's skill requirement to leave the native passive-property list");
                        await ctx.ExecuteInCreatureContextAsync(actor, () =>
                        {
                            ctx.AssertEqual(0, NativeWeaponDeltaCap(ctx, spare), "A spare missing RequiresSkill reproduces the original uncapped native path");
                            var uncappedDamage = SampleOrdinaryDamage(ctx, actor, target, spare, true);
                            ctx.Assert(attackStat > defenseStat
                                    ? uncappedDamage.Average() > spareDamage.Average()
                                    : uncappedDamage.Average() < spareDamage.Average(),
                                "Removing only the spare requirement changes actual native damage in the expected direction");
                            ctx.Log($"{resref} requirement={requiredRank}, cap={cap}, attributes={attackStat}/{defenseStat}: " +
                                    $"128 matched-seed native ordinary rolls per hand, main={mainDamage.Average():0.00}, " +
                                    $"generated={spareDamage.Average():0.00}, missing-property control={uncappedDamage.Average():0.00}");
                        });

                        // The next scenario starts with a fresh production-generated companion.
                        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(main));
                        await WaitForRemovedPair(ctx, actor, main, spare);
                        await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
                        spare = await WaitForPair(ctx, actor, main);
                        AssertMatchingRequirementsAndCaps(ctx, main, spare, requiredRank, cap);
                    }
                }
                finally { RangedWeaponAttackContext.Clear(actor); }
            }
        }
        finally { Combat.SetAutoAttackHitResolutionOverride(null); }
    }

    [EngineTest("Twin Pistol native character saves restore one upgraded owned pair over repeated reloads", Category = "TwinPistolEquipment", TimeoutSeconds = 95f)]
    public static async Task CharacterArchives(EngineTestContext ctx)
    {
        var player = await PlayerAbilityFixture.CreateAsync(ctx);
        try
        {
            player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
            var main = await ctx.EquipItemAsync(player.Creature, "b_twinpistol", InventorySlot.RightHand);
            var identity = Guid.NewGuid().ToString();
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => SetLocalString(main, "TWIN_ARCHIVE_ID", identity));
            for (var cycle = 0; cycle < 2; cycle++)
            {
                var actor = player.Creature;
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 7 + cycle), main);
                    if (cycle == 0)
                        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire), main);
                });
                var properties = Properties(main);
                var oldSpare = await WaitForPair(ctx, actor, main);
                AssertMatchingRequirementsAndCaps(ctx, main, oldSpare, 0, 1);
                player = await player.SaveAndReloadAsync(ctx, TwinPistolEquipment.ReleaseCompanions);
                actor = player.Creature;
                await ctx.ExecuteInCreatureContextAsync(actor, () => TwinPistolEquipment.RestorePair(actor));
                main = FindArchivedPair(ctx, actor, identity);
                var spare = await WaitForPair(ctx, actor, main);
                AssertMatchingRequirementsAndCaps(ctx, main, spare, 0, 1);
                ctx.AssertEqual(identity, GetLocalString(main, "TWIN_ARCHIVE_ID"), "Native character export retains the real item's identity");
                ctx.AssertEqual(properties, Properties(main), "Native character export retains every real item upgrade");
                ctx.AssertEqual(Item.GetDMG(main), Item.GetDMG(spare), "The restored spare mirrors the real weapon damage profile");
                ctx.AssertEqual(Properties(main, true), Properties(spare, true), "The regenerated spare mirrors all saved attack-profile upgrades");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(oldSpare), 5f, "logout cleanup to destroy the old world's temporary gun");
                AssertOwnedItems(ctx, actor, main, 1);

                await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(cycle == 0 ? main : spare));
                await WaitForRemovedPair(ctx, actor, main, spare);
                player = await player.SaveAndReloadAsync(ctx, TwinPistolEquipment.ReleaseCompanions);
                actor = player.Creature;
                await ctx.ExecuteInCreatureContextAsync(actor, () => TwinPistolEquipment.RestorePair(actor));
                main = FindArchivedPair(ctx, actor, identity);
                ctx.AssertEqual(properties, Properties(main), "Carried-state save/reload retains all real item upgrades");
                ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, actor)), "A carried pair is not automatically equipped on reload");
                AssertOwnedItems(ctx, actor, main, 0);
                await EquipExisting(ctx, actor, main, InventorySlot.RightHand);
                spare = await WaitForPair(ctx, actor, main);
                AssertMatchingRequirementsAndCaps(ctx, main, spare, 0, 1);
            }
            ctx.SetResultDetail("Two upgraded equipped/carried cycles; four native SaveCreature/LoadCreature PC reloads. Connected client/server-vault login and barter UI are not simulated.");
        }
        finally { player.Dispose(); }
    }

    internal static async Task<uint> WaitForPair(EngineTestContext ctx, uint actor, uint main)
    {
        await ctx.WaitUntilAsync(() => TwinPistolEquipment.IsActivePair(actor), 6f, "the owned item to create and equip its temporary spare");
        var spare = GetItemInSlot(InventorySlot.LeftHand, actor);
        ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "The original real item remains equipped");
        ctx.Assert(TwinPistolEquipment.IsLinkedPair(actor, main, spare), "The temporary gun is linked to the real item");
        return spare;
    }

    private static int[] SampleOrdinaryDamage(EngineTestContext ctx, uint actor, uint target, uint weapon, bool simulated)
    {
        var profile = WeaponShotProfile.Capture(actor, weapon, SkillType.TwinPistol);
        using var selected = simulated ? RangedWeaponAttackContext.SelectProfile(actor, profile) : null;
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
        var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
        native.m_pcCombatRound.StartCombatRound(target);
        ctx.SeedRandom(917253);
        var damage = new int[128];
        for (var sample = 0; sample < damage.Length; sample++)
        {
            native.m_pcCombatRound.m_nCurrentAttack = 0;
            native.m_pcCombatRound.GetAttack(0).m_bRangedAttack = 1;
            native.ResolveAttackRoll(defender);
            damage[sample] = native.m_pStats.GetDamageRoll(defender, 0, 0, 0, 0, 0);
            var roll = RangedWeaponAttackContext.GetRoll(actor, target, 0);
            ctx.Assert(roll is { Ordinary: true, Completed: true }, "The production native resolver completes an ordinary weapon roll");
            ctx.AssertEqual(weapon, roll.Weapon, "Native damage belongs to the requested hand");
            ctx.AssertEqual(damage[sample], roll.Damage, "The ordinary shot records the actual native physical damage");
        }
        return damage;
    }

    private static void AssertMatchingRequirementsAndCaps(EngineTestContext ctx, uint main, uint spare, int rank, int cap)
    {
        foreach (var item in new[] { main, spare })
        {
            var requirements = new List<(int Skill, int Rank)>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.RequiresSkill)
                    requirements.Add((GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip)));
            ctx.AssertEqual(1, requirements.Count, "Each hand retains exactly one authored skill requirement");
            ctx.AssertEqual(((int)SkillType.TwinPistol, rank), requirements[0], "Both hands retain the authored Twin Pistol tier requirement");
            ctx.AssertEqual(cap, NativeWeaponDeltaCap(ctx, item), "Both hands resolve the same production native attribute-delta cap");
        }
    }

    private static int NativeWeaponDeltaCap(EngineTestContext ctx, uint item)
    {
        var getCap = typeof(GetDamageRoll).GetMethod("GetWeaponDeltaCap", BindingFlags.NonPublic | BindingFlags.Static);
        ctx.Assert(getCap != null, "The production native weapon-cap resolver exists");
        var weapon = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(item).AsNWSItem();
        return (int)getCap.Invoke(null, new object[] { weapon });
    }

    private static uint FindArchivedPair(EngineTestContext ctx, uint actor, string identity)
    {
        var items = OwnedItems(actor);
        var summary = string.Join(", ", items.Select(item =>
            $"{item:X}:{GetResRef(item)}:type={(int)GetBaseItemType(item)}:" +
            $"pair={GetLocalInt(item, TwinPistolEquipment.PairVariable)}:archive={GetLocalString(item, "TWIN_ARCHIVE_ID")}"));
        ctx.Log($"Restored owned items for {actor:X}: {summary}");
        var originals = items.Where(item => GetLocalString(item, "TWIN_ARCHIVE_ID") == identity).ToArray();
        ctx.AssertEqual(1, originals.Length, $"Native character reload retains exactly the original real item; observed={summary}");
        ctx.Assert(TwinPistolEquipment.IsPairItem(originals[0]), "The original real item's pair marker survives native character export");
        return originals[0];
    }

    internal static async Task WaitForRemovedPair(EngineTestContext ctx, uint actor, uint main, uint spare)
    {
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, actor) != main && !GetIsObjectValid(spare),
            6f, "the whole pair to unequip and its temporary gun to be destroyed");
        ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.LeftHand, actor)), "The temporary hand is empty after removing the pair");
        ctx.AssertEqual(actor, GetItemPossessor(main), "The original real item stays owned");
        ctx.AssertEqual(1, GetItemStackSize(main), "The real pair remains one item");
        ctx.Assert(!TwinPistolEquipment.IsActivePair(actor), "Unequipped items cannot retain the paired attack context");
    }

    internal static async Task<uint> CreateCarriedItem(EngineTestContext ctx, uint actor, string resref)
    {
        var item = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(actor, () => item = CreateItemOnObject(resref, actor));
        await ctx.WaitUntilAsync(() => GetIsObjectValid(item) && GetItemPossessor(item) == actor, 5f, $"owned item {resref} to enter inventory");
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
        await ctx.WaitUntilAsync(() => GetItemInSlot(slot, actor) == item, 6f, "the requested real item to equip");
    }

    internal static HashSet<uint> OwnedItems(uint actor)
    {
        var items = new HashSet<uint>();
        AddInventory(actor);
        foreach (var slot in Enum.GetValues<InventorySlot>())
        {
            if (slot == InventorySlot.Invalid) continue;
            var item = GetItemInSlot(slot, actor);
            if (GetIsObjectValid(item)) items.Add(item);
        }
        return items;

        void AddInventory(uint container)
        {
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                if (!items.Add(item)) continue;
                if (GetHasInventory(item)) AddInventory(item);
            }
        }
    }

    internal static void AssertOwnedItems(EngineTestContext ctx, uint actor, uint main, int temporaryCount)
    {
        var items = OwnedItems(actor);
        ctx.AssertEqual(1, items.Count(TwinPistolEquipment.IsPairItem), "Exactly one originally acquired Twin Pistol item is owned");
        ctx.AssertEqual(temporaryCount, items.Count(TwinPistolEquipment.IsGeneratedCompanion), "Only the equipped temporary spare may exist");
        ctx.Assert(items.Contains(main), "The exact real item remains equipped or in owned storage");
        ctx.AssertEqual(actor, GetItemPossessor(main), "The real item belongs to the expected player");
        ctx.AssertEqual(1, GetItemStackSize(main), "The real item is never duplicated into a stack");
    }

    internal static string Properties(uint item, bool mirroredOnly = false)
    {
        var properties = new List<string>();
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
        {
            if (mirroredOnly && !TwinPistolEquipment.IsMirroredProperty(GetItemPropertyType(property))) continue;
            // GFF Subtype and CostValue are WORDs; native -1 and reloaded 65535 are the same sentinel.
            properties.Add($"{(int)GetItemPropertyType(property)}:{unchecked((ushort)GetItemPropertySubType(property))}:{unchecked((ushort)GetItemPropertyCostTableValue(property))}");
        }
        return string.Join(";", properties.OrderBy(property => property));
    }
}
