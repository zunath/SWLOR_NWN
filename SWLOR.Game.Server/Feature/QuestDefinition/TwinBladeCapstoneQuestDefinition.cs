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
    public class TwinBladeCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string TempestBloomFoundationQuestId = "tempest_bloom_foundation";
        internal const string TempestBloomMeasureQuestId = "tempest_bloom_measure";
        internal const string TempestBloomBreachQuestId = "tempest_bloom_breach";
        internal const string TempestBloomCircleQuestId = "tempest_bloom_circle";
        internal const string TempestBloomMasteryQuestId = "tempest_bloom_mastery";
        internal const string TempestBloomAdeptResref = "cp_tempbloom_ad";
        internal const string TempestBloomSpecialistResref = "cp_tempbloom_sp";
        internal const string TempestBloomInnerCircleResref = "cp_tempbloom_ic";
        internal const string RedBloomFoundationQuestId = "red_bloom_foundation";
        internal const string RedBloomMeasureQuestId = "red_bloom_measure";
        internal const string RedBloomBreachQuestId = "red_bloom_breach";
        internal const string RedBloomCircleQuestId = "red_bloom_circle";
        internal const string RedBloomMasteryQuestId = "red_bloom_mastery";
        internal const string RedBloomAdeptResref = "cp_redbloom_ad";
        internal const string RedBloomSpecialistResref = "cp_redbloom_sp";
        internal const string RedBloomInnerCircleResref = "cp_redbloom_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            TempestBloomFoundation();
            TempestBloomMeasure();
            TempestBloomBreach();
            TempestBloomCircle();
            TempestBloomMastery();
            RedBloomFoundation();
            RedBloomMeasure();
            RedBloomBreach();
            RedBloomCircle();
            RedBloomMastery();

            return _builder.Build();
        }

        private void TempestBloomFoundation()
        {
            _builder.Create(TempestBloomFoundationQuestId, "First Principle: Tempest Bloom")
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneSmugglersMoonFightClubBackroomsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveSmugglersMoonFightClubBackroomsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomBackroomBoutLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat Tempest Bloom adepts and secure the Tempest Bloom Backroom Bout Ledger.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Tempest Bloom Backroom Bout Ledger from Smuggler's Moon Fight Club Backrooms. Return to Iven Brask for the next Tempest Bloom lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void TempestBloomMeasure()
        {
            _builder.Create(TempestBloomMeasureQuestId, "The Measure of Tempest Bloom")
                .PrerequisiteQuest(TempestBloomFoundationQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomRingShockRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat Tempest Bloom specialists and secure the Tempest Bloom Ring Shock Regulator.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Tempest Bloom Ring Shock Regulator from Smuggler's Moon Fight Club Backrooms. Return to Iven Brask for the next Tempest Bloom lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void TempestBloomBreach()
        {
            _builder.Create(TempestBloomBreachQuestId, "Fault Line: Tempest Bloom")
                .PrerequisiteQuest(TempestBloomMeasureQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomCrackedPitSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat the Tempest Bloom warden and secure the Tempest Bloom Cracked Pit Sigil.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Tempest Bloom Cracked Pit Sigil from Smuggler's Moon Fight Club Backrooms. Return to Iven Brask for the next Tempest Bloom lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void TempestBloomCircle()
        {
            _builder.Create(TempestBloomCircleQuestId, "Circle of Proof: Tempest Bloom")
                .PrerequisiteQuest(TempestBloomBreachQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomPromotersPayoutChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat the Tempest Bloom inner circle and secure the Tempest Bloom Promoter's Payout Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Tempest Bloom Promoter's Payout Chit from Smuggler's Moon Fight Club Backrooms. Return to Iven Brask for the next Tempest Bloom lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void TempestBloomMastery()
        {
            _builder.Create(TempestBloomMasteryQuestId, "Tempest Bloom Mastery")
                .PrerequisiteQuest(TempestBloomCircleQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Tempest Bloom master is waiting in Smuggler's Moon Fight Club Backrooms. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Tempest Bloom master is defeated. Return to Iven Brask and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TempestBloom);
                });
        }

        private void RedBloomFoundation()
        {
            _builder.Create(RedBloomFoundationQuestId, "First Principle: Red Bloom")
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneSmugglersMoonFightClubBackroomsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveSmugglersMoonFightClubBackroomsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomBackroomBoutLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "The Red Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat Red Bloom adepts and secure the Red Bloom Backroom Bout Ledger.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Red Bloom Backroom Bout Ledger from Smuggler's Moon Fight Club Backrooms. Return to Nyra Tane for the next Red Bloom lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void RedBloomMeasure()
        {
            _builder.Create(RedBloomMeasureQuestId, "The Measure of Red Bloom")
                .PrerequisiteQuest(RedBloomFoundationQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomRingShockRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Red Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat Red Bloom specialists and secure the Red Bloom Ring Shock Regulator.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Red Bloom Ring Shock Regulator from Smuggler's Moon Fight Club Backrooms. Return to Nyra Tane for the next Red Bloom lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void RedBloomBreach()
        {
            _builder.Create(RedBloomBreachQuestId, "Fault Line: Red Bloom")
                .PrerequisiteQuest(RedBloomMeasureQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomCrackedPitSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "The Red Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat the Red Bloom warden and secure the Red Bloom Cracked Pit Sigil.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Red Bloom Cracked Pit Sigil from Smuggler's Moon Fight Club Backrooms. Return to Nyra Tane for the next Red Bloom lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void RedBloomCircle()
        {
            _builder.Create(RedBloomCircleQuestId, "Circle of Proof: Red Bloom")
                .PrerequisiteQuest(RedBloomBreachQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomPromotersPayoutChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "The Red Bloom capstone line continues in Smuggler's Moon Fight Club Backrooms. Defeat the Red Bloom inner circle and secure the Red Bloom Promoter's Payout Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Red Bloom Promoter's Payout Chit from Smuggler's Moon Fight Club Backrooms. Return to Nyra Tane for the next Red Bloom lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void RedBloomMastery()
        {
            _builder.Create(RedBloomMasteryQuestId, "Red Bloom Mastery")
                .PrerequisiteQuest(RedBloomCircleQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Red Bloom master is waiting in Smuggler's Moon Fight Club Backrooms. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Red Bloom master is defeated. Return to Nyra Tane and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.RedBloom);
                });
        }

        private static void RemoveSmugglersMoonFightClubBackroomsAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                SpearCapstoneQuestDefinition.CripplingDefenseFoundationQuestId,
                SpearCapstoneQuestDefinition.CripplingDefenseMeasureQuestId,
                SpearCapstoneQuestDefinition.CripplingDefenseBreachQuestId,
                SpearCapstoneQuestDefinition.CripplingDefenseCircleQuestId,
                SpearCapstoneQuestDefinition.CripplingDefenseMasteryQuestId,
                TwinBladeCapstoneQuestDefinition.TempestBloomFoundationQuestId,
                TwinBladeCapstoneQuestDefinition.TempestBloomMeasureQuestId,
                TwinBladeCapstoneQuestDefinition.TempestBloomBreachQuestId,
                TwinBladeCapstoneQuestDefinition.TempestBloomCircleQuestId,
                TwinBladeCapstoneQuestDefinition.TempestBloomMasteryQuestId,
                TwinBladeCapstoneQuestDefinition.RedBloomFoundationQuestId,
                TwinBladeCapstoneQuestDefinition.RedBloomMeasureQuestId,
                TwinBladeCapstoneQuestDefinition.RedBloomBreachQuestId,
                TwinBladeCapstoneQuestDefinition.RedBloomCircleQuestId,
                TwinBladeCapstoneQuestDefinition.RedBloomMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneSmugglersMoonFightClubBackroomsKey, questIds);
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
