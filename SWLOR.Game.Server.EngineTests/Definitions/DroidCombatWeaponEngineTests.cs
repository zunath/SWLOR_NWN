using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class DroidInventoryEngineTests
    {
        private const string CombatProbe = "DROID_COMBAT_TEST_PROBE";
        private static readonly List<string> _rifleUnequips = new();

        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void RecordRifleUnequip()
        {
            if (!GetLocalBool(OBJECT_SELF, CombatProbe))
                return;
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            if (GetBaseItemType(item) != BaseItem.Rifle)
                return;
            _rifleUnequips.Add($"action={GetCurrentAction(OBJECT_SELF)} target={GetName(GetAttackTarget(OBJECT_SELF))} " +
                               $"bolts={GetItemStackSize(GetItemInSlot(InventorySlot.Bolts, OBJECT_SELF))} " +
                               $"stack={Environment.StackTrace}");
        }

        private static readonly List<string> _droidConversations = new();

        [NWNEventHandler(ScriptName.OnDroidConversation)]
        public static void RecordDroidConversation()
        {
            if (!GetLocalBool(OBJECT_SELF, CombatProbe))
                return;
            _droidConversations.Add($"pattern={GetListenPatternNumber()} command={GetLastAssociateCommand(OBJECT_SELF)} " +
                                    $"speakerIsMaster={GetLastSpeaker() == GetMaster(OBJECT_SELF)} inCombat={GetIsInCombat(OBJECT_SELF)}");
        }

        [EngineTest("Droid weapon equips and unequips are logged outside summon restores", Category = "DroidCombat", TimeoutSeconds = 90f)]
        public static async Task DroidEquipmentChangesAreLogged(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var controller = await CreateController(ctx, owner);
            var rifle = await CreateEquipment(ctx, owner, "oph_rifle");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var constructed = Droid.LoadConstructedDroid(controller);
                constructed.EquippedItems[InventorySlot.RightHand] = ObjectPlugin.Serialize(rifle);
                Droid.SaveConstructedDroid(controller, constructed);
                DestroyObject(rifle);
            });
            await ctx.WaitFrameAsync();
            var before = DroidEquipmentDiagnostics.LastEntry;
            var droid = await Spawn(ctx, owner, controller);
            try
            {
                rifle = GetItemInSlot(InventorySlot.RightHand, droid);
                ctx.AssertEqual("oph_rifle", GetResRef(rifle), "The summon restores the saved rifle");
                ctx.AssertEqual(before, DroidEquipmentDiagnostics.LastEntry, "Summon restores are not logged");

                // The summon sets the hand baseline, so an eventless engine unequip (disarm, polymorph)
                // right after it is caught by the first heartbeat.
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(droid).AsNWSCreature();
                    var nativeRifle = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(rifle).AsNWSItem();
                    ctx.Assert(native.UnequipItem(nativeRifle, 0, 0) != 0, "The engine unequips the rifle without events");
                });
                await ctx.ExecuteInCreatureContextAsync(droid, () => ExecuteScript(ScriptName.OnDroidHeartbeat, droid));
                var silent = DroidEquipmentDiagnostics.LastEntry;
                ctx.Assert(silent.StartsWith("change=hands changed without an equip event") && silent.Contains("detail=before=oph_rifle|") &&
                           silent.Contains("hands=|"), $"The silent change is logged: {silent}");
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(droid).AsNWSCreature();
                    var nativeRifle = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(rifle).AsNWSItem();
                    ctx.Assert(native.EquipItem((uint)global::NWN.Native.API.EquipmentSlot.RightHand, nativeRifle, 1, 0, 0) != 0,
                        "The engine restores the rifle to the hand");
                    ExecuteScript(ScriptName.OnDroidHeartbeat, droid);
                });

                await Unequip(ctx, droid, rifle);
                var unequipped = DroidEquipmentDiagnostics.LastEntry;
                ctx.Assert(unequipped.StartsWith("change=unequipped") && unequipped.Contains("item=oph_rifle slot=RightHand") && unequipped.Contains("hands=|"),
                    $"The unequip is logged: {unequipped}");
                ctx.Assert(unequipped.Contains("action=") && unequipped.Contains("inCombat=") && unequipped.Contains("associateCommand="),
                    $"The unequip entry carries the droid's state: {unequipped}");

                await Equip(ctx, droid, rifle, InventorySlot.RightHand);
                var equipped = DroidEquipmentDiagnostics.LastEntry;
                ctx.Assert(equipped.StartsWith("change=equipped") && equipped.Contains("item=oph_rifle slot=RightHand") && equipped.Contains("hands=oph_rifle|"),
                    $"The equip is logged: {equipped}");

                // A refused equip changes nothing and is not logged.
                var ring = await CreateEquipment(ctx, owner, "advent_ring", false);
                await Transfer(ctx, owner, ring, droid);
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ctx.Assert(!string.IsNullOrEmpty(Item.CanEquip(droid, ring)), "The droid cannot use the ring");
                    ClearAllActions();
                    ActionEquipItem(ring, InventorySlot.LeftRing);
                });
                await ctx.DelaySecondsAsync(2f);
                ctx.Assert(GetItemInSlot(InventorySlot.LeftRing, droid) != ring, "The ring was refused");
                ctx.AssertEqual(equipped, DroidEquipmentDiagnostics.LastEntry, "A refused equip is not logged");
                ctx.Log($"unequip: {unequipped}");
                ctx.Log($"equip: {equipped}");
                ctx.Log($"silent: {silent}");
            }
            finally
            {
                if (GetIsObjectValid(droid))
                    await Dismiss(ctx, owner, droid);
            }
        }

        [EngineTest("Idle droid keeps its rifle when a melee enemy attacks its owner", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static async Task IdleDroidOwnerAttacked(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var owner = player.Creature;
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            _rifleUnequips.Clear();
            _droidConversations.Clear();
            try
            {
                var rifle = await CreateEquipment(ctx, owner, "oph_rifle");
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    AddItemProperty(DurationType.Permanent, ItemPropertyUnlimitedAmmo(), rifle));
                await Transfer(ctx, owner, rifle, droid);
                await Equip(ctx, droid, rifle, InventorySlot.RightHand);
                SetImmortal(droid, true);
                SetLocalBool(droid, CombatProbe, true);
                SetPlotFlag(owner, true);

                var enemy = ctx.SpawnCreature("nw_bandit001", 5f);
                await ctx.WaitFrameAsync();
                await ctx.EquipItemAsync(enemy, "b_longsword", InventorySlot.RightHand);
                SetPlotFlag(enemy, true);
                ObjectPlugin.SetPosition(droid, GetPosition(owner) + new System.Numerics.Vector3(0f, 1f, 0f));
                ObjectPlugin.SetPosition(enemy, GetPosition(owner) + new System.Numerics.Vector3(1f, 0.5f, 0f));
                ctx.MakeHostile(enemy);
                CreaturePlugin.DoPerceptionUpdateOnCreature(droid, enemy);
                CreaturePlugin.DoPerceptionUpdateOnCreature(enemy, owner);
                await ctx.ExecuteInCreatureContextAsync(droid, () => ClearAllActions(true));
                AssignCommand(enemy, () => ActionAttack(owner));

                for (var tick = 0; tick < 200; tick++)
                {
                    await ctx.DelaySecondsAsync(0.1f);
                    if (tick % 30 == 0)
                        await ctx.ExecuteInCreatureContextAsync(droid, () => ClearAllActions(true));
                    var held = GetItemInSlot(InventorySlot.RightHand, droid);
                    if (held != rifle)
                        ctx.Fail($"The droid's right hand changed to '{GetName(held)}' after {tick / 10f:0.0}s. " +
                                 $"Conversations: {string.Join(" | ", _droidConversations)}. Unequips: {string.Join(" || ", _rifleUnequips)}");
                }
                ctx.Log($"conversations: {string.Join(" | ", _droidConversations)}");
                ctx.Assert(_droidConversations.Count > 0, "The droid heard its owner's associate events");
                AssignCommand(enemy, () => ClearAllActions());
            }
            finally
            {
                if (GetIsObjectValid(droid))
                {
                    DeleteLocalBool(droid, CombatProbe);
                    SetImmortal(droid, false);
                    await Dismiss(ctx, owner, droid);
                }
            }
        }

        [EngineTest("Med Kit droid keeps its rifle while healing mid-fight", Category = "DroidCombat", TimeoutSeconds = 150f)]
        public static Task MedKitDroidKeepsRifle(EngineTestContext ctx) => MedKitFight(ctx, false);

        [EngineTest("Med Kit droid summoned with its saved armor and rifle keeps the rifle while healing", Category = "DroidCombat", TimeoutSeconds = 150f)]
        public static Task SavedLoadoutMedKitDroidKeepsRifle(EngineTestContext ctx) => MedKitFight(ctx, true);

        private static readonly (InventorySlot Slot, string Resref)[] _savedDroidLoadout =
        {
            (InventorySlot.Head, "dhhl005"), (InventorySlot.Chest, "dhar005"), (InventorySlot.Boots, "dhlg005"),
            (InventorySlot.Arms, "dhbr005"), (InventorySlot.Cloak, "dlcl005"), (InventorySlot.LeftRing, "dlrg005s"),
            (InventorySlot.RightRing, "dlrg005"), (InventorySlot.Neck, "dlnk005"), (InventorySlot.Belt, "dlbe005"),
            (InventorySlot.RightHand, "oph_rifle")
        };

        private static async Task MedKitFight(EngineTestContext ctx, bool savedLoadout)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var owner = player.Creature;
            var controller = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                controller = CreateItemOnObject(Droid.DroidControlItemResref, owner);
                ctx.Assert(GetIsObjectValid(controller), "The controller exists");
                foreach (var (stat, value) in new[]
                         {
                             (DroidStatSubType.Tier, 5), (DroidStatSubType.AISlots, 20), (DroidStatSubType.HP, 70),
                             (DroidStatSubType.STM, 10), (DroidStatSubType.MGT, 10), (DroidStatSubType.PER, 32),
                             (DroidStatSubType.VIT, 18), (DroidStatSubType.WIL, 10), (DroidStatSubType.AGI, 23),
                             (DroidStatSubType.SOC, 10), (DroidStatSubType.Rifle, 45), (DroidStatSubType.Pistol, 45),
                             (DroidStatSubType.Armor, 45)
                         })
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)stat, value), controller);
                var constructed = Droid.LoadConstructedDroid(controller);
                constructed.LearnedPerks.Add(new DroidPerk { Perk = PerkType.MedKit, Level = 4 });
                constructed.ActivePerks.Add(new DroidPerk { Perk = PerkType.MedKit, Level = 4 });
                if (savedLoadout)
                {
                    // A summon restores the controller's saved equipment, as in normal play.
                    foreach (var (slot, resref) in _savedDroidLoadout)
                    {
                        var item = CreateItemOnObject(resref, owner);
                        ctx.Assert(GetIsObjectValid(item), $"The {resref} loadout item exists");
                        constructed.EquippedItems[slot] = ObjectPlugin.Serialize(item);
                        DestroyObject(item);
                    }
                }
                Droid.SaveInstructions(controller, constructed);
            });
            await ctx.WaitFrameAsync();
            var droid = await Spawn(ctx, owner, controller);
            _rifleUnequips.Clear();
            try
            {
                var rifle = OBJECT_INVALID;
                if (savedLoadout)
                {
                    await ctx.WaitUntilAsync(() => GetResRef(GetItemInSlot(InventorySlot.RightHand, droid)) == "oph_rifle", 5f, "the saved rifle to be restored in hand");
                    rifle = GetItemInSlot(InventorySlot.RightHand, droid);
                    foreach (var (slot, resref) in _savedDroidLoadout)
                        ctx.AssertEqual(resref, GetResRef(GetItemInSlot(slot, droid)), $"The droid wears its saved {resref}");
                }
                else
                {
                    await ctx.ExecuteInCreatureContextAsync(owner, () => rifle = CreateItemOnObject("oph_rifle", owner));
                    await ctx.WaitFrameAsync();
                    await Transfer(ctx, owner, rifle, droid);
                    await Equip(ctx, droid, rifle, InventorySlot.RightHand);
                }
                SetImmortal(droid, true);
                SetLocalBool(droid, CombatProbe, true);

                var enemy = ctx.SpawnCreature("nar_thief", 5f);
                await ctx.WaitFrameAsync();
                SetPlotFlag(enemy, false);
                Stat.SetNPCMaxHitPoints(enemy, 30000, true);
                ObjectPlugin.SetCurrentHitPoints(enemy, 30000);
                SetImmortal(enemy, true);
                ctx.MakeHostile(enemy);
                ObjectPlugin.SetPosition(enemy, GetPosition(droid) + new System.Numerics.Vector3(1f, 0f, 0f));
                CreaturePlugin.DoPerceptionUpdateOnCreature(droid, enemy);
                CreaturePlugin.DoPerceptionUpdateOnCreature(enemy, droid);
                SetPlotFlag(owner, true);
                AssignCommand(enemy, () => ActionAttack(droid));
                AssignCommand(owner, () => ActionAttack(enemy));

                await ctx.WaitUntilAsync(() => GetAttackTarget(droid) == enemy, 15f, "the droid to fight back");
                // Droids play Med Kit's native gesture, so the recast marks each heal.
                var heals = 0;
                var onRecast = false;
                for (var tick = 0; tick < 450; tick++)
                {
                    if (tick % 20 == 0)
                        ObjectPlugin.SetCurrentHitPoints(droid, Math.Max(1, GetMaxHitPoints(droid) / 3));
                    await ctx.DelaySecondsAsync(0.1f);
                    ctx.AssertEqual(rifle, GetItemInSlot(InventorySlot.RightHand, droid),
                        $"The droid holds its rifle after {tick / 10f:0.0}s. Unequips: {string.Join(" || ", _rifleUnequips)}");
                    var recasting = Recast.IsOnRecastDelay(droid, RecastGroup.MedKit).Item1;
                    if (recasting && !onRecast)
                        heals++;
                    onRecast = recasting;
                }
                ctx.Log($"medkit uses={heals} hp={GetCurrentHitPoints(droid)}/{GetMaxHitPoints(droid)} target={GetName(GetAttackTarget(droid))}");
                ctx.Assert(heals > 0, "The droid used Med Kit during the fight");
                AssignCommand(enemy, () => ClearAllActions());
                AssignCommand(owner, () => ClearAllActions());
            }
            finally
            {
                if (GetIsObjectValid(droid))
                {
                    DeleteLocalBool(droid, CombatProbe);
                    SetImmortal(droid, false);
                    await Dismiss(ctx, owner, droid);
                }
            }
        }

        [EngineTest("Droid keeps its rifle against a point-blank melee attacker over several rounds", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static Task PointBlankMelee(EngineTestContext ctx) => FightWithRifle(ctx, true, 0, 1f);

        [EngineTest("Droid keeps its rifle against Nar Shaddaa thieves while its owner fights", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static Task NarShaddaaThief(EngineTestContext ctx) => FightWithRifle(ctx, true, 0, 1f, "nar_thief", true);

        [EngineTest("Droid keeps its rifle against Nar Shaddaa rogue droids while its owner fights", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static Task NarShaddaaRogueDroid(EngineTestContext ctx) => FightWithRifle(ctx, true, 0, 6f, "nar_rogue_droid", true);

        [EngineTest("Droid keeps its rifle when a melee attacker closes in from range", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static Task AttackerClosesIn(EngineTestContext ctx) => FightWithRifle(ctx, true, 0, 8f);

        [EngineTest("Droid keeps its rifle when its equipped rifle ammunition runs out", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static Task FiniteAmmunitionRunsOut(EngineTestContext ctx) => FightWithRifle(ctx, true, 3, 1f);

        [EngineTest("Droid keeps a rifle without unlimited ammunition when its ammunition runs out", Category = "DroidCombat", TimeoutSeconds = 120f)]
        public static Task LimitedRifleAmmunitionRunsOut(EngineTestContext ctx) => FightWithRifle(ctx, false, 3, 1f);

        private static async Task FightWithRifle(EngineTestContext ctx, bool unlimitedAmmo, int boltCount, float enemyDistance,
            string enemyResref = "nw_bandit001", bool ownerJoins = false)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var owner = player.Creature;
            var controller = await CreateController(ctx, owner);
            var droid = await Spawn(ctx, owner, controller);
            _rifleUnequips.Clear();
            try
            {
                ctx.Assert(CompanionControl.IsControlledCompanion(droid), "The droid belongs to a real player fixture");
                var rifle = await CreateEquipment(ctx, owner, "oph_rifle");
                if (unlimitedAmmo)
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                        AddItemProperty(DurationType.Permanent, ItemPropertyUnlimitedAmmo(), rifle));
                if (boltCount > 0)
                {
                    // Real ammunition goes in first so the rifle never generates its own stack.
                    var bolts = await CreateEquipment(ctx, owner, "rifle_bullets");
                    await ctx.ExecuteInCreatureContextAsync(owner, () => SetItemStackSize(bolts, boltCount));
                    await Transfer(ctx, owner, bolts, droid);
                    await Equip(ctx, droid, bolts, InventorySlot.Bolts);
                }
                await Transfer(ctx, owner, rifle, droid);
                await Equip(ctx, droid, rifle, InventorySlot.RightHand);

                Stat.SetNPCMaxHitPoints(droid, 30000, true);
                ObjectPlugin.SetCurrentHitPoints(droid, 30000);
                SetImmortal(droid, true);
                SetLocalBool(droid, CombatProbe, true);

                var enemy = ctx.SpawnCreature(enemyResref, 4f + enemyDistance);
                await ctx.WaitFrameAsync();
                if (!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, enemy)) &&
                    !GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureRight, enemy)))
                    await ctx.EquipItemAsync(enemy, "b_longsword", InventorySlot.RightHand);
                SetPlotFlag(enemy, false);
                Stat.SetNPCMaxHitPoints(enemy, 30000, true);
                ObjectPlugin.SetCurrentHitPoints(enemy, 30000);
                SetImmortal(enemy, true);
                ctx.MakeHostile(enemy);
                ObjectPlugin.SetPosition(enemy, GetPosition(droid) + new System.Numerics.Vector3(enemyDistance, 0f, 0f));
                CreaturePlugin.DoPerceptionUpdateOnCreature(droid, enemy);
                CreaturePlugin.DoPerceptionUpdateOnCreature(enemy, droid);
                AssignCommand(enemy, () => ActionAttack(droid));
                if (ownerJoins)
                {
                    SetPlotFlag(owner, true);
                    CreaturePlugin.DoPerceptionUpdateOnCreature(owner, enemy);
                    AssignCommand(owner, () => ActionAttack(enemy));
                }

                await ctx.WaitUntilAsync(() => GetAttackTarget(droid) == enemy, 15f, "the droid to fight back");
                for (var tick = 0; tick < 300; tick++)
                {
                    await ctx.DelaySecondsAsync(0.1f);
                    var held = GetItemInSlot(InventorySlot.RightHand, droid);
                    if (held != rifle)
                        ctx.Fail($"The droid's right hand changed to '{GetName(held)}' after {tick / 10f:0.0}s " +
                                 $"(bolts={GetItemStackSize(GetItemInSlot(InventorySlot.Bolts, droid))}, " +
                                 $"rifle possessor={GetName(GetItemPossessor(rifle))}). Unequips: {string.Join(" || ", _rifleUnequips)}");
                }
                ctx.AssertEqual(0, _rifleUnequips.Count, "The rifle is never unequipped during the fight");
                ctx.Log($"distance={GetDistanceBetween(droid, enemy):0.0} target={GetName(GetAttackTarget(droid))} " +
                        $"bolts={GetItemStackSize(GetItemInSlot(InventorySlot.Bolts, droid))}");
                AssignCommand(enemy, () => ClearAllActions());
                AssignCommand(owner, () => ClearAllActions());
            }
            finally
            {
                if (GetIsObjectValid(droid))
                {
                    DeleteLocalBool(droid, CombatProbe);
                    SetImmortal(droid, false);
                    await Dismiss(ctx, owner, droid);
                }
            }
        }
    }
}
