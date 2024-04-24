using System.Collections.Generic;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class DantooineQuestDefinition : IQuestListDefinition
    {
            private readonly QuestBuilder _builder = new QuestBuilder();
            public Dictionary<string, QuestDetail> BuildQuests()
        {
            DanBundle();
            DanMedicalSupplies();
            BlueMilkQuest();
            CullVoritorlizardThreat();
            return _builder.Build();
        }

        private void BlueMilkQuest()
        {
            _builder.Create("bantha_milk_quest", "Bantha Milk Quest")
               .AddState()
                   .SetStateJournalText("The farmer from Dantooine requires milk that has been taken from the Dantari. Find it and bring back the milk.")
                   .AddCollectItemObjective("bantha_milk", 20)
               .AddState()
                   .SetStateJournalText("Return to the farmer and deliver the stolen blue milk.")
                   .AddXPReward(4000)
                   .AddGoldReward(2500);

           
        }
  


        private void CullVoritorlizardThreat()
        {
            _builder.Create("voritor_lizard_threat", "Cull the Voritor Lizard Threat")

                .AddState()
                .SetStateJournalText("Jason wants you to head to the Janta Caves and kill ten Voritor Lizards. Report back when this is done.")
                .AddKillObjective(NPCGroupType.Dantooine_VoritorLizard, 10)

                .AddState()
                .SetStateJournalText("Return to Jason in the Dantooine Colony and report your progress.")

                .AddGoldReward(4000)
                .AddXPReward(5000);


        }

        private void DanMedicalSupplies()
        {
            _builder.Create("medical_supplies", "Medical Supplies for the Clinic")

                .AddState()
                .SetStateJournalText("The clinic in Dantooine Medical Facility needs  kolto injections and  medi syringes. Collect them from the Abandoned Warehouse and return them to the clinic.")
                .AddCollectItemObjective("kolto_injection", 20)
                .AddCollectItemObjective("medisyringes", 5)

                .AddState()
                .SetStateJournalText("You delivered the kolto injections and medi syringes to the clinic. Talk to the clinic staff for your reward.")

                .AddXPReward(4000)
                .AddGoldReward(5000)
                .AddItemReward("med_supplies", 20)
                .AddItemReward("stim_pack", 10)
                .AddItemReward("wild_sandwich", 1);

        }
        private void DanBundle()
        {
            _builder.Create("hay_bundles", "Hay bales for Wrrl")
              

                .AddState()
                .SetStateJournalText("The farmer needs help with his herd. Collect 20 bags of hay bales from the Ruin Farmlands and return them to the farmer.")
                .AddCollectItemObjective("haybundle", 20)


                .AddState()
                .SetStateJournalText("You delivered the hay bundles to the farmer. Talk to the farmer for your reward.")
        
                .AddXPReward(2000)
                .AddGoldReward(1000);

        }

    }
}

