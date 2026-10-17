using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NWN.Native.API;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CraftedFoodCheckpointEngineTests
    {
        private const string MigrationCheckpointVariable = "PLAYER_MIGRATION_VERSION";

        [EngineTest("Character exports retain food receipts and completed batches across repeated inventory reloads", Category = "CraftedFoodDelivery")]
        public static async Task ExportFoodClaimWithInventory(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyRepeatedExports(ctx, owner));
        }

        [EngineTest("Player login restores food checkpoints and clears absent or empty saved data", Category = "CraftedFoodDelivery")]
        public static async Task RestoreFoodClaimOnLogin(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyLogin(ctx, owner));
        }

        [EngineTest("Pre-checkpoint character exports clear stale food data while retaining migration checkpoints", Category = "CraftedFoodDelivery")]
        public static async Task LoadPreReceiptExport(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () => VerifyAbsentReceipt(ctx, owner));
        }

        private static unsafe void VerifyRepeatedExports(EngineTestContext ctx, uint owner)
        {
            var portion = CreateItemOnObject("esh_hotpot", owner);
            ctx.Assert(GetIsObjectValid(portion), "The inventory checkpoint includes a real food portion");
            SetItemCharges(portion, 1);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
            using var receiptName = new CExoString(CraftedFoodClaim.CheckpointVariable);
            using var recoveryName = new CExoString(CraftedFoodDelivery.RecoveryVariable);
            var recoveryData = CreateRecoveryBatch(ctx, owner);
            using var recovery = new CExoString(recoveryData);
            native.m_ScriptVars.SetString(recoveryName, recovery);
            using var versionName = new CExoString(MigrationCheckpointVariable);
            using var firstReceipt = new CExoString("{\"DeliveryId\":\"food-checkpoint-fixture\",\"Revision\":1,\"ItemIndexes\":[0]}");
            native.m_ScriptVars.SetString(receiptName, firstReceipt);
            native.m_ScriptVars.SetInt(versionName, 0);

            using var firstReload = new CNWSCreature(OBJECT_INVALID, 0, 1);
            ExportAndLoad(ctx, native, firstReload, firstReceipt.ToString(), recoveryData, 0);
            ctx.AssertEqual(1, CountPortions(ctx, firstReload), "The receipt and delivered portion survive the first reload together");

            using var secondReceipt = new CExoString("{\"DeliveryId\":\"food-checkpoint-fixture\",\"Revision\":2,\"ItemIndexes\":[0]}");
            firstReload.m_ScriptVars.SetString(receiptName, secondReceipt);
            firstReload.m_ScriptVars.SetInt(versionName, 15);
            using var secondReload = new CNWSCreature(OBJECT_INVALID, 0, 1);
            ExportAndLoad(ctx, firstReload, secondReload, secondReceipt.ToString(), recoveryData, 15);
            ctx.AssertEqual(1, CountPortions(ctx, secondReload), "A second save and reload retain exactly the same delivered portion");
        }

        private static unsafe void ExportAndLoad(EngineTestContext ctx, CNWSCreature source,
            CNWSCreature destination, string expectedReceipt, string expectedRecovery, int expectedVersion)
        {
            using var file = new CResGFF();
            using var root = new CResStruct();
            Export(ctx, source, file, root);
            var found = 0;
            using var empty = new CExoString(string.Empty);
            fixed (byte* field = Encoding.ASCII.GetBytes("FoodClaim\0"))
            {
                using var savedReceipt = file.ReadFieldCExoString(root, field, &found, empty);
                ctx.AssertEqual(expectedReceipt, savedReceipt.ToString(), "The native export contains the exact food receipt");
            }
            ctx.Assert(found != 0, "The food receipt is a dedicated saved string field");
            found = 0;
            fixed (byte* field = Encoding.ASCII.GetBytes("FoodBatch\0"))
            {
                using var savedRecovery = file.ReadFieldCExoString(root, field, &found, empty);
                ctx.AssertEqual(expectedRecovery, savedRecovery.ToString(), "The native export contains all completed food payloads");
            }
            ctx.Assert(found != 0, "The completed food batch is a dedicated saved string field");
            found = 0;
            fixed (byte* field = Encoding.ASCII.GetBytes("PlayerFileVer\0"))
                ctx.AssertEqual(expectedVersion, file.ReadFieldINT(root, field, &found, 0), "Migration checkpoint semantics are retained");
            ctx.AssertEqual(expectedVersion > 0, found != 0, "Only a positive migration version is saved");
            ctx.Assert(destination.LoadCreature(file, root, 0, 0, 0, 0) != 0, "The native character export reloads");
            AssertReceipt(ctx, destination, expectedReceipt);
            AssertRecovery(ctx, destination, expectedRecovery);
            using var versionName = new CExoString(MigrationCheckpointVariable);
            ctx.AssertEqual(expectedVersion, destination.m_ScriptVars.GetInt(versionName), "Reload restores the migration checkpoint");
        }

        private static unsafe void VerifyLogin(EngineTestContext ctx, uint owner)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
            using var receiptName = new CExoString(CraftedFoodClaim.CheckpointVariable);
            using var recoveryName = new CExoString(CraftedFoodDelivery.RecoveryVariable);
            var recoveryData = CreateRecoveryBatch(ctx, owner);
            using var recovery = new CExoString(recoveryData);
            using var receipt = new CExoString("{\"DeliveryId\":\"food-checkpoint-fixture\",\"Revision\":3,\"ItemIndexes\":[]}");
            using var stale = new CExoString("stale login receipt");
            foreach (var savedReceipt in new[] { receipt.ToString(), null, string.Empty })
            {
                native.m_ScriptVars.DestroyString(receiptName);
                native.m_ScriptVars.DestroyString(recoveryName);
                if (!string.IsNullOrEmpty(savedReceipt))
                {
                    native.m_ScriptVars.SetString(receiptName, receipt);
                    native.m_ScriptVars.SetString(recoveryName, recovery);
                }
                using var file = new CResGFF();
                using var root = new CResStruct();
                Export(ctx, native, file, root);
                if (savedReceipt == string.Empty)
                {
                    using var empty = new CExoString(string.Empty);
                    fixed (byte* field = Encoding.ASCII.GetBytes("FoodClaim\0"))
                        ctx.Assert(file.WriteFieldCExoString(root, empty, field) != 0, "Create an explicitly empty historical receipt");
                    fixed (byte* field = Encoding.ASCII.GetBytes("FoodBatch\0"))
                        ctx.Assert(file.WriteFieldCExoString(root, empty, field) != 0, "Create an explicitly empty historical batch");
                }

                using var restored = new CNWSCreature(OBJECT_INVALID, 0, 1);
                restored.m_ScriptVars.SetString(receiptName, stale);
                restored.m_ScriptVars.SetString(recoveryName, stale);
                // The player login path reads stats directly and bypasses LoadCreature.
                ctx.AssertEqual(0u, restored.m_pStats.ReadStatsFromGff(file, root, restored.m_cAppearance, 0, 1, 0, 1),
                    "Native player stats load succeeds");
                AssertReceipt(ctx, restored, savedReceipt ?? string.Empty);
                AssertRecovery(ctx, restored, !string.IsNullOrEmpty(savedReceipt) ? recoveryData : string.Empty);
            }
        }

        private static unsafe void VerifyAbsentReceipt(EngineTestContext ctx, uint owner)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
            using var receiptName = new CExoString(CraftedFoodClaim.CheckpointVariable);
            using var recoveryName = new CExoString(CraftedFoodDelivery.RecoveryVariable);
            using var versionName = new CExoString(MigrationCheckpointVariable);
            native.m_ScriptVars.DestroyString(receiptName);
            native.m_ScriptVars.DestroyString(recoveryName);
            native.m_ScriptVars.SetInt(versionName, 15);
            using var file = new CResGFF();
            using var root = new CResStruct();
            Export(ctx, native, file, root);
            fixed (byte* field = Encoding.ASCII.GetBytes("FoodClaim\0"))
                ctx.AssertEqual(uint.MaxValue, file.GetFieldByLabel(root, field), "Exports without a receipt omit the optional field");
            fixed (byte* field = Encoding.ASCII.GetBytes("FoodBatch\0"))
                ctx.AssertEqual(uint.MaxValue, file.GetFieldByLabel(root, field), "Exports without a recovery batch omit the optional field");
            using var restored = new CNWSCreature(OBJECT_INVALID, 0, 1);
            using var stale = new CExoString("stale character receipt");
            restored.m_ScriptVars.SetString(receiptName, stale);
            restored.m_ScriptVars.SetString(recoveryName, stale);
            ctx.Assert(restored.LoadCreature(file, root, 0, 0, 0, 0) != 0, "A pre-receipt export reloads");
            AssertReceipt(ctx, restored, string.Empty);
            AssertRecovery(ctx, restored, string.Empty);
            ctx.AssertEqual(15, restored.m_ScriptVars.GetInt(versionName), "Missing food data does not change the migration checkpoint");
        }

        private static unsafe void Export(EngineTestContext ctx, CNWSCreature source, CResGFF file, CResStruct root)
        {
            using var type = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            ctx.Assert(file.CreateGFFFile(root, type, format) != 0, "Create the character export");
            var wasPlayer = source.m_bPlayerCharacter;
            try
            {
                source.m_bPlayerCharacter = 1;
                ctx.Assert(source.SaveCreature(file, root, 0, 0, 1, 0) != 0, "Native player export succeeds");
            }
            finally
            {
                source.m_bPlayerCharacter = wasPlayer;
            }
            fixed (byte* field = Encoding.ASCII.GetBytes("VarTable\0"))
                ctx.AssertEqual(uint.MaxValue, file.GetFieldByLabel(root, field), "BIC exports omit ordinary creature locals");
        }

        private static void AssertReceipt(EngineTestContext ctx, CNWSCreature creature, string expected)
        {
            using var name = new CExoString(CraftedFoodClaim.CheckpointVariable);
            using var actual = creature.m_ScriptVars.GetString(name);
            ctx.AssertEqual(expected, actual.ToString(), "Loading restores or clears the exact food receipt");
        }

        private static string CreateRecoveryBatch(EngineTestContext ctx, uint owner)
        {
            var storage = CreateObject(ObjectType.Placeable, "craft_temp_store", GetLocation(owner));
            ctx.Track(storage);
            ctx.Assert(GetIsObjectValid(storage) && GetHasInventory(storage), "Create completed food assembly storage");
            var batch = new CraftedFoodDelivery { PlayerId = GetObjectUUID(owner) };
            for (var index = 0; index < 4; index++)
            {
                var item = CreateItemOnObject("esh_hotpot", storage);
                ctx.Assert(GetIsObjectValid(item) && GetItemPossessor(item) == storage, "Each completed portion exists in assembly storage");
                SetLocalInt(item, "FOOD_CHECKPOINT_PORTION", index + 1);
                SetItemCharges(item, 1);
                var payload = ObjectPlugin.Serialize(item);
                ctx.Assert(!string.IsNullOrEmpty(payload), "Each completed portion has a real native item payload");
                batch.Items.Add(payload);
            }
            return JsonConvert.SerializeObject(batch);
        }

        private static void AssertRecovery(EngineTestContext ctx, CNWSCreature creature, string expected)
        {
            using var name = new CExoString(CraftedFoodDelivery.RecoveryVariable);
            using var actual = creature.m_ScriptVars.GetString(name);
            ctx.AssertEqual(expected, actual.ToString(), "Loading restores or clears the exact completed food batch");
            if (!string.IsNullOrEmpty(expected))
            {
                var batch = JsonConvert.DeserializeObject<CraftedFoodDelivery>(actual.ToString());
                ctx.AssertEqual(4, batch.Items.Count, "The recovered batch retains all four completed item payloads");
                ctx.Assert(batch.Items.TrueForAll(payload => !string.IsNullOrEmpty(payload)), "Every recovered portion retains its serialized item");
            }
        }

        private static int CountPortions(EngineTestContext ctx, CNWSCreature creature)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(creature.m_idSelf); GetIsObjectValid(item); item = GetNextItemInInventory(creature.m_idSelf))
            {
                if (GetResRef(item) != "esh_hotpot")
                    continue;
                count++;
                ctx.AssertEqual(creature.m_idSelf, GetItemPossessor(item), "The reloaded food portion belongs to the reloaded creature");
                ctx.AssertEqual(1, GetItemStackSize(item), "The food portion remains independently consumable");
                ctx.AssertEqual(1, GetItemCharges(item), "The food portion retains its remaining use");
            }
            return count;
        }
    }
}
