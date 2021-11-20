﻿using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class RepairingCoxxionEquipment: AbstractQuest
    {
        public RepairingCoxxionEquipment()
        {
            CreateQuest(26, "Repairing Coxxion Equipment", "caxx_repair")
                
                .AddObjectiveCollectItem(1, "la_rep_1", 1, false)
                .AddObjectiveCollectItem(1, "br_rep_1", 1, false)
                .AddObjectiveCollectItem(1, "bp_rep_1", 1, false)

                .AddRewardGold(800)
                .AddRewardFame(4, 25)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
