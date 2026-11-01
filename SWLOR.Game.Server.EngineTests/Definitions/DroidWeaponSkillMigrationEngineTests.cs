using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        /// <summary>A CPU blueprint with both the martial and ranged groups at 25.</summary>
        internal const string LegacyDroidCpu = "d_jg_cpu3_m";
        internal const int LegacyCpuSkill = 25;
        internal const int LegacyControllerKatar = 30;
        internal const int LegacyControllerStaff = 25;

        /// <summary>A CPU saved before the Unarmed and Twin Pistol skills existed.</summary>
        internal static void StampLegacyDroidCpu(uint cpu)
        {
            RemoveDroidStats(cpu, DroidStatSubType.Unarmed, DroidStatSubType.TwinPistol);
        }

        /// <summary>
        /// A controller assembled before the new skills: a boosted Katar, the ranged group at zero as assembly saves
        /// skills no part grants, and the CPU saved inside it.
        /// </summary>
        internal static void StampLegacyDroidController(uint controller, string serializedCpu)
        {
            RemoveDroidStats(controller, DroidStatSubType.Katar, DroidStatSubType.Staff, DroidStatSubType.Pistol, DroidStatSubType.Rifle,
                DroidStatSubType.Throwing, DroidStatSubType.Unarmed, DroidStatSubType.TwinPistol);
            foreach (var (skill, value) in new[]
                     {
                         (DroidStatSubType.Katar, LegacyControllerKatar), (DroidStatSubType.Staff, LegacyControllerStaff),
                         (DroidStatSubType.Pistol, 0), (DroidStatSubType.Rifle, 0), (DroidStatSubType.Throwing, 0),
                     })
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)skill, value), controller);
            Droid.SaveConstructedDroid(controller, new ConstructedDroid { Name = "Weapon skill fixture", SerializedCPU = serializedCpu });
        }

        /// <summary>The new skills on a migrated controller and the CPU saved inside it.</summary>
        internal static void AssertMigratedDroidWeaponSkills(EngineTestContext ctx, uint controller, string label)
        {
            ctx.AssertEqual(LegacyControllerStaff, PropertyValue(controller, ItemPropertyType.DroidStat, (int)DroidStatSubType.Unarmed),
                $"{label}: the controller's Unarmed takes its lower martial skill, not the Katar boost");
            // Assembly adds a zero for every skill the same way; whether the engine keeps a zero DroidStat decides
            // whether the ranged group, and so a zero Twin Pistol, exists on the controller at all.
            var rangedKept = PropertyValue(controller, ItemPropertyType.DroidStat, (int)DroidStatSubType.Pistol) >= 0;
            ctx.AssertEqual(rangedKept ? 0 : -1, PropertyValue(controller, ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinPistol),
                $"{label}: Twin Pistol matches the zero ranged skills (pistol property kept: {rangedKept})");
            ctx.AssertEqual(LegacyControllerKatar, PropertyValue(controller, ItemPropertyType.DroidStat, (int)DroidStatSubType.Katar),
                $"{label}: the Katar boost is kept");
            ctx.AssertEqual(1, CountDroidStats(controller, DroidStatSubType.Unarmed), $"{label}: exactly one Unarmed property");
            ctx.AssertEqual(rangedKept ? 1 : 0, CountDroidStats(controller, DroidStatSubType.TwinPistol), $"{label}: at most one Twin Pistol property");

            var cpu = Deserialize(ctx, Droid.LoadConstructedDroid(controller).SerializedCPU);
            ctx.AssertEqual(LegacyDroidCpu, GetResRef(cpu), $"{label}: the saved CPU is kept");
            ctx.AssertEqual(LegacyCpuSkill, PropertyValue(cpu, ItemPropertyType.DroidStat, (int)DroidStatSubType.Unarmed),
                $"{label}: the saved CPU gains Unarmed as a new CPU carries it");
            ctx.AssertEqual(LegacyCpuSkill, PropertyValue(cpu, ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinPistol),
                $"{label}: the saved CPU gains Twin Pistol as a new CPU carries it");
            DestroyObject(cpu);
        }

        private static void RemoveDroidStats(uint item, params DroidStatSubType[] skills)
        {
            var matches = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.DroidStat && skills.Contains((DroidStatSubType)GetItemPropertySubType(ip)))
                    matches.Add(ip);
            foreach (var ip in matches)
                Invoke("MigrationObject", "RemoveProperty", item, ip);
        }

        private static int CountDroidStats(uint item, DroidStatSubType skill)
        {
            var count = 0;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.DroidStat && GetItemPropertySubType(ip) == (int)skill)
                    count++;
            return count;
        }
    }

    public static partial class DroidInventoryEngineTests
    {
        [EngineTest("Migration gives a player's saved droid controller and CPU Unarmed and Twin Pistol, used through two resummons",
            Category = "DroidInventory", TimeoutSeconds = 120f)]
        public static Task WeaponSkillsPlayerController(EngineTestContext ctx) => VerifyDroidWeaponSkills(ctx, false);

        [EngineTest("Migration gives a banked droid controller and CPU Unarmed and Twin Pistol, used through two bank cycles",
            Category = "DroidInventory", TimeoutSeconds = 120f)]
        public static Task WeaponSkillsStoredController(EngineTestContext ctx) => VerifyDroidWeaponSkills(ctx, true);

        private static async Task VerifyDroidWeaponSkills(EngineTestContext ctx, bool stored)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var owner = player.Creature;
            var controller = await CreateController(ctx, owner);
            var cpu = await CreateLoot(ctx, owner, MigrationEngineTests.LegacyDroidCpu);
            var controllerId = GetObjectUUID(controller);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                MigrationEngineTests.StampLegacyDroidCpu(cpu);
                MigrationEngineTests.StampLegacyDroidController(controller, ObjectPlugin.Serialize(cpu));
                DestroyObject(cpu);
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(controller), "Acquiring an item never adds droid skills");
            });
            await ctx.WaitFrameAsync();

            PlayerAbilityFixture login = null;
            try
            {
                if (stored)
                {
                    controller = await DepositMigrateAndWithdraw(ctx, owner, controller, true);
                    MigrationEngineTests.AssertMigratedDroidWeaponSkills(ctx, controller, "Stored migration");
                }
                else
                {
                    SavedCharacterMigration.Upgraded upgraded = null;
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        upgraded = SavedCharacterMigration.Run(owner, DB.Get<Player>(player.Id), "_24_CorrectCarapaceBracers");
                        var character = SavedCharacterMigration.Load(ctx, upgraded);
                        MigrationEngineTests.AssertMigratedDroidWeaponSkills(ctx,
                            SavedCharacterMigration.FindItem(character, Droid.DroidControlItemResref), "Character migration");
                        var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_24_CorrectCarapaceBracers");
                        ctx.Assert(retried.File.SequenceEqual(upgraded.File), "A retry makes no further change");
                    });
                    login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 8f);
                    owner = login.Creature;
                    controller = SavedCharacterMigration.FindItem(owner, Droid.DroidControlItemResref);
                    // The fixture still holds the original controller, so the reloaded copy gets its own UUID.
                    controllerId = GetObjectUUID(controller);
                }

                for (var cycle = 1; cycle <= 2; cycle++)
                {
                    ctx.AssertEqual(1, Items(owner).Count(item => GetResRef(item) == Droid.DroidControlItemResref), $"Cycle {cycle}: exactly one controller");
                    ctx.AssertEqual(controllerId, GetObjectUUID(controller), $"Cycle {cycle}: the controller keeps its identity");
                    ctx.AssertEqual(owner, GetItemPossessor(controller), $"Cycle {cycle}: the owner keeps the controller");
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                        ctx.Assert(!LootStatRevisionMigration.MigratePlayerItems(controller), $"Cycle {cycle}: the live rule finds nothing left to add"));

                    var droid = await Spawn(ctx, owner, controller);
                    try
                    {
                        ctx.AssertEqual(MigrationEngineTests.LegacyControllerStaff, Skill.GetCreatureSkillRank(droid, SkillType.Unarmed),
                            $"Cycle {cycle}: the droid uses its Unarmed skill");
                        ctx.AssertEqual(0, Skill.GetCreatureSkillRank(droid, SkillType.TwinPistol), $"Cycle {cycle}: Twin Pistol stays at zero");
                        ctx.AssertEqual(MigrationEngineTests.LegacyControllerKatar, Skill.GetCreatureSkillRank(droid, SkillType.Katar),
                            $"Cycle {cycle}: the boosted Katar skill is unchanged");
                    }
                    finally
                    {
                        await Dismiss(ctx, owner, droid);
                    }

                    // Save and reload the controller: through the bank and the stored migration, or by resummoning.
                    if (stored)
                        controller = await DepositMigrateAndWithdraw(ctx, owner, controller, false);
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                        MigrationEngineTests.AssertMigratedDroidWeaponSkills(ctx, controller, $"Cycle {cycle} reload"));
                }
            }
            finally
            {
                login?.Dispose();
            }
        }

        /// <summary>Banks the controller, runs the stored-item migration twice, and withdraws it.</summary>
        private static async Task<uint> DepositMigrateAndWithdraw(EngineTestContext ctx, uint owner, uint controller, bool expectChange)
        {
            InventoryItem banked = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () => banked = Bank.DepositItem(owner, controller));
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(controller), 5f, "the controller to enter storage");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var before = DB.Get<InventoryItem>(banked.Id).Data;
                StoredItemSweepRunner.Run();
                var migrated = DB.Get<InventoryItem>(banked.Id).Data;
                ctx.AssertEqual(expectChange, migrated != before, "The stored migration changes only a legacy controller");
                StoredItemSweepRunner.Run();
                ctx.AssertEqual(migrated, DB.Get<InventoryItem>(banked.Id).Data, "Stored migration retry is stable");
                ctx.AssertEqual(string.Empty, Bank.WithdrawItem(owner, banked.Id), "The controller is withdrawn");
            });
            await ctx.WaitFrameAsync();
            var withdrawn = MigrationEngineTests.FindHeadgear(owner, Droid.DroidControlItemResref);
            ctx.Assert(GetIsObjectValid(withdrawn), "The withdrawn controller is carried");
            return withdrawn;
        }
    }
}
