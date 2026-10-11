using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Dungeon enemy groups preserve offline progress and saved NPCs beyond 50 records", Category = "DungeonEnemyGroups")]
    public static async Task DungeonEnemyGroupExchange(EngineTestContext ctx)
    {
        var creature = ctx.SpawnCreature("cp_sabstorm_ad");
        await ctx.WaitFrameAsync();
        var ids = Enumerable.Range(0, 65).Select(_ => Guid.NewGuid().ToString()).ToArray();
        ids[0] = GetObjectUUID(creature);
        var archiveId = Guid.NewGuid().ToString();
        const string questId = "saber_storm_foundation";
        const NPCGroupType oldGroup = NPCGroupType.Dantooine_SaberStorm_Adept;
        const NPCGroupType newGroup = NPCGroupType.Dantooine_StormDrillDroid;
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            ctx.AssertEqual((int)newGroup, GetLocalInt(creature, "QUEST_NPC_GROUP_ID"), "Deployed blueprint uses the new group");
            SetLocalInt(creature, "QUEST_NPC_GROUP_ID", (int)oldGroup);
            SetLocalInt(creature, "RETAINED_LOCAL", 73);
            var original = ObjectPlugin.Serialize(creature);
            try
            {
                foreach (var id in ids)
                {
                    var player = new Player(id);
                    player.Quests[questId] = new PlayerQuest
                    {
                        CurrentState = 1,
                        KillProgresses = new Dictionary<NPCGroupType, int> { [oldGroup] = 3 }
                    };
                    DB.Set(player);
                }
                DB.Set(new DMCreature { Id = archiveId, Name = "Retained name", Tag = "retained-tag", Data = original });
                var migration = new _26_ReplaceDungeonEnemyGroups();
                migration.Migrate();
                foreach (var id in ids)
                {
                    var quest = DB.Get<Player>(id).Quests[questId];
                    ctx.AssertEqual(3, quest.KillProgresses[newGroup], "Remaining kills survive for every player, including beyond row 50");
                    ctx.Assert(!quest.KillProgresses.ContainsKey(oldGroup), "Old counter is removed");
                    ctx.AssertEqual(1, quest.CurrentState, "Quest state is unchanged");
                }
                var archive = DB.Get<DMCreature>(archiveId);
                var restored = Deserialize(ctx, archive.Data);
                ctx.AssertEqual((int)newGroup, GetLocalInt(restored, "QUEST_NPC_GROUP_ID"), "Saved NPC group is migrated");
                ctx.AssertEqual(73, GetLocalInt(restored, "RETAINED_LOCAL"), "Unrelated creature state is retained");
                ctx.AssertEqual(GetName(creature), GetName(restored), "Creature name is retained");
                ctx.AssertEqual(GetResRef(creature), GetResRef(restored), "Creature template is retained");
                ctx.AssertEqual("Retained name", archive.Name, "Archive metadata is retained");
                var savedPlayer = JsonConvert.SerializeObject(DB.Get<Player>(ids[0]));
                var savedArchive = archive.Data;
                migration.Migrate();
                ctx.AssertEqual(savedPlayer, JsonConvert.SerializeObject(DB.Get<Player>(ids[0])), "Player retry is idempotent");
                ctx.AssertEqual(savedArchive, DB.Get<DMCreature>(archiveId).Data, "Archive retry is byte-for-byte unchanged");
                ctx.Assert(Quest.GetQuestsAssociatedWithNPCGroup(newGroup).Contains(questId), "Quest cache targets the new group");
                new KillTargetObjective(newGroup, 6).Advance(creature, questId);
                ctx.AssertEqual(2, DB.Get<Player>(ids[0]).Quests[questId].KillProgresses[newGroup],
                    "The next kill continues the migrated counter");
            }
            finally
            {
                foreach (var id in ids) DB.Delete<Player>(id);
                DB.Delete<DMCreature>(archiveId);
            }
        });
    }
}
