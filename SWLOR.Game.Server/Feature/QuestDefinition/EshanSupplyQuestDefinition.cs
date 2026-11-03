using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class EshanSupplyQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            StarwortForStrainedHands();
            AQuietFountain();
            TheServiceTable();
            PracticeSleeves();
            GreensForTheLateShift();
            TomorrowsSilverwood();

            var quests = _builder.Build();
            foreach (var quest in quests.Values)
            {
                if (quest.IsRepeatable)
                    quest.Prerequisites.Add(new EshanDailySupplyPrerequisite(quest.QuestId));
            }

            return quests;
        }

        private void StarwortForStrainedHands()
        {
            _builder.Create("eshan_starwort_hands", "Starwort for Strained Hands")
                .AddState()
                .SetStateJournalText("Kael Senn, the arms tutor in Eshan City's Highcrest Quarter, needs three Dantooine Starwort for compresses used by trainees and injured workers. Scavenge Dantooine Herbs in Dantooine's Crystal Fields (Scavenging rank 5), or obtain the herbs from another gatherer. Return the three herbs to Kael. No kills are required.")
                .AddCollectItemObjective("dant_starwort", 3)
                .AddState()
                .SetStateJournalText("The three Dantooine Starwort have been delivered. Speak to Kael Senn in Eshan City's Highcrest Quarter for your payment.")
                .AddXPReward(2200)
                .AddGoldReward(1500);
        }

        private void AQuietFountain()
        {
            _builder.Create("eshan_fountain_parts", "A Quiet Fountain")
                .AddState()
                .SetStateJournalText("Tellis Vor, the maintenance hand in Eshan City's Highcrest Quarter, needs six Scrap Metal and four Good Quality Electronics to repair a fountain pump. Scrap Metal can be scavenged from junk piles on CZ-220; Good Quality Electronics can be found in tier-three supply crates on the Abandoned Station (Scavenging rank 3) or obtained through trade. Return the parts to Tellis. You need not defeat any hostiles for this order.")
                .AddCollectItemObjective("scrap_metal", 6)
                .AddCollectItemObjective("elec_good", 4)
                .AddState()
                .SetStateJournalText("Tellis Vor has the replacement parts. Speak to him in Eshan City's Highcrest Quarter for your payment.")
                .AddXPReward(2000)
                .AddGoldReward(1400);
        }

        private void TheServiceTable()
        {
            _builder.Create("eshan_service_meds", "The Service Table")
                .AddState()
                .SetStateJournalText("Lady Aelis Varo in Eshan City's Highcrest Quarter is stocking a treatment table for the household's service workers. Bring her four Kolto Injections and two Medi Syringes. These can be scavenged from Medical Piles in Dantooine's Abandoned Warehouse without a Scavenging perk, or obtained through trade. Deliver the supplies to Lady Aelis; this order requires no kills.")
                .AddCollectItemObjective("kolto_injection", 4)
                .AddCollectItemObjective("medisyringes", 2)
                .AddState()
                .SetStateJournalText("The medical supplies are delivered. Speak to Lady Aelis Varo in Eshan City's Highcrest Quarter for your payment.")
                .AddXPReward(1800)
                .AddGoldReward(1200);
        }

        private void PracticeSleeves()
        {
            _builder.Create("eshan_practice_sleeves", "Practice Sleeves")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Vesa Neral, the clothier in Eshan City's Highcrest Quarter, needs eight Good Quality Fiberplast for replacement practice sleeves. Scavenge fiberplast bushes in Eshan's Veylan Groves (Scavenging rank 3), or obtain the material through trade, then deliver it to Vesa. This supply order becomes available again 24 real-world hours after completion; abandoning a later order starts that wait again.")
                .AddCollectItemObjective("fiberp_good", 8)
                .AddState()
                .SetStateJournalText("The eight Good Quality Fiberplast are delivered. Speak to Vesa Neral in Eshan City's Highcrest Quarter for your payment. Another practice-sleeve order is available 24 real-world hours after completion.")
                .AddXPReward(650)
                .AddGoldReward(400);
        }

        private void GreensForTheLateShift()
        {
            _builder.Create("eshan_shift_greens", "Greens for the Late Shift")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Nema Tal, the provisioner in Eshan City's Highcrest Quarter, needs six Eshan Silverwood Greens and four Eshan Scallions for the workers' evening meal. Scavenge Veylan Greens Patches in Eshan's Veylan Groves (Scavenging rank 1), or obtain these ingredients through trade. Deliver them to Nema; no hunting is required. This supply order becomes available again 24 real-world hours after completion; abandoning a later order starts that wait again.")
                .AddCollectItemObjective("esh_greens", 6)
                .AddCollectItemObjective("esh_scallion", 4)
                .AddState()
                .SetStateJournalText("The greens and scallions are delivered. Speak to Nema Tal in Eshan City's Highcrest Quarter for your payment. Another late-shift meal order is available 24 real-world hours after completion.")
                .AddXPReward(600)
                .AddGoldReward(400);
        }

        private void TomorrowsSilverwood()
        {
            _builder.Create("eshan_grove_seedlings", "Tomorrow's Silverwood")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Sivra Varo in Eshan City's Highcrest Quarter needs six Eshan Silverwood Seeds and four Good Quality Fiberplast for seedling wraps. Scavenge Silverwood Seed Bushes (Scavenging rank 1) and fiberplast bushes (Scavenging rank 3) in Eshan's Veylan Groves, or obtain the supplies through trade, then deliver them to Sivra. Leave the standing trees alone. This order becomes available again 24 real-world hours after completion; abandoning a later order starts that wait again.")
                .AddCollectItemObjective("esh_seeds", 6)
                .AddCollectItemObjective("fiberp_good", 4)
                .AddState()
                .SetStateJournalText("The seeds and seedling-wrap material are delivered. Speak to Sivra Varo in Eshan City's Highcrest Quarter for your payment. Another seedling order is available 24 real-world hours after completion.")
                .AddXPReward(700)
                .AddGoldReward(350);
        }
    }
}
