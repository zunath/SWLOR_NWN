using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerListEngineTests
    {
        /// <summary>
        /// Flags belong to the identity a player shows. A disguise keeps its own set, and switching
        /// between identities must neither lose nor leak either set across save and reload.
        /// </summary>
        [EngineTest("Player list flags persist separately for a character and their disguise", Category = "PlayerList", TimeoutSeconds = 45f)]
        public static async Task FlagsPersistPerIdentity(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var disguise = new PlayerDisguise
            {
                PlayerId = fixture.Id,
                PrivateName = "Engine test disguise",
                Descriptor = "A masked engine tester",
            };
            DB.Set(disguise);

            try
            {
                for (var cycle = 1; cycle <= 2; cycle++)
                {
                    PlayerList.SetFlag(player, PlayerListFlagType.GeneralRP, true);
                    PlayerList.SetFlag(player, PlayerListFlagType.Crafting, true);
                    PlayerList.SetFlag(player, PlayerListFlagType.Crafting, true);
                    AssertFlags(ctx, DB.Get<Player>(fixture.Id).PlayerListFlags, $"cycle {cycle}: saved character flags",
                        PlayerListFlagType.GeneralRP, PlayerListFlagType.Crafting);

                    fixture.Update(record => record.ActiveDisguiseId = disguise.Id);
                    AssertFlags(ctx, PlayerList.GetFlags(player), $"cycle {cycle}: a disguise starts without the character's flags");

                    PlayerList.SetFlag(player, PlayerListFlagType.PvP, true);
                    AssertFlags(ctx, DB.Get<PlayerDisguise>(disguise.Id).PlayerListFlags, $"cycle {cycle}: saved disguise flags",
                        PlayerListFlagType.PvP);
                    AssertFlags(ctx, DB.Get<Player>(fixture.Id).PlayerListFlags, $"cycle {cycle}: disguise flags leave the character's alone",
                        PlayerListFlagType.GeneralRP, PlayerListFlagType.Crafting);

                    var disguised = PlayerList.BuildSubjects().Single(subject => subject.Player == player);
                    AssertFlags(ctx, disguised.Flags, $"cycle {cycle}: the list shows the disguise's flags", PlayerListFlagType.PvP);
                    ctx.AssertEqual(disguise.Descriptor, disguised.Descriptor, $"cycle {cycle}: the list shows the disguise's descriptor");
                    ctx.Assert(disguised.IsDisguised, $"cycle {cycle}: the subject is marked disguised");

                    fixture.Update(record => record.ActiveDisguiseId = string.Empty);
                    AssertFlags(ctx, PlayerList.GetFlags(player), $"cycle {cycle}: removing the disguise restores the character's flags",
                        PlayerListFlagType.GeneralRP, PlayerListFlagType.Crafting);

                    PlayerList.SetFlag(player, PlayerListFlagType.GeneralRP, false);
                    PlayerList.SetFlag(player, PlayerListFlagType.Crafting, false);
                    AssertFlags(ctx, DB.Get<Player>(fixture.Id).PlayerListFlags, $"cycle {cycle}: cleared character flags are saved");

                    fixture.Update(record => record.ActiveDisguiseId = disguise.Id);
                    PlayerList.SetFlag(player, PlayerListFlagType.PvP, false);
                    AssertFlags(ctx, DB.Get<PlayerDisguise>(disguise.Id).PlayerListFlags, $"cycle {cycle}: cleared disguise flags are saved");
                    fixture.Update(record => record.ActiveDisguiseId = string.Empty);
                }
            }
            finally
            {
                DB.Delete<PlayerDisguise>(disguise.Id);
            }
        }

        /// <summary>
        /// Players see their own name, the names they assigned, and descriptors; never another player's
        /// character name.
        /// </summary>
        [EngineTest("Player list shows players only the names they assigned", Category = "PlayerList", TimeoutSeconds = 45f)]
        public static async Task NamesFollowTheViewersAssignments(EngineTestContext ctx)
        {
            using var viewer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var other = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var unnamed = FindEntry(ctx, viewer.Creature, other.Creature);
            ctx.AssertEqual(string.Empty, unnamed.Name, "an unnamed player has no name in the list");
            ctx.AssertEqual(Disguise.GetDisplayDescriptor(other.Creature), unnamed.Descriptor, "an unnamed player shows their public descriptor");
            ctx.AssertEqual(GetName(viewer.Creature), FindEntry(ctx, viewer.Creature, viewer.Creature).Name, "the viewer sees their own name");

            try
            {
                PlayerName.SetKnownName(viewer.Creature, other.Creature, "Engine Testname");
                var named = FindEntry(ctx, viewer.Creature, other.Creature);
                ctx.AssertEqual("Engine Testname", named.Name, "an assigned name is shown");
                ctx.Assert(PlayerListRules.MatchesSearch("testn", named.Name, named.Descriptor), "the assigned name is searchable");
                ctx.Assert(!PlayerListRules.MatchesSearch(GetName(other.Creature), named.Name, named.Descriptor),
                    "the character's real name is not searchable");

                // The cached Settings values last for one script context.
                viewer.Update(record =>
                {
                    record.Settings.ShowDescriptorsForNamedPlayers = false;
                    record.Settings.ShowOwnDescriptor = false;
                });
                await ctx.WaitFrameAsync();
                var hidden = FindEntry(ctx, viewer.Creature, other.Creature);
                ctx.AssertEqual(string.Empty, hidden.Descriptor, "a hidden descriptor is not shown beside an assigned name");
                var descriptorWord = Disguise.GetDisplayDescriptor(other.Creature).Split(' ').Last();
                ctx.Assert(!PlayerListRules.MatchesSearch(descriptorWord, hidden.Name, hidden.Descriptor),
                    $"a hidden descriptor is not searchable ('{descriptorWord}')");
                ctx.AssertEqual(string.Empty, FindEntry(ctx, viewer.Creature, viewer.Creature).Descriptor,
                    "the viewer's own descriptor follows their setting");
                viewer.Update(record =>
                {
                    record.Settings.ShowDescriptorsForNamedPlayers = true;
                    record.Settings.ShowOwnDescriptor = true;
                });
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(Disguise.GetDisplayDescriptor(other.Creature), FindEntry(ctx, viewer.Creature, other.Creature).Descriptor,
                    "turning the setting back on shows the descriptor again");
            }
            finally
            {
                PlayerName.ForgetKnownName(viewer.Creature, other.Creature);
            }

            ctx.AssertEqual(string.Empty, FindEntry(ctx, viewer.Creature, other.Creature).Name, "forgetting the name returns the player to Unassigned");
        }

        /// <summary>
        /// Mark Hostile and Clear Hostile change the viewer's PvP attitude, which the list reads back.
        /// </summary>
        [EngineTest("Player list hostility follows the viewer's PvP attitude", Category = "PlayerList", TimeoutSeconds = 45f)]
        public static async Task HostilityFollowsAttitude(EngineTestContext ctx)
        {
            using var viewer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var other = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            ctx.Assert(!PlayerList.IsHostileTo(viewer.Creature, other.Creature), "players start out not hostile");

            try
            {
                for (var cycle = 1; cycle <= 2; cycle++)
                {
                    await ctx.ExecuteInCreatureContextAsync(viewer.Creature, () => PlayerList.SetHostile(viewer.Creature, other.Creature, true));
                    ctx.Assert(PlayerList.IsHostileTo(viewer.Creature, other.Creature), $"cycle {cycle}: Mark Hostile makes the viewer hostile");
                    ctx.Assert(PlayerList.GetRelation(viewer.Creature, other.Creature).ViewerDislikesTarget,
                        $"cycle {cycle}: the relation reports hostility");
                    ctx.AssertEqual(PlayerListStatus.Hostile, FindEntry(ctx, viewer.Creature, other.Creature).Status,
                        $"cycle {cycle}: the list marks the player Hostile");
                    ctx.Log($"cycle {cycle}: after Mark Hostile the other player is hostile back: {PlayerList.IsHostileTo(other.Creature, viewer.Creature)}");

                    await ctx.ExecuteInCreatureContextAsync(viewer.Creature, () => PlayerList.SetHostile(viewer.Creature, other.Creature, false));
                    ctx.Assert(!PlayerList.IsHostileTo(viewer.Creature, other.Creature), $"cycle {cycle}: Clear Hostile ends the viewer's hostility");
                    ctx.AssertEqual(PlayerListStatus.None, FindEntry(ctx, viewer.Creature, other.Creature).Status,
                        $"cycle {cycle}: the list clears the Hostile marker");
                    ctx.Log($"cycle {cycle}: after Clear Hostile the other player is hostile back: {PlayerList.IsHostileTo(other.Creature, viewer.Creature)}");
                }
            }
            finally
            {
                SetPCLike(viewer.Creature, other.Creature);
                SetPCLike(other.Creature, viewer.Creature);
            }
        }

        /// <summary>
        /// The list reads party membership and leadership from the engine, and its controls change both
        /// the engine party and the Party service's caches.
        /// </summary>
        [EngineTest("Player list party controls follow the engine party", Category = "PlayerList", TimeoutSeconds = 45f)]
        public static async Task PartyControlsFollowTheEngineParty(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            using var member = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var solo = PlayerList.GetRelation(leader.Creature, member.Creature);
            ctx.Assert(!solo.ViewerIsInParty && !solo.TargetIsInOtherParty, "both players start without a party");
            ctx.Assert(PlayerListRules.Evaluate(solo).Invite.IsEnabled, "a solo player can invite another solo player");

            for (var cycle = 1; cycle <= 2; cycle++)
            {
                await ctx.ExecuteInCreatureContextAsync(member.Creature, () => AddToParty(member.Creature, leader.Creature));
                await ctx.WaitUntilAsync(() => GetFactionLeader(member.Creature) == leader.Creature, 5f,
                    $"cycle {cycle}: the member to join the leader's party");
                // Accepting an invitation fills the Party caches; a script-driven join skips that handler.
                await ctx.ExecuteInCreatureContextAsync(member.Creature, () =>
                    typeof(Party).GetMethod("AddToParty", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { leader.Creature, member.Creature }));
                ctx.Assert(Party.IsInParty(leader.Creature, member.Creature), $"cycle {cycle}: the Party service groups them");

                var asLeader = PlayerList.GetRelation(leader.Creature, member.Creature);
                ctx.Assert(asLeader.ViewerIsPartyLeader && asLeader.TargetIsInViewerParty, $"cycle {cycle}: the leader sees their member");
                ctx.AssertEqual(PlayerListStatus.Party, FindEntry(ctx, leader.Creature, member.Creature).Status, $"cycle {cycle}: the member is marked Party");
                ctx.AssertEqual(PlayerListStatus.Leader, FindEntry(ctx, member.Creature, leader.Creature).Status, $"cycle {cycle}: the leader is marked Leader");
                var leaderActions = PlayerListRules.Evaluate(asLeader);
                ctx.Assert(leaderActions.Remove.IsEnabled && leaderActions.MakeLeader.IsEnabled, $"cycle {cycle}: the leader can manage the member");

                var transferred = false;
                await ctx.ExecuteInCreatureContextAsync(leader.Creature, () => transferred = PlayerList.MakeLeader(leader.Creature, member.Creature));
                ctx.Assert(transferred, $"cycle {cycle}: the engine accepts the leadership transfer");
                await ctx.WaitUntilAsync(() => GetFactionLeader(leader.Creature) == member.Creature, 5f,
                    $"cycle {cycle}: leadership to pass to the member");
                ctx.Assert(PlayerList.GetRelation(member.Creature, leader.Creature).ViewerIsPartyLeader, $"cycle {cycle}: the member now leads");

                await ctx.ExecuteInCreatureContextAsync(member.Creature, () => PlayerList.RemoveFromParty(leader.Creature));
                await ctx.WaitUntilAsync(() => !GetFactionEqual(leader.Creature, member.Creature), 5f,
                    $"cycle {cycle}: the former leader to be removed from the party");
                var afterRemoval = PlayerList.GetRelation(member.Creature, leader.Creature);
                ctx.Assert(!afterRemoval.ViewerIsInParty && !afterRemoval.TargetIsInOtherParty, $"cycle {cycle}: both players are solo again");
                ctx.Assert(!Party.IsInParty(member.Creature, leader.Creature), $"cycle {cycle}: the Party service no longer groups them");
            }
        }

        /// <summary>
        /// DMs are listed only to other DMs. A player's list, its count, its action checks, and tells
        /// all treat a DM as absent.
        /// </summary>
        [EngineTest("Player list shows DMs only to other DMs", Category = "PlayerList", TimeoutSeconds = 45f)]
        public static async Task DMsAreHiddenFromPlayers(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            using var otherDM = await PlayerAbilityFixture.CreateAsync(ctx, 4f);

            var restoreDM = InstallNativeDM(ctx, dm.Creature);
            var restoreOtherDM = InstallNativeDM(ctx, otherDM.Creature);
            try
            {
                var subjects = PlayerList.BuildSubjects();
                ctx.Assert(subjects.Single(subject => subject.Player == dm.Creature).IsDM, "the DM is recognized as a DM");

                var playerRows = PlayerList.BuildEntries(player.Creature, subjects);
                ctx.Assert(playerRows.All(row => row.Player != dm.Creature && row.Player != otherDM.Creature),
                    "a player's list has no DMs");
                ctx.Assert(playerRows.All(row => !row.Relation.TargetIsDM && row.Status != PlayerListStatus.DM),
                    "no row in a player's list is marked DM");
                ctx.AssertEqual(subjects.Count - 2, playerRows.Count, "a player's online count leaves DMs out");

                var hiddenRelation = PlayerList.GetRelation(player.Creature, dm.Creature);
                ctx.Assert(!hiddenRelation.HasTarget && !hiddenRelation.TargetIsDM, "a player gets no relation to a DM");
                ctx.Assert(!PlayerListRules.Evaluate(hiddenRelation).SendTell.IsEnabled, "a player cannot act on a DM");
                ctx.AssertEqual("That player is no longer online.", PlayerList.SendTell(player.Creature, dm.Creature, "Hello"),
                    "a tell to a DM is refused as if they were offline");

                var dmRow = PlayerList.BuildEntries(otherDM.Creature, subjects).SingleOrDefault(row => row.Player == dm.Creature);
                ctx.Assert(dmRow != null, "another DM sees the DM");
                ctx.AssertEqual(PlayerListStatus.DM, dmRow.Status, "the DM is marked DM for other DMs");
                ctx.AssertEqual(PlayerList.DMDescriptor, dmRow.Descriptor, "the DM shows the DM descriptor");
                ctx.AssertEqual(GetName(dm.Creature), dmRow.Name, "other DMs see the DM's real name");
                ctx.Assert(PlayerList.BuildEntries(otherDM.Creature, subjects).Any(row => row.Player == player.Creature),
                    "DMs still see players");

                ctx.Assert(!PlayerListRules.Evaluate(PlayerList.GetRelation(player.Creature, otherDM.Creature)).JumpTo.IsEnabled,
                    "a player cannot jump");
                var dmToPlayer = PlayerList.GetRelation(otherDM.Creature, player.Creature);
                ctx.Assert(PlayerListRules.Evaluate(dmToPlayer).JumpTo.IsEnabled, "a DM can jump to a selected player");
                await ctx.ExecuteInCreatureContextAsync(otherDM.Creature, () => PlayerList.JumpTo(otherDM.Creature, player.Creature));
                await ctx.WaitUntilAsync(() => GetDistanceBetween(otherDM.Creature, player.Creature) < 2f, 5f,
                    "the DM to arrive at the selected player");
            }
            finally
            {
                restoreOtherDM();
                restoreDM();
            }
        }

        /// <summary>
        /// A possessing DM's window is bound to the possessed creature while the roster lists their avatar.
        /// The avatar row must still be their own: marked as self, with no tell or jump to themselves.
        /// </summary>
        [EngineTest("Player list treats a possessing DM's avatar as their own row", Category = "PlayerList", TimeoutSeconds = 45f)]
        public static async Task PossessingDMSeesTheirOwnRow(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var npc = ctx.SpawnCreature("civilian", 4f);
            await ctx.WaitFrameAsync();

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var client = server.GetClientObjectByObjectId(dm.Creature);
            var nativeDM = server.GetCreatureByGameObjectID(dm.Creature);
            var nativeNpc = server.GetCreatureByGameObjectID(npc);
            var originalAssociate = nativeNpc.m_nAssociateType;
            var originalMaster = nativeNpc.m_oidMaster;
            var restoreDM = InstallNativeDM(ctx, dm.Creature);
            try
            {
                // NWNX AssociateType::DMPossess, installed without moving a synthetic client between areas.
                nativeNpc.m_nAssociateType = 7;
                nativeNpc.m_oidMaster = dm.Creature;
                client.SetGameObject(nativeNpc);
                ctx.Assert(GetIsDMPossessed(npc), "native GetIsDMPossessed recognizes the possessed creature");

                var ownRow = PlayerList.BuildEntries(npc, PlayerList.BuildSubjects()).SingleOrDefault(row => row.Player == dm.Creature);
                ctx.Assert(ownRow != null, "the possessing DM sees their avatar row");
                ctx.Assert(ownRow.Relation.TargetIsSelf, "the avatar row is the possessing DM's own row");
                ctx.AssertEqual(PlayerListStatus.DM, ownRow.Status, "the avatar row is marked DM");

                var ownActions = PlayerListRules.Evaluate(PlayerList.GetRelation(npc, dm.Creature));
                ctx.Assert(!ownActions.SendTell.IsEnabled, "the possessing DM cannot send a tell to themselves");
                ctx.Assert(!ownActions.JumpTo.IsEnabled, "the possessing DM cannot jump to themselves");
                ctx.Assert(PlayerListRules.Evaluate(PlayerList.GetRelation(npc, player.Creature)).JumpTo.IsEnabled,
                    "the possessing DM can still jump to a player");
                ctx.Assert(PlayerList.BuildEntries(player.Creature, PlayerList.BuildSubjects()).All(row => row.Player != dm.Creature),
                    "players still do not see the possessing DM");
            }
            finally
            {
                client.SetGameObject(nativeDM);
                nativeNpc.m_nAssociateType = originalAssociate;
                nativeNpc.m_oidMaster = originalMaster;
                restoreDM();
            }
        }

        /// <summary>
        /// The client keeps a NUI window open across possession, so an open player list must be reopened on
        /// whatever the DM controls next or it keeps acting for the object it opened on.
        /// </summary>
        [EngineTest("Player list follows a DM into and out of possession", Category = "PlayerList", TimeoutSeconds = 60f)]
        public static async Task WindowFollowsDMPossession(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var npc = ctx.SpawnCreature("civilian", 4f);
            await ctx.WaitFrameAsync();

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var client = server.GetClientObjectByObjectId(dm.Creature);
            var nativeDM = server.GetCreatureByGameObjectID(dm.Creature);
            var nativeNpc = server.GetCreatureByGameObjectID(npc);
            var originalAssociate = nativeNpc.m_nAssociateType;
            var originalMaster = nativeNpc.m_oidMaster;
            var originalPlayerFlag = nativeDM.m_bPlayerCharacter;
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            ctx.Assert(!windows.ContainsKey(dm.Id), "the DM fixture has no existing GUI registration");
            var window = Gui.GetWindowTemplate(GuiWindowType.PlayerList).CreatePlayerWindowAction();
            windows.Add(dm.Id, new Dictionary<GuiWindowType, GuiPlayerWindow> { [GuiWindowType.PlayerList] = window });
            var model = (PlayerListViewModel)window.ViewModel;
            // Login normally seeds the saved geometry; Bind reads it before applying the default.
            model.Geometry = new GuiRectangle(0f, 0f, 610f, 620f);
            var restoreDM = InstallNativeDM(ctx, dm.Creature);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () => DMPresence.ToggleWindow(dm.Creature, GuiWindowType.PlayerList));
                await ctx.WaitFrameAsync();
                ctx.Assert(window.WindowToken > 0, "the DM opens a real player list window");
                ctx.AssertEqual(dm.Creature, GetWindowOwner(model), "the window opens on the avatar");

                // Possessing starts on the avatar, and the engine settles it within the same tick. A possessing
                // DM's avatar stops reporting GetIsPC, which is how DMPresence finds the creature.
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                {
                    PlayerList.MoveWindowOnPossession();
                    nativeNpc.m_nAssociateType = 7; // NWNX AssociateType::DMPossess.
                    nativeNpc.m_oidMaster = dm.Creature;
                    nativeDM.m_bPlayerCharacter = 0;
                    client.SetGameObject(nativeNpc);
                });
                ctx.Assert(DMPresence.GetControlledCreature(dm.Creature) == npc, "DMPresence sees the DM controlling the creature");
                await ctx.WaitUntilAsync(() => GetWindowOwner(model) == npc, 5f, "the window to follow the DM into the creature");
                ctx.Assert(model.IsDMViewer, "the possessing DM still sees the staff view");
                ctx.Assert(model.RowNames.Any(name => name.EndsWith("(You)")), "the possessing DM still has their own row");

                // Returning to the avatar starts on the possessed creature.
                await ctx.ExecuteInCreatureContextAsync(npc, () =>
                {
                    PlayerList.MoveWindowOnPossession();
                    client.SetGameObject(nativeDM);
                    nativeDM.m_bPlayerCharacter = originalPlayerFlag;
                    nativeNpc.m_nAssociateType = originalAssociate;
                    nativeNpc.m_oidMaster = originalMaster;
                });
                await ctx.WaitUntilAsync(() => GetWindowOwner(model) == dm.Creature, 5f, "the window to return to the avatar");
                ctx.Assert(model.IsDMViewer, "the DM sees the staff view on the avatar again");
            }
            finally
            {
                if (window.WindowToken > 0)
                    NuiDestroy(GetWindowOwner(model), window.WindowToken);
                client.SetGameObject(nativeDM);
                nativeDM.m_bPlayerCharacter = originalPlayerFlag;
                nativeNpc.m_nAssociateType = originalAssociate;
                nativeNpc.m_oidMaster = originalMaster;
                restoreDM();
                windows.Remove(dm.Id);
            }
        }

        private static uint GetWindowOwner(PlayerListViewModel model)
        {
            return (uint)model.GetType()
                .GetProperty("Player", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(model);
        }

        /// <summary>
        /// Invite sends the native party invitation from the viewer to the selected player, which the
        /// invited client answers through the engine's own accept and decline popup.
        /// </summary>
        [EngineTest("Player list invitations reach the native party invite", Category = "PlayerList", TimeoutSeconds = 30f)]
        public static async Task InviteSendsTheNativeInvitation(EngineTestContext ctx)
        {
            using var inviter = await PlayerAbilityFixture.CreateAsync(ctx);
            using var invited = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var invitedCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(invited.Creature);
            ctx.Assert(invitedCreature.m_bInvitedToParty == 0, "the player starts without an invitation");

            try
            {
                await ctx.ExecuteInCreatureContextAsync(inviter.Creature, () => PlayerList.Invite(inviter.Creature, invited.Creature));
                ctx.Assert(invitedCreature.m_bInvitedToParty != 0, "the selected player has a pending invitation");
                ctx.AssertEqual(inviter.Creature, invitedCreature.m_oidInvitedToPartyBy, "the invitation comes from the viewer");
            }
            finally
            {
                invitedCreature.m_bInvitedToParty = 0;
                invitedCreature.m_oidInvitedToPartyBy = OBJECT_INVALID;
            }
        }

        // Installs a real native DM identity on a headless fixture (as AppearanceEditorDmEngineTests does)
        // and returns the action that restores the original fields.
        private static Action InstallNativeDM(EngineTestContext ctx, uint creature)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var client = server.GetClientObjectByObjectId(creature);
            var playerInfo = server.GetNetLayer().GetPlayerInfo(client.m_nPlayerID);
            var nativeCreature = server.GetCreatureByGameObjectID(creature);
            var originalCharacterType = client.m_nCharacterType;
            var originalDMFile = nativeCreature.m_pStats.m_bIsDMCharacterFile;
            var originalPrivileges = playerInfo.m_bGameMasterPrivileges;
            var originalPlayerLogin = playerInfo.m_bGameMasterIsPlayerLogin;

            // NWNX's CharacterType::DM is 2.
            client.m_nCharacterType = 2;
            nativeCreature.m_pStats.m_bIsDMCharacterFile = 1;
            playerInfo.m_bGameMasterPrivileges = 1;
            playerInfo.m_bGameMasterIsPlayerLogin = 0;
            ctx.Assert(GetIsDM(creature), "native GetIsDM recognizes the fixture");

            return () =>
            {
                client.m_nCharacterType = originalCharacterType;
                nativeCreature.m_pStats.m_bIsDMCharacterFile = originalDMFile;
                playerInfo.m_bGameMasterPrivileges = originalPrivileges;
                playerInfo.m_bGameMasterIsPlayerLogin = originalPlayerLogin;
            };
        }

        private static PlayerListEntry FindEntry(EngineTestContext ctx, uint viewer, uint target)
        {
            var entry = PlayerList.BuildEntries(viewer, PlayerList.BuildSubjects()).SingleOrDefault(row => row.Player == target);
            ctx.Assert(entry != null, "the target appears in the viewer's list");
            return entry;
        }

        private static void AssertFlags(
            EngineTestContext ctx,
            IEnumerable<PlayerListFlagType> actual,
            string label,
            params PlayerListFlagType[] expected)
        {
            var actualSet = (actual ?? Enumerable.Empty<PlayerListFlagType>()).ToList();
            ctx.Assert(actualSet.Count == expected.Length && expected.All(actualSet.Contains),
                $"{label}: expected [{string.Join(", ", expected)}] but was [{string.Join(", ", actualSet)}].");
        }
    }
}
