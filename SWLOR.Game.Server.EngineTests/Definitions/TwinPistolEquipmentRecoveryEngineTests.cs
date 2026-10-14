using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolEquipmentRecoveryEngineTests
{
    private static uint _observed = OBJECT_INVALID;
    private static int _ordinaryHits;

    [NWNEventHandler(ScriptName.OnSWLORDamage)]
    public static void ObserveOrdinaryHits()
    {
        if (OBJECT_SELF == _observed && int.Parse(EventsPlugin.GetEventData("DAMAGE")) > 0)
            _ordinaryHits++;
    }

    [EngineTest("Twin Pistol packed inventory preserves invalid equipment and blocks attacks until recovery", Category = "TwinPistolEquipment", TimeoutSeconds = 60f)]
    public static async Task PackedInventoryWithoutBag(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 25;
            record.Skills[SkillType.Pistol].Rank = 0;
            record.Skills[SkillType.Rifle].Rank = 50;
            record.Perks[PerkType.PairedVolley] = 1;
            record.Perks[PerkType.Headshot] = 1;
        });
        var escrow = ctx.SpawnCreature("civilian", 6f);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        SetCommandable(false, escrow);
        await ctx.DelaySecondsAsync(1f);
        var companion = await ctx.EquipItemAsync(actor, "fld_companion", InventorySlot.LeftHand);
        var main = await ctx.EquipItemAsync(actor, "fld_pistol", InventorySlot.RightHand);
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
                ClearAllActions();
                CreaturePlugin.AddFeat(actor, FeatType.PairedVolley1);
                CreaturePlugin.AddFeat(actor, FeatType.Headshot1);
                foreach (var item in CarriedItems(actor))
                    ctx.Assert(ItemPlugin.MoveTo(item, escrow, true), "Setup stores each original carried item without deleting it");
                filler = CreateItemOnObject("sardine_ball", actor);
                ctx.Track(filler);
                ctx.Assert(GetIsObjectValid(filler), "The packed inventory contains a real owned item");
                ctx.AssertEqual("1", Get2DAString("baseitems", "InvSlotWidth", (int)GetBaseItemType(filler)), "The only carried item is one cell wide");
                ctx.AssertEqual("1", Get2DAString("baseitems", "InvSlotHeight", (int)GetBaseItemType(filler)), "The only carried item is one cell high");
                ctx.Assert(!GetHasInventory(filler), "The packed inventory has no recovery container");
                ctx.AssertEqual(1, CarriedItems(actor).Count, "No large donor or bag can make recovery space");
                ctx.Assert(GetIsObjectValid(GetObjectByTag("TEMP_ITEM_STORAGE")), "The existing storage helper can attempt its normal recovery bag path");
                repository.m_nWidth = 1;
                repository.m_nHeight = 1;
                repository.m_nBoundary = 1;
                repository.m_bScalable = 0;
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 10000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                SetCommandable(false, target);
                ctx.MakeHostile(target);
                TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 60f, "PACKED_EQUIPMENT");
            });
            var properties = Properties(main);
            ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The original equipment is valid before removing the companion");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                ctx.Assert(ItemPlugin.MoveTo(companion, escrow, true), "The original companion deliberately transfers to escrow"));
            await ctx.WaitUntilAsync(() => !string.IsNullOrEmpty(TwinPistolEquipmentValidation.GetUnusableEquipmentError(actor)),
                5f, "failed safe storage to mark the retained weapon unusable");

            for (var retry = 0; retry < 2; retry++)
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => TwinPistolEquipmentValidation.Revalidate(actor));
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, actor), "Failure retains the original main pistol in its equipment slot");
                ctx.AssertEqual(actor, GetItemPossessor(main), "Failure never drops or transfers the player's main pistol");
                ctx.AssertEqual(1, GetItemStackSize(main), "Repeated failure preserves the original weapon quantity");
                ctx.AssertEqual(properties, Properties(main), "Repeated failure preserves every original item property");
                ctx.AssertEqual(filler, CarriedItems(actor).Single(), "Repeated failure creates no carried bag or duplicate item");
                ctx.Assert(!string.IsNullOrEmpty(Item.CanEquip(actor, main, InventorySlot.RightHand)), "The retained lone pistol still fails its actual skill requirement");
                ctx.Assert(!string.IsNullOrEmpty(TwinPistolEquipmentValidation.GetUnusableEquipmentError(actor)), "The scoped failure remains active while space is unavailable");
            }

            _observed = actor;
            _ordinaryHits = 0;
            // Forced hits distinguish a readiness block from a randomly missed attack.
            Combat.SetAutoAttackHitResolutionOverride(true);
            Combat.SetAbilityHitResolutionOverride(true);
            AssignCommand(actor, () => ActionAttack(target));
            await ctx.DelaySecondsAsync(2f);
            ctx.AssertEqual(0, _ordinaryHits, "Retained invalid equipment cannot deliver ordinary damage");
            player.Update(record => record.Stamina = Stat.GetMaxStamina(actor));
            var stamina = Stat.GetCurrentStamina(actor);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                foreach (var feat in new[] { FeatType.PairedVolley1, FeatType.Headshot1 })
                {
                    ctx.Assert(!UsePerkFeat.TryUseAbility(actor, target, feat, GetLocation(target)), "The actual active and queued feat paths reject unusable retained equipment");
                    ctx.AssertEqual(TwinPistolEquipmentValidation.GetUnusableEquipmentError(actor), Ability.GetLastActivationDenialReason(), "Each denial reports the scoped storage failure");
                    ctx.Assert(!Recast.IsOnRecastDelay(actor, Ability.GetAbilityDetail(feat).RecastGroup).Item1, "Rejected abilities do not start their recast");
                }
            });
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "Rejected active and queued abilities spend no stamina");
            ctx.Assert(!UsePerkFeat.HasQueuedWeaponAbility(actor), "The rejected queued ability leaves no pending replacement");

            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                repository.m_nWidth = width;
                repository.m_nHeight = height;
                repository.m_nBoundary = boundary;
                repository.m_bScalable = scalable;
                ctx.Assert(ItemPlugin.MoveTo(filler, escrow, true), "Freeing space retains the original filler item");
                TwinPistolEquipmentValidation.Revalidate(actor);
            });
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, actor) != main, 5f,
                "normal safe recovery to return the same pistol after room is available");
            ctx.AssertEqual(actor, GetItemPossessor(main), "Successful recovery retains the same weapon owner");
            ctx.AssertEqual(properties, Properties(main), "Successful recovery preserves the original weapon properties");
            ctx.AssertEqual(string.Empty, TwinPistolEquipmentValidation.GetUnusableEquipmentError(actor), "Recovery clears the scoped readiness block");
            ctx.AssertEqual(1, CarriedItems(actor).Count(item => item == main), "Recovery returns exactly the original main weapon");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                ctx.Assert(ItemPlugin.MoveTo(companion, actor, true), "The same companion returns from escrow"));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionEquipItem(companion, InventorySlot.LeftHand));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.LeftHand, actor) == companion, 5f, "the original companion to equip again");
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionEquipItem(main, InventorySlot.RightHand));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, actor) == main, 5f, "the original main to restore the valid pair");
            ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "Recovery restores the original valid equipment arrangement");
            AssignCommand(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => _ordinaryHits > 0, 6f, "ordinary firing to resume with eligible equipment");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.PairedVolley1, GetLocation(target)),
                    "The actual paid ability path resumes after equipment recovery");
            });
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
                5f, "the recovered active ability's real impact");
            ctx.AssertEqual(1, GetItemStackSize(main), "The complete failure and recovery cycle retains one main pistol");
            ctx.AssertEqual(1, GetItemStackSize(companion), "The complete cycle retains one companion pistol");
        }
        finally
        {
            _observed = OBJECT_INVALID;
            repository.m_nWidth = width;
            repository.m_nHeight = height;
            repository.m_nBoundary = boundary;
            repository.m_bScalable = scalable;
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            TwinPistolAbility.Cancel(actor);
            UsePerkFeat.ClearQueuedAbility(actor);
            SetCommandable(true, escrow);
            SetCommandable(true, target);
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
        }
    }

    private static List<uint> CarriedItems(uint creature)
    {
        var items = new List<uint>();
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            if (GetItemPossessor(item, true) == creature) items.Add(item);
        return items;
    }

    private static string Properties(uint item)
    {
        var properties = new List<string>();
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            properties.Add($"{(int)GetItemPropertyType(property)}:{GetItemPropertySubType(property)}:{GetItemPropertyCostTableValue(property)}");
        return string.Join(";", properties.OrderBy(property => property));
    }
}
