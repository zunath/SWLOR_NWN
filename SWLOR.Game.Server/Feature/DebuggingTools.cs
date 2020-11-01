using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using Dialog = SWLOR.Game.Server.Service.Dialog;

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

        [NWNEventHandler("test4")]
        public static void DebugGiveXP()
        {
            var player = GetLastUsedBy();
            Skill.GiveSkillXP(player, SkillType.Longsword, 5000);
        }

        [NWNEventHandler("test6")]
        public static void IncreaseEnmityOnBoy()
        {
            var player = GetLastUsedBy();
            var boy = GetObjectByTag("ENMITY_TARGET");
            var lastAttacker = GetLastAttacker(player);

            Enmity.ModifyEnmity(boy, lastAttacker, 999);
        }

        [NWNEventHandler("test7")]
        public static void GiveEffect()
        {
            var player = GetLastUsedBy();
            StatusEffect.Apply(player, player, StatusEffectType.Invincible, 30.0f);
        }

        [NWNEventHandler("test9")]
        public static void OpenHomePurchaseMenu()
        {
            var player = GetLastUsedBy();

            Creature.AddFeatByLevel(player, Feat.PropertyTool, 1);

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
