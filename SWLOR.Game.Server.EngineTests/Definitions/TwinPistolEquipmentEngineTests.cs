using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolEquipmentEngineTests
{
    [EngineTest("Twin Pistol rejects combat droids and incompatible legacy native launchers clearly", Category = "TwinPistolEquipment", TimeoutSeconds = 30f)]
    public static async Task UnsupportedEquipmentContexts(EngineTestContext ctx)
    {
        var droid = ctx.SpawnCreature(Droid.DroidResref);
        var actor = ctx.SpawnCreature("civilian", 3f);
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(droid, () =>
        {
            var companion = CreateItemOnObject("b_companion", droid);
            ctx.Assert(GetIsObjectValid(companion), "The obtainable starter companion is available");
            ctx.AssertEqual("Combat droids cannot equip companion pistols.", Item.CanEquip(droid, companion, InventorySlot.LeftHand),
                "Unsupported combat droid integration fails before equipping or granting bonuses");
            ctx.Assert(!TwinPistolEquipment.IsActivePair(droid), "An unsupported droid cannot activate ordinary paired attacks");
        });
        var legacy = await ctx.EquipItemAsync(actor, "blast_jawa_d", InventorySlot.RightHand);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            var companion = CreateItemOnObject("b_companion", actor);
            ctx.AssertEqual(BaseItem.LegacyPistol, GetBaseItemType(legacy), "The fixture uses the unchanged legacy native launcher");
            ctx.AssertEqual("Legacy pistols cannot be paired with a companion pistol.", Item.CanEquip(actor, companion, InventorySlot.LeftHand),
                "The incompatible native ammunition path is excluded only from the new paired context");
            ctx.AssertEqual(legacy, GetItemInSlot(InventorySlot.RightHand, actor), "Rejection preserves the existing standalone legacy weapon");
        });
    }

    [EngineTest("Twin Pistol upgraded companion archives preserve ownership and equipment over repeated reloads", Category = "TwinPistolEquipment", TimeoutSeconds = 75f)]
    public static async Task CompanionArchives(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var companion = await ctx.EquipItemAsync(player.Creature, "fld_companion", InventorySlot.LeftHand);
        var main = await ctx.EquipItemAsync(player.Creature, "fld_pistol", InventorySlot.RightHand);
        var identity = Guid.NewGuid().ToString();
        var originalDamage = Item.GetDMG(companion);
        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
        {
            SetLocalString(companion, "TWIN_ARCHIVE_ID", identity);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 7), companion);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire), companion);
        });
        for (var cycle = 0; cycle < 2; cycle++)
        {
            var archive = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                ClearAllActions();
                CreaturePlugin.RunUnequip(player.Creature, main);
                CreaturePlugin.RunUnequip(player.Creature, companion);
                archive = ObjectPlugin.Serialize(companion);
                DestroyObject(companion);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(companion), 5f, "the original companion to leave the engine before reload");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                companion = ObjectPlugin.Deserialize(archive);
                ctx.Track(companion);
                ctx.Assert(ObjectPlugin.AcquireItem(player.Creature, companion), "Reloaded companion returns to its owner");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => ActionEquipItem(companion, InventorySlot.LeftHand));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.LeftHand, player.Creature) == companion, 5f, "reloaded companion to equip first");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => ActionEquipItem(main, InventorySlot.RightHand));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, player.Creature) == main, 5f, "main pistol to complete the restored pair");
            ctx.Assert(TwinPistolEquipment.IsActivePair(player.Creature), "A reloaded upgraded companion remains a valid pair");
            ctx.AssertEqual(player.Creature, GetItemPossessor(companion), "Ownership remains stable");
            ctx.AssertEqual(identity, GetLocalString(companion, "TWIN_ARCHIVE_ID"), "Archived item identity remains stable");
            ctx.AssertEqual(1, GetItemStackSize(companion), "A weapon reload cannot duplicate its quantity");
            ctx.AssertEqual(originalDamage, Item.GetDMG(companion), "Reload does not alter weapon damage");
            var profiles = new System.Collections.Generic.List<(ItemPropertyType Type, int Subtype, int Cost)>();
            for (var ip = GetFirstItemProperty(companion); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(companion))
                profiles.Add((GetItemPropertyType(ip), GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip)));
            ctx.Assert(profiles.Any(p => p.Type == ItemPropertyType.Accuracy && p.Cost == 7), "The Accuracy upgrade survives reload");
            ctx.Assert(profiles.Any(p => p.Type == ItemPropertyType.WeaponDamageType && p.Subtype == (int)CombatDamageType.Fire), "The typed-damage upgrade survives reload");
            // Native inventory iteration excludes equipped items; count the union by object ID.
            var owned = new System.Collections.Generic.HashSet<uint> { companion };
            for (var item = GetFirstItemInInventory(player.Creature); GetIsObjectValid(item); item = GetNextItemInInventory(player.Creature))
                if (GetBaseItemType(item) == BaseItem.CompanionPistol) owned.Add(item);
            ctx.AssertEqual(1, owned.Count, "Repeated reload leaves one owned companion across equipped and carried items");
        }
    }

    [EngineTest("Twin Pistol companion-first equip uses Twin ranks and revalidates the lone pistol", Category = "TwinPistolEquipment", TimeoutSeconds = 45f)]
    public static async Task CompanionFirstAndRemoval(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 25;
            record.Skills[SkillType.Pistol].Rank = 0;
        });
        var companion = await ctx.EquipItemAsync(player.Creature, "tp_comp_proto", InventorySlot.LeftHand);
        ctx.Assert(!TwinPistolEquipment.IsActivePair(player.Creature), "A companion equipped first is inactive");
        ctx.AssertEqual(SkillType.Invalid, Skill.GetWeaponSkill(player.Creature, companion), "Inactive companions provide no attacking skill");
        ctx.Assert(!EquipmentPredicates.HasDualWield(player.Creature), "An inactive companion grants no weapon defense predicate");
        ctx.AssertEqual(OBJECT_INVALID, EquipmentPredicates.GetOffhandAttackWeapon(player.Creature), "The carrier is never a native melee offhand");
        ctx.Assert(!Combat.HasEquippedWeaponForAbility(player.Creature), "An inactive companion cannot satisfy a generic equipped-weapon requirement");
        ctx.Assert(!Combat.CanItemTriggerWeaponAbility(companion, SkillType.Pistol), "An inactive carrier cannot deliver Pistol abilities");
        ctx.Assert(!Combat.CanItemTriggerWeaponAbility(companion, SkillType.Rifle), "An inactive carrier cannot deliver Rifle abilities");

        var main = await ctx.EquipItemAsync(player.Creature, "fld_pistol", InventorySlot.RightHand);
        ctx.Assert(TwinPistolEquipment.IsActivePair(player.Creature), "A Twin-only character can complete a tiered pair");
        ctx.AssertEqual(SkillType.TwinPistol, Skill.GetWeaponSkill(player.Creature, main), "The main hand resolves to Twin Pistol");
        ctx.AssertEqual(SkillType.TwinPistol, Skill.GetWeaponSkill(player.Creature, companion), "The simulated hand resolves to the same skill");
        ctx.AssertEqual(SkillType.Pistol, Skill.GetWeaponSkill(player.Creature, main, SkillType.Pistol), "A cross-trained Pistol ability retains its captured identity");
        ctx.Assert(!EquipmentPredicates.HasSingleWeapon(player.Creature), "An occupied companion hand disables Single Weapon");
        ctx.Assert(Combat.HasEquippedWeaponForAbility(player.Creature), "A validated pair satisfies generic equipped-weapon requirements");
        ctx.Assert(Combat.CanItemTriggerWeaponAbility(companion, SkillType.Pistol), "An active companion can deliver a captured cross-trained Pistol ability");
        ctx.Assert(Combat.CanItemTriggerWeaponAbility(companion, SkillType.Rifle), "An active companion can deliver a captured generic queued Rifle ability");

        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
        {
            var storedRequirement = SkillType.Invalid;
            for (var ip = GetFirstItemProperty(main); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(main))
                if (GetItemPropertyType(ip) == ItemPropertyType.RequiresSkill) storedRequirement = (SkillType)GetItemPropertySubType(ip);
            ctx.AssertEqual(SkillType.Pistol, storedRequirement, "Prospective equip never rewrites the stored pistol requirement");
        });

        for (var cycle = 0; cycle < 2; cycle++)
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => CreaturePlugin.RunUnequip(player.Creature, companion));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, player.Creature) != main, 5f,
                "main hand to return to inventory after losing its Twin Pistol eligibility");
            ctx.AssertEqual(player.Creature, GetItemPossessor(main), "Revalidation preserves the owned main-hand item");
            ctx.AssertEqual(player.Creature, GetItemPossessor(companion), "Revalidation preserves the owned companion");
            ctx.AssertEqual(0, Skill.GetCreatureSkillRank(player.Creature, SkillType.Pistol), "No hidden Pistol investment is granted");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => ActionEquipItem(companion, InventorySlot.LeftHand));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.LeftHand, player.Creature) == companion, 5f, "same companion to re-equip");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => ActionEquipItem(main, InventorySlot.RightHand));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, player.Creature) == main, 5f, "same main-hand pistol to re-equip");
            ctx.Assert(TwinPistolEquipment.IsActivePair(player.Creature), "The same two owned items restore the pair after repeated transitions");
        }
    }
}
