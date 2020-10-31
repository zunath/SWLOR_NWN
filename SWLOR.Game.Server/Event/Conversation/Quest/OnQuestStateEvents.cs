using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Event.Conversation.Quest
{
    public static class OnQuestStateEvents
    {
        [NWNEventHandler("on_qst1_state_1")]
        public static int OnQuest1State1()
        {
            return Check(1, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_1")]
        public static int OnQuest2State1()
        {
            return Check(2, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_1")]
        public static int OnQuest3State1()
        {
            return Check(3, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_1")]
        public static int OnQuest4State1()
        {
            return Check(4, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_1")]
        public static int OnQuest5State1()
        {
            return Check(5, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_1")]
        public static int OnQuest6State1()
        {
            return Check(6, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_1")]
        public static int OnQuest7State1()
        {
            return Check(7, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_1")]
        public static int OnQuest8State1()
        {
            return Check(8, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_1")]
        public static int OnQuest9State1()
        {
            return Check(9, 1) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_1")]
        public static int OnQuest10State1()
        {
            return Check(10, 1) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_2")]
        public static int OnQuest1State2()
        {
            return Check(1, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_2")]
        public static int OnQuest2State2()
        {
            return Check(2, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_2")]
        public static int OnQuest3State2()
        {
            return Check(3, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_2")]
        public static int OnQuest4State2()
        {
            return Check(4, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_2")]
        public static int OnQuest5State2()
        {
            return Check(5, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_2")]
        public static int OnQuest6State2()
        {
            return Check(6, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_2")]
        public static int OnQuest7State2()
        {
            return Check(7, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_2")]
        public static int OnQuest8State2()
        {
            return Check(8, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_2")]
        public static int OnQuest9State2()
        {
            return Check(9, 2) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_2")]
        public static int OnQuest10State2()
        {
            return Check(10, 2) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_3")]
        public static int OnQuest1State3()
        {
            return Check(1, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_3")]
        public static int OnQuest2State3()
        {
            return Check(2, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_3")]
        public static int OnQuest3State3()
        {
            return Check(3, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_3")]
        public static int OnQuest4State3()
        {
            return Check(4, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_3")]
        public static int OnQuest5State3()
        {
            return Check(5, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_3")]
        public static int OnQuest6State3()
        {
            return Check(6, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_3")]
        public static int OnQuest7State3()
        {
            return Check(7, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_3")]
        public static int OnQuest8State3()
        {
            return Check(8, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_3")]
        public static int OnQuest9State3()
        {
            return Check(9, 3) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_3")]
        public static int OnQuest10State3()
        {
            return Check(10, 3) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_4")]
        public static int OnQuest1State4()
        {
            return Check(1, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_4")]
        public static int OnQuest2State4()
        {
            return Check(2, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_4")]
        public static int OnQuest3State4()
        {
            return Check(3, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_4")]
        public static int OnQuest4State4()
        {
            return Check(4, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_4")]
        public static int OnQuest5State4()
        {
            return Check(5, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_4")]
        public static int OnQuest6State4()
        {
            return Check(6, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_4")]
        public static int OnQuest7State4()
        {
            return Check(7, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_4")]
        public static int OnQuest8State4()
        {
            return Check(8, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_4")]
        public static int OnQuest9State4()
        {
            return Check(9, 4) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_4")]
        public static int OnQuest10State4()
        {
            return Check(10, 4) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_5")]
        public static int OnQuest1State5()
        {
            return Check(1, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_5")]
        public static int OnQuest2State5()
        {
            return Check(2, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_5")]
        public static int OnQuest3State5()
        {
            return Check(3, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_5")]
        public static int OnQuest4State5()
        {
            return Check(4, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_5")]
        public static int OnQuest5State5()
        {
            return Check(5, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_5")]
        public static int OnQuest6State5()
        {
            return Check(6, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_5")]
        public static int OnQuest7State5()
        {
            return Check(7, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_5")]
        public static int OnQuest8State5()
        {
            return Check(8, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_5")]
        public static int OnQuest9State5()
        {
            return Check(9, 5) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_5")]
        public static int OnQuest10State5()
        {
            return Check(10, 5) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_6")]
        public static int OnQuest1State6()
        {
            return Check(1, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_6")]
        public static int OnQuest2State6()
        {
            return Check(2, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_6")]
        public static int OnQuest3State6()
        {
            return Check(3, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_6")]
        public static int OnQuest4State6()
        {
            return Check(4, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_6")]
        public static int OnQuest5State6()
        {
            return Check(5, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_6")]
        public static int OnQuest6State6()
        {
            return Check(6, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_6")]
        public static int OnQuest7State6()
        {
            return Check(7, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_6")]
        public static int OnQuest8State6()
        {
            return Check(8, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_6")]
        public static int OnQuest9State6()
        {
            return Check(9, 6) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_6")]
        public static int OnQuest10State6()
        {
            return Check(10, 6) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_7")]
        public static int OnQuest1State7()
        {
            return Check(1, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_7")]
        public static int OnQuest2State7()
        {
            return Check(2, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_7")]
        public static int OnQuest3State7()
        {
            return Check(3, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_7")]
        public static int OnQuest4State7()
        {
            return Check(4, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_7")]
        public static int OnQuest5State7()
        {
            return Check(5, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_7")]
        public static int OnQuest6State7()
        {
            return Check(6, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_7")]
        public static int OnQuest7State7()
        {
            return Check(7, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_7")]
        public static int OnQuest8State7()
        {
            return Check(8, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_7")]
        public static int OnQuest9State7()
        {
            return Check(9, 7) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_7")]
        public static int OnQuest10State7()
        {
            return Check(10, 7) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_8")]
        public static int OnQuest1State8()
        {
            return Check(1, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_8")]
        public static int OnQuest2State8()
        {
            return Check(2, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_8")]
        public static int OnQuest3State8()
        {
            return Check(3, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_8")]
        public static int OnQuest4State8()
        {
            return Check(4, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_8")]
        public static int OnQuest5State8()
        {
            return Check(5, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_8")]
        public static int OnQuest6State8()
        {
            return Check(6, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_8")]
        public static int OnQuest7State8()
        {
            return Check(7, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_8")]
        public static int OnQuest8State8()
        {
            return Check(8, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_8")]
        public static int OnQuest9State8()
        {
            return Check(9, 8) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_8")]
        public static int OnQuest10State8()
        {
            return Check(10, 8) ? 1 : 0;
        }

        [NWNEventHandler("on_qst1_state_9")]
        public static int OnQuest1State9()
        {
            return Check(1, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst2_state_9")]
        public static int OnQuest2State9()
        {
            return Check(2, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst3_state_9")]
        public static int OnQuest3State9()
        {
            return Check(3, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst4_state_9")]
        public static int OnQuest4State9()
        {
            return Check(4, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst5_state_9")]
        public static int OnQuest5State9()
        {
            return Check(5, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst6_state_9")]
        public static int OnQuest6State9()
        {
            return Check(6, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst7_state_9")]
        public static int OnQuest7State9()
        {
            return Check(7, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst8_state_9")]
        public static int OnQuest8State9()
        {
            return Check(8, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst9_state_9")]
        public static int OnQuest9State9()
        {
            return Check(9, 9) ? 1 : 0;
        }
        [NWNEventHandler("on_qst10_state_9")]
        public static int OnQuest10State9()
        {
            return Check(10, 9) ? 1 : 0;
        }


        public static bool Check(int index, int state)
        {
            NWPlayer player = NWScript.GetPCSpeaker();
            NWObject talkTo = NWScript.OBJECT_SELF;
            var questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!QuestService.QuestExistsByID(questID))
            {
                NWScript.SpeakString("ERROR: Quest #" + index + " State #" + state + " is improperly configured. Please notify an admin");
                return false;
            }

            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            if (status == null) return false;

            var has = status.QuestState == state && status.CompletionDate == null;
            return has;
        }
    }
}
