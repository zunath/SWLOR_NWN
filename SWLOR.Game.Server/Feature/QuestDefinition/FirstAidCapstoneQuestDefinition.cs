using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class FirstAidCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string EmergencyCocktailFoundationQuestId = "emergency_cocktail_foundation";
        internal const string EmergencyCocktailMeasureQuestId = "emergency_cocktail_measure";
        internal const string EmergencyCocktailBreachQuestId = "emergency_cocktail_breach";
        internal const string EmergencyCocktailCircleQuestId = "emergency_cocktail_circle";
        internal const string EmergencyCocktailMasteryQuestId = "emergency_cocktail_mastery";
        internal const string EmergencyCocktailAdeptResref = "cp_emcocktail_ad";
        internal const string EmergencyCocktailSpecialistResref = "cp_emcocktail_sp";
        internal const string EmergencyCocktailInnerCircleResref = "cp_emcocktail_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            EmergencyCocktailFoundation();
            EmergencyCocktailMeasure();
            EmergencyCocktailBreach();
            EmergencyCocktailCircle();
            EmergencyCocktailMastery();

            return _builder.Build();
        }

        private void EmergencyCocktailFoundation()
        {
            _builder.Create(EmergencyCocktailFoundationQuestId, "Six Doses Under Fire")
                .PrerequisiteSkill(SkillType.FirstAid, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneMonCalaMedicalSublevelKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveMonCalaMedicalSublevelAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyCocktailTriageWardLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyCocktailTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "Kavi Dorn gave you the Mon Cala Medical Sublevel Key. On Mon Cala, travel east from Dac City through the Coral Isles and enter the Coral Isles Facility on the south-west side of the outer isles, held by eco-terrorists. Inside, go through the sliding door off the entry hall; the service lift down to the Medical Sublevel stands in the red-walled bay just east of the office block. Defeat six Emergency Cocktail Adepts and recover the Emergency Cocktail Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.MonCala_EmergencyCocktail_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Emergency Cocktail Triage Ward Ledger. Return it to Kavi Dorn at the Medshed on Smuggler's Moon.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void EmergencyCocktailMeasure()
        {
            _builder.Create(EmergencyCocktailMeasureQuestId, "Five Bad Batches")
                .PrerequisiteQuest(EmergencyCocktailFoundationQuestId)
                .PrerequisiteSkill(SkillType.FirstAid, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyCocktailKoltoConduitCoupler)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyCocktailKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Emergency Cocktail Specialists deeper in the Medical Sublevel on Mon Cala and recover the Emergency Cocktail Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.MonCala_EmergencyCocktail_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Emergency Cocktail Kolto Conduit Coupler. Return it to Kavi Dorn at the Medshed on Smuggler's Moon.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void EmergencyCocktailBreach()
        {
            _builder.Create(EmergencyCocktailBreachQuestId, "One Dose Too Strong")
                .PrerequisiteQuest(EmergencyCocktailMeasureQuestId)
                .PrerequisiteSkill(SkillType.FirstAid, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyCocktailFracturedWardSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyCocktailFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "With two trusted companions, defeat the Emergency Cocktail Warden in the south-west wing of the Medical Sublevel on Mon Cala. Recover the Emergency Cocktail Fractured Ward Sigil.")
                .AddKillObjective(NPCGroupType.MonCala_EmergencyCocktail_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is dead and you hold the Emergency Cocktail Fractured Ward Sigil. Return it to Kavi Dorn at the Medshed on Smuggler's Moon.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void EmergencyCocktailCircle()
        {
            _builder.Create(EmergencyCocktailCircleQuestId, "Four Minutes to Flatline")
                .PrerequisiteQuest(EmergencyCocktailBreachQuestId)
                .PrerequisiteSkill(SkillType.FirstAid, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyCocktailMatronsWardToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyCocktailMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Emergency Cocktail Inner Circle in the Medical Sublevel on Mon Cala and recover the Emergency Cocktail Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.MonCala_EmergencyCocktail_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The inner circle is finished and you hold the Emergency Cocktail Matron's Ward Token. Return it to Kavi Dorn at the Medshed on Smuggler's Moon.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void EmergencyCocktailMastery()
        {
            _builder.Create(EmergencyCocktailMasteryQuestId, "The Perfected Batch")
                .PrerequisiteQuest(EmergencyCocktailCircleQuestId)
                .PrerequisiteSkill(SkillType.FirstAid, 50)

                .AddState()
                .SetStateJournalText(
                    "With companions at your side, defeat the Emergency Cocktail Master in the Protected Ward, reached from the north-east corner of the Medical Sublevel on Mon Cala. His death is the only proof Kavi Dorn needs.")
                .AddKillObjective(NPCGroupType.MonCala_EmergencyCocktail_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Cocktail Master is dead. Return to Kavi Dorn at the Medshed on Smuggler's Moon.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.EmergencyCocktail);
                });
        }

        private static void RemoveMonCalaMedicalSublevelAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                FirstAidCapstoneQuestDefinition.EmergencyCocktailFoundationQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailMeasureQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailBreachQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailCircleQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailMasteryQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineFoundationQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineMeasureQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineBreachQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineCircleQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineMasteryQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitFoundationQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitMeasureQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitBreachQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitCircleQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneMonCalaMedicalSublevelKey, questIds);
        }

        private static void RemoveAreaAccessIfNoLongerNeeded(
            uint player,
            KeyItemType accessKeyItem,
            IEnumerable<string> questIds)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));

            foreach (var questId in questIds)
            {
                if (!dbPlayer.Quests.TryGetValue(questId, out var quest))
                    continue;

                if (quest.TimesCompleted > 0 || quest.CurrentState > 0)
                    return;
            }

            KeyItem.RemoveKeyItem(player, accessKeyItem);
        }
    }
}
