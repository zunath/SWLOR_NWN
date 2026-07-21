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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDantooineMedicalSublevelKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDantooineMedicalSublevelAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyCocktailTriageWardLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyCocktailTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "Use Kavi Dorn's key to enter the Dantooine Medical Sublevel on Dantooine. Defeat six Emergency Cocktail adepts and secure the Emergency Cocktail Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.Dantooine_EmergencyCocktail_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Cocktail Triage Ward Ledger is secured. Return it to Kavi Dorn at the Dantooine Republic medical center.")
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
                    "Defeat five Emergency Cocktail specialists in the Dantooine Medical Sublevel on Dantooine and recover the Emergency Cocktail Kolto Conduit Coupler from their conduit splice.")
                .AddKillObjective(NPCGroupType.Dantooine_EmergencyCocktail_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Cocktail Kolto Conduit Coupler is recovered. Return it to Kavi Dorn at the Dantooine Republic medical center.")
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
                    "Defeat the Emergency Cocktail warden guarding the sealed clean room in the Dantooine Medical Sublevel on Dantooine and secure the Emergency Cocktail Fractured Ward Sigil. Kavi Dorn advises bringing two trusted companions.")
                .AddKillObjective(NPCGroupType.Dantooine_EmergencyCocktail_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is defeated and the Emergency Cocktail Fractured Ward Sigil is secured. Return it to Kavi Dorn at the Dantooine Republic medical center.")
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
                    "Defeat the four members of the Emergency Cocktail inner circle in the matron's ward of the Dantooine Medical Sublevel on Dantooine and secure the Emergency Cocktail Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.Dantooine_EmergencyCocktail_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyCocktailMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The inner circle is defeated and the Emergency Cocktail Matron's Ward Token is secured. Return it to Kavi Dorn at the Dantooine Republic medical center.")
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
                    "Defeat the Emergency Cocktail master in the deepest room of the Dantooine Medical Sublevel on Dantooine. His defeat is the only proof required. Kavi Dorn warns that this is not a fight for one person; bring companions.")
                .AddKillObjective(NPCGroupType.Dantooine_EmergencyCocktail_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Cocktail master is defeated. Return to Kavi Dorn at the Dantooine Republic medical center for the final lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.EmergencyCocktail);
                });
        }

        private static void RemoveDantooineMedicalSublevelAccessIfNoLongerNeeded(uint player)
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

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneDantooineMedicalSublevelKey, questIds);
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
