using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class SingleWeaponEngineTests
{
    [EngineTest("Single Weapon natural and Doublehand bonuses follow equipment and purchased ranks", Category = "SingleWeapon", TimeoutSeconds = 60f)]
    public static async Task EquipmentAndRanks(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Armor].Rank = 40);
        var weapon = await ctx.EquipItemAsync(player.Creature, "b_longsword", InventorySlot.RightHand);
        await SetDamage(ctx, player.Creature, weapon, 24);
        foreach (var (rank, damage) in new[] { (0, 29), (1, 32), (2, 35), (3, 39) })
        {
            player.Update(record => record.Perks[PerkType.Doublehand] = rank);
            ctx.AssertEqual(damage, WeaponDamage.GetEffectiveDMG(player.Creature, weapon), $"Purchased rank {rank} combines with natural 20% once");
        }
        ctx.AssertEqual(24, Item.GetDMG(weapon), "The item's persistent DMG is unchanged");
        var description = GetDescription(weapon);
        var preview = new ExamineItemPayload(weapon);
        ctx.AssertEqual(24, preview.ItemDMG, "Examine snapshots base DMG for serialized market/storage items");
        ctx.AssertEqual(OBJECT_INVALID, preview.ItemObject, "A serialized preview must not follow a destroyed/reused engine object ID");
        ctx.AssertEqual(description, preview.Description, "Authored description is preserved");
        var shield = await ctx.EquipItemAsync(player.Creature, "ec_shield", InventorySlot.LeftHand);
        ctx.Assert(!EquipmentPredicates.HasSingleWeapon(player.Creature), "A shield disables single-weapon eligibility");
        ctx.AssertEqual(24, WeaponDamage.GetEffectiveDMG(player.Creature, weapon), "A shield disables both damage percentages");
        DestroyObject(shield);
        await ctx.WaitFrameAsync();
        var off = await ctx.EquipItemAsync(player.Creature, "b_longsword", InventorySlot.LeftHand);
        ctx.AssertEqual(24, WeaponDamage.GetEffectiveDMG(player.Creature, weapon), "A second weapon disables the bonus");
        ctx.AssertEqual(Item.GetDMG(off), WeaponDamage.GetEffectiveDMG(player.Creature, off), "Off-hand damage never receives Single Weapon");
        DestroyObject(off);
        await ctx.WaitFrameAsync();
        ctx.AssertEqual(39, WeaponDamage.GetEffectiveDMG(player.Creature, weapon), "Unequipping restores the purchased bonus");
        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 4), weapon));
        await ctx.WaitFrameAsync();
        ctx.AssertEqual(28, Item.GetDMG(weapon), "Permanent enhancement DMG contributes to the item rating");
        ctx.AssertEqual(45, WeaponDamage.GetEffectiveDMG(player.Creature, weapon), "The combined 60% applies once to the complete enhanced rating");
        var npc = ctx.SpawnCreature("nw_bandit001", 3f);
        await ctx.WaitFrameAsync();
        var npcWeapon = await ctx.EquipItemAsync(npc, "b_longsword", InventorySlot.RightHand);
        await SetDamage(ctx, npc, npcWeapon, 24);
        ctx.AssertEqual(24, WeaponDamage.GetEffectiveDMG(npc, npcWeapon), "Authored NPC budgets do not gain either bonus by default");
        ctx.SetNPCPerkLevel(npc, PerkType.Doublehand, 3);
        ctx.AssertEqual(34, WeaponDamage.GetEffectiveDMG(npc, npcWeapon), "An explicitly authored NPC perk applies through its stat");
        ctx.AssertEqual(0, Combat.GetCombatImpactWeaponDamage(player.Creature, SkillType.Force), "Force damage has no weapon component");
        ctx.AssertEqual(0, Combat.GetCombatImpactWeaponDamage(player.Creature, SkillType.Devices), "Devices damage has no weapon component");
    }

    [EngineTest("Pistols receive Single Weapon and Doublehand only without a shield", Category = "SingleWeapon", TimeoutSeconds = 60f)]
    public static async Task PistolShield(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Armor].Rank = 40);
        var pistol = await ctx.EquipItemAsync(player.Creature, "b_pistol", InventorySlot.RightHand);
        await SetDamage(ctx, player.Creature, pistol, 23);
        foreach (var (rank, damage) in new[] { (0, 28), (3, 37) })
        {
            player.Update(record => record.Perks[PerkType.Doublehand] = rank);
            ctx.Assert(EquipmentPredicates.HasSingleWeapon(player.Creature), "A pistol with an empty off hand qualifies");
            ctx.AssertEqual(damage, WeaponDamage.GetEffectiveDMG(player.Creature, pistol), $"Pistol Doublehand rank {rank} combines with natural 20% once");
        }
        var shield = await ctx.EquipItemAsync(player.Creature, "ec_shield", InventorySlot.LeftHand);
        ctx.Assert(!EquipmentPredicates.HasSingleWeapon(player.Creature), "A shield disables pistol single-weapon eligibility");
        ctx.AssertEqual(23, WeaponDamage.GetEffectiveDMG(player.Creature, pistol), "A shield disables both pistol damage percentages");
        DestroyObject(shield);
        await ctx.WaitFrameAsync();
        ctx.AssertEqual(37, WeaponDamage.GetEffectiveDMG(player.Creature, pistol), "Removing the shield restores the pistol bonus");
    }

    [EngineTest("Single Weapon natural bonus belongs to player-owned combat droids", Category = "SingleWeapon", TimeoutSeconds = 60f)]
    public static async Task DroidOwnership(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var droid = ctx.SpawnCreature(Droid.DroidResref, 2f);
        await ctx.WaitFrameAsync();
        ctx.AssertEqual(0, WeaponDamage.GetNaturalSingleWeaponPercent(droid), "An unowned authored droid has no natural bonus");
        await ctx.ExecuteInCreatureContextAsync(player.Creature, () => AddHenchman(player.Creature, droid));
        ctx.AssertEqual(20, WeaponDamage.GetNaturalSingleWeaponPercent(droid), "A player-owned combat droid receives the same natural equipment bonus");
        await ctx.ExecuteInCreatureContextAsync(player.Creature, () => RemoveHenchman(player.Creature, droid));
        ctx.AssertEqual(0, WeaponDamage.GetNaturalSingleWeaponPercent(droid), "Removing player ownership restores the authored budget");
    }

    [EngineTest("Single Weapon bonus reaches the native auto-attack damage hook", Category = "SingleWeapon", TimeoutSeconds = 60f)]
    public static async Task NativeDamage(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var target = ctx.SpawnCreature("nw_rat001", 1f);
        var weapon = await ctx.EquipItemAsync(player.Creature, "b_longsword", InventorySlot.RightHand);
        await SetDamage(ctx, player.Creature, weapon, 24);
        double Sample()
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player.Creature).AsNWSCreature();
            var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
            native.m_pcCombatRound.StartCombatRound(target);
            ctx.SeedRandom(12345);
            return Enumerable.Range(0, 256).Average(_ => native.m_pStats.GetDamageRoll(defender, 0, 0, 0, 0, 0));
        }
        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
        {
            player.Update(record => record.Perks[PerkType.Doublehand] = 0);
            var natural = Sample();
            player.Update(record => record.Perks[PerkType.Doublehand] = 3);
            var trained = Sample();
            ctx.Assert(natural > 0, "Native physical damage samples are positive");
            ctx.Assert(trained / natural is > 1.2 and < 1.5, "Native damage uses the effective rating, before the formula");
            ctx.Log($"256 native noncritical rolls per build: natural mean {natural:0.00}, Doublehand III mean {trained:0.00}.");
        });
    }

    [EngineTest("Single Weapon ability rating is captured once and cannot be double-scaled or changed between targets", Category = "SingleWeapon", TimeoutSeconds = 60f)]
    public static async Task AbilitySnapshot(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.Perks[PerkType.Doublehand] = 3;
            record.Skills[SkillType.Armor].Rank = 40;
        });
        var weapon = await ctx.EquipItemAsync(player.Creature, "b_longsword", InventorySlot.RightHand);
        await SetDamage(ctx, player.Creature, weapon, 24);
        var ability = Ability.GetAbilityDetail(FeatType.SavageCleave1);
        Ability.BeginAbilityImpact(player.Creature, ability);
        try
        {
            var getImpact = typeof(Ability).GetMethod("GetTrackedAbilityImpact", BindingFlags.NonPublic | BindingFlags.Static);
            var impact = getImpact.Invoke(null, new object[] { player.Creature });
            var capturedDamage = (int?)impact.GetType().GetProperty("TriggeringWeaponDamage").GetValue(impact);
            ctx.AssertEqual(39, capturedDamage.Value, "Weapon ability captures combined rating at impact start");
            var getThrowDamage = typeof(ThrowLightsaberAbilityDefinition).GetMethod("GetEquippedWeaponDamageAdjustment",
                BindingFlags.NonPublic | BindingFlags.Static);
            var capturedThrowDamage = (Func<uint, int>)getThrowDamage.Invoke(null, new object[] { player.Creature });
            ctx.AssertEqual(39, capturedThrowDamage(player.Creature), "Throw Lightsaber's explicit weapon component receives the same rating");
            var shield = await ctx.EquipItemAsync(player.Creature, "ec_shield", InventorySlot.LeftHand);
            ctx.AssertEqual(24, WeaponDamage.GetEffectiveDMG(player.Creature, weapon), "Live equipment no longer qualifies");
            ctx.AssertEqual(39, Combat.GetCombatImpactWeaponDamage(player.Creature, SkillType.Vibroblade,
                triggeringWeaponDamage: capturedDamage), "Later targets use the captured rating without applying the percentage twice");
            ctx.AssertEqual(39, capturedThrowDamage(player.Creature), "Throw Lightsaber captures its weapon component once for all path targets");
            DestroyObject(shield);
        }
        finally { Ability.EndAbilityImpact(player.Creature); }
    }

    private static async Task SetDamage(EngineTestContext ctx, uint creature, uint weapon, int damage)
    {
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            var properties = new System.Collections.Generic.List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                properties.Add(property);
            foreach (var property in properties.Where(ip => GetItemPropertyType(ip) == ItemPropertyType.DMG))
                RemoveItemProperty(weapon, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, damage), weapon);
        });
        await ctx.WaitFrameAsync();
    }
}
