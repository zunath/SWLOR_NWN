using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public sealed class UnarmedMasteryQuestDefinition : IQuestListDefinition
    {
        internal const string PerfectFormMasteryQuestId = "perfect_form_mastery";
        internal const string HaymakerMasteryQuestId = "haymaker_mastery";
        internal const string OpeningSpentObjective = "unarmed_opening_spent";
        internal const string FinishSpentObjective = "unarmed_finish_spent";
        internal const string DistractionSpentObjective = "unarmed_distraction_spent";
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            PerfectForm();
            Haymaker();
            return _builder.Build();
        }

        private void PerfectForm()
        {
            _builder.Create(PerfectFormMasteryQuestId, "The Shape of a Combination")
                .PrerequisiteSkill(SkillType.Unarmed, 50)
                .AddState()
                .SetStateJournalText("Complete 30 landed Palm Strikes that spend Opening and 30 landed finishers that spend Finish against hostile, unsummoned NPC opponents of combat level 45 or higher. Use equipped Power Gloves. Only your own paid combinations after accepting this trial count; misses, free casts and another fighter's attacks do not. Return to Tressa Kade in the Offices & Labs on CZ-220 when both requirements are complete.")
                .AddCombatObjective(OpeningSpentObjective, "Opening-spending Palm Strikes", 30)
                .AddCombatObjective(FinishSpentObjective, "Finish-spending finishers", 30)
                .AddState()
                .SetStateJournalText("You completed both combination requirements. Return to Tressa Kade in the Offices & Labs on CZ-220 to complete your Perfect Form training.")
                .OnCompleteAction((player, source) => Achievement.GiveAchievement(player, AchievementType.PerfectForm));
        }

        private void Haymaker()
        {
            _builder.Create(HaymakerMasteryQuestId, "Make the Opening Count")
                .PrerequisiteSkill(SkillType.Unarmed, 50)
                .AddState()
                .SetStateJournalText("Land 40 Uppercuts against hostile, unsummoned NPC opponents of combat level 45 or higher. Use equipped Power Gloves. Only your own paid attacks after accepting this trial count; misses and free casts do not. Rattled and Retaliation are optional. Return to Borrik Sen in the Hangar on CZ-220 when the requirement is complete.")
                .AddCombatObjective(DistractionSpentObjective, "Landed Uppercuts", 40)
                .AddState()
                .SetStateJournalText("You completed the heavy-strike requirement. Return to Borrik Sen in the Hangar on CZ-220 to complete your Haymaker training.")
                .OnCompleteAction((player, source) => Achievement.GiveAchievement(player, AchievementType.Haymaker));
        }
    }
}
