using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using Dialog = SWLOR.Game.Server.Service.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void DebugSpawnCreature()
        {
            var location = GetLocation(GetWaypointByTag("DEATH_DEFAULT_RESPAWN_POINT"));
            var spawn = CreateObject(ObjectType.Creature, "test_zombie", location);

            SetLocalInt(spawn, "QUEST_NPC_GROUP_ID", 1);
        }

        [NWNEventHandler("test6")]
        public static void IncreaseEnmityOnBoy()
        {
            var player = GetLastUsedBy();
            var boy = GetObjectByTag("ENMITY_TARGET");
            var lastAttacker = GetLastAttacker(player);

            Enmity.ModifyEnmity(boy, lastAttacker, 999);
        }

        [NWNEventHandler("test9")]
        public static void OpenHomePurchaseMenu()
        {
            var player = GetLastUsedBy();

            Creature.AddFeatByLevel(player, Feat.StructureManagementTool, 1);

            Dialog.StartConversation(player, OBJECT_SELF, nameof(PlayerHouseDialog));
        }

        [NWNEventHandler("test10")]
        public static void SpawnGold()
        {
            var player = GetLastUsedBy();
            GiveGoldToCreature(player, 5000);
        }

        [NWNEventHandler("test11")]
        public static void DisplayAchievementWindow()
        {
            Achievement.DisplayAchievementNotificationWindow(GetLastUsedBy(), "Test Achievement");
        }
    }
}
