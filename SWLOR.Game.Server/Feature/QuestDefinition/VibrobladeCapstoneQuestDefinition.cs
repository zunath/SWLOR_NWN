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
    public class VibrobladeCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string InvincibleFoundationQuestId = "invincible_foundation";
        internal const string InvincibleMeasureQuestId = "invincible_measure";
        internal const string InvincibleBreachQuestId = "invincible_breach";
        internal const string InvincibleCircleQuestId = "invincible_circle";
        internal const string InvincibleMasteryQuestId = "invincible_mastery";
        internal const string InvincibleAdeptResref = "cp_invinc_ad";
        internal const string InvincibleSpecialistResref = "cp_invinc_sp";
        internal const string InvincibleInnerCircleResref = "cp_invinc_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            InvincibleFoundation();
            InvincibleMeasure();
            InvincibleBreach();
            InvincibleCircle();
            InvincibleMastery();

            return _builder.Build();
        }

        private void InvincibleFoundation()
        {
            _builder.Create(InvincibleFoundationQuestId, "First Principle: Invincible")
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneVelesMilitiaAnnexKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveVelesMilitiaAnnexAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInvincibleVelesDrillLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInvincibleVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "The Invincible capstone line continues in Veles Militia Annex. Defeat Invincible adepts and secure the Invincible Veles Drill Ledger.")
                .AddKillObjective(NPCGroupType.Viscara_Invincible_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInvincibleVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Invincible Veles Drill Ledger from Veles Militia Annex. Return to Captain Tov Renn for the next Invincible lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void InvincibleMeasure()
        {
            _builder.Create(InvincibleMeasureQuestId, "The Measure of Invincible")
                .PrerequisiteQuest(InvincibleFoundationQuestId)
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInvincibleMilitiaRangeRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInvincibleMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "The Invincible capstone line continues in Veles Militia Annex. Defeat Invincible specialists and secure the Invincible Militia Range Relay.")
                .AddKillObjective(NPCGroupType.Viscara_Invincible_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInvincibleMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Invincible Militia Range Relay from Veles Militia Annex. Return to Captain Tov Renn for the next Invincible lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void InvincibleBreach()
        {
            _builder.Create(InvincibleBreachQuestId, "Fault Line: Invincible")
                .PrerequisiteQuest(InvincibleMeasureQuestId)
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInvincibleScoredChallengeBadge)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInvincibleScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "The Invincible capstone line continues in Veles Militia Annex. Defeat the Invincible warden and secure the Invincible Scored Challenge Badge.")
                .AddKillObjective(NPCGroupType.Viscara_Invincible_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInvincibleScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Invincible Scored Challenge Badge from Veles Militia Annex. Return to Captain Tov Renn for the next Invincible lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void InvincibleCircle()
        {
            _builder.Create(InvincibleCircleQuestId, "Circle of Proof: Invincible")
                .PrerequisiteQuest(InvincibleBreachQuestId)
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInvincibleCaptainsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInvincibleCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Invincible capstone line continues in Veles Militia Annex. Defeat the Invincible inner circle and secure the Invincible Captain's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Viscara_Invincible_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInvincibleCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Invincible Captain's Challenge Chit from Veles Militia Annex. Return to Captain Tov Renn for the next Invincible lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void InvincibleMastery()
        {
            _builder.Create(InvincibleMasteryQuestId, "Invincible Mastery")
                .PrerequisiteQuest(InvincibleCircleQuestId)
                .PrerequisiteSkill(SkillType.Vibroblade, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Invincible master is waiting in Veles Militia Annex. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Viscara_Invincible_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Invincible master is defeated. Return to Captain Tov Renn and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.Invincible);
                });
        }

        private static void RemoveVelesMilitiaAnnexAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                VibrobladeCapstoneQuestDefinition.InvincibleFoundationQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleMeasureQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleBreachQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleCircleQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleMasteryQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureFoundationQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureMeasureQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureBreachQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureCircleQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureMasteryQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownFoundationQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownMeasureQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownBreachQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownCircleQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneVelesMilitiaAnnexKey, questIds);
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
