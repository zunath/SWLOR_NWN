using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class MimicryQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new QuestBuilder();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            SalvagedIntelligence();

            return _builder.Build();
        }

        // Intro to the Combat Analyzer (Mimicry) system: a salvager lends the player a prototype
        // analyzer and sends them to field-test it on wild Kath Hounds, then points them at the
        // Techniques window and the Combat Analyzer perk line.
        private void SalvagedIntelligence()
        {
            _builder.Create("salvaged_intel", "Salvaged Intelligence")

                .AddState()
                .SetStateJournalText("Renna Tarsk lent you a salvaged Combat Analyzer prototype to field-test. Defeat three Kath Hounds in the Viscara Wildlands so it can record their combat patterns, then return to her in the Veles Colony.")
                .AddKillObjective(NPCGroupType.Viscara_WildlandKathHounds, 3)

                .AddState()
                .SetStateJournalText("The prototype recorded the Kath Hounds' patterns. Return to Renna Tarsk in the Veles Colony so she can calibrate it.")

                .AddGoldReward(400)
                .AddXPReward(1500)

                .OnCompleteAction((player, sourceObject) =>
                {
                    Skill.GiveSkillXP(player, SkillType.Mimicry, 400);
                    SendMessageToPC(player,
                        "Renna calibrates the prototype and copies its schematics for you. Invest in the Combat Analyzer perk to record techniques from enemies you defeat, then review, equip, and manage them with the /techniques window.");
                });
        }
    }
}
