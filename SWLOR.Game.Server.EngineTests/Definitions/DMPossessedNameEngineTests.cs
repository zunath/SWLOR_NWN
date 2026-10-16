using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DMPossessedNameEngineTests
    {
        [EngineTest("Possessed NPC names survive arriving observers and chat", Category = "DMPossessedName", TimeoutSeconds = 45f)]
        public static async Task CustomNameSurvivesNameOverrides(EngineTestContext ctx)
        {
            using var target = await PlayerAbilityFixture.CreateAsync(ctx);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(target.Creature);
            var originalAssociateType = native.m_nAssociateType;
            var originalPlayerFlag = native.m_bPlayerCharacter;
            try
            {
                // SetName rejects PCs. Rename before applying the native possession state.
                native.m_bPlayerCharacter = 0;
                SetName(target.Creature, "Fat Mac");
                native.m_bPlayerCharacter = originalPlayerFlag;
                // NWNX Rename treats associate types 7 and 8 as DM possession.
                foreach (ushort associateType in new ushort[] { 7, 8 })
                {
                    native.SetAssociateType(associateType);
                    ctx.Assert(GetIsDMPossessed(target.Creature), "fixture follows the native DM possession branch");
                    ctx.Assert(GetIsPC(target.Creature) && !GetIsDM(target.Creature), "possessed NPC reaches the player branch unless explicitly excluded");
                    using var observer = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
                    PlayerName.RefreshNameOverridesForPlayer(observer.Creature);
                    PlayerName.RefreshNameOverridesForObserver(observer.Creature);
                    PlayerName.RefreshNameOverridesForPlayer(target.Creature);
                    typeof(PlayerName).GetMethod("ApplyTrueNameOverride", BindingFlags.NonPublic | BindingFlags.Static)!
                        .Invoke(null, new object[] { observer.Creature, target.Creature });

                    ctx.AssertEqual("Fat Mac", PlayerName.GetDisplayName(observer.Creature, target.Creature), "arriving observer sees the custom NPC name");
                    ctx.AssertEqual("Fat Mac", PlayerName.GetChatDisplayName(observer.Creature, target.Creature), "chat resolves the custom NPC name");
                    ctx.AssertEqual("Known names may only target player characters.",
                        PlayerName.ValidateKnownNameAssignment(observer.Creature, target.Creature, "Republic Soldier"), "NPC cannot acquire a private player identity");
                    PlayerName.SendChatMessageWithChatNameOverride(observer.Creature, target.Creature, () =>
                        ctx.AssertEqual(string.Empty, RenamePlugin.GetPCNameOverride(target.Creature, observer.Creature), "chat cannot install a player override on the NPC"));
                    ctx.AssertEqual(string.Empty, RenamePlugin.GetPCNameOverride(target.Creature, observer.Creature), "observer and staff refreshes cannot install a player override");
                    ctx.AssertEqual(string.Empty, RenamePlugin.GetPCNameOverride(target.Creature), "NPC has no default player override");
                    ctx.AssertEqual("Fat Mac", GetName(target.Creature), "native custom name remains after refresh and chat");
                    ctx.Assert(!PlayerName.ToggleAfk(target.Creature), "NPC cannot acquire an AFK player override");
                }
            }
            finally
            {
                native.SetAssociateType(originalAssociateType);
                native.m_bPlayerCharacter = originalPlayerFlag;
            }
        }
    }
}
