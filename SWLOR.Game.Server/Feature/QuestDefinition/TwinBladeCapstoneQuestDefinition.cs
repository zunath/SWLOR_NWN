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
            _builder.Create(TempestBloomFoundationQuestId, "The Undercard")
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
                    "Iven Brask wants the backroom fight bill under the Nar Shaddaa fight club shut down. Use his key on the sealed door behind the main pit, defeat six Tempest Bloom adepts in the Smuggler's Moon Fight Club Backrooms, and recover the Tempest Bloom Backroom Bout Ledger.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are dead and the Tempest Bloom Backroom Bout Ledger is in hand. Return it to Iven Brask at the Tilted Visor on Nar Shaddaa.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void TempestBloomMeasure()
        {
            _builder.Create(TempestBloomMeasureQuestId, "Five Falls, No Bell")
                .PrerequisiteQuest(TempestBloomFoundationQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomRingShockRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "Defeat the five Tempest Bloom specialists running the live pits in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa, then pull the rewired Tempest Bloom Ring Shock Regulator off the wall.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "The specialists are finished and the Tempest Bloom Ring Shock Regulator has been torn out. Bring it to Iven Brask at the Tilted Visor on Nar Shaddaa.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void TempestBloomBreach()
        {
            _builder.Create(TempestBloomBreachQuestId, "The Warden Takes All Comers")
                .PrerequisiteQuest(TempestBloomMeasureQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomCrackedPitSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Tempest Bloom warden guarding the deep gate in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and take the Tempest Bloom Cracked Pit Sigil. The warden has never been beaten by a lone fighter; bring allies.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is dead and the Tempest Bloom Cracked Pit Sigil is in hand. Return it to Iven Brask at the Tilted Visor on Nar Shaddaa.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void TempestBloomCircle()
        {
            _builder.Create(TempestBloomCircleQuestId, "The House Card")
                .PrerequisiteQuest(TempestBloomBreachQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneTempestBloomPromotersPayoutChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneTempestBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four fighters of the Tempest Bloom inner circle beyond the warden's gate in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and recover the Tempest Bloom Promoter's Payout Chit from the last of them.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneTempestBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "The inner circle is broken and the Tempest Bloom Promoter's Payout Chit is in hand. Deliver it to Iven Brask at the Tilted Visor on Nar Shaddaa.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void TempestBloomMastery()
        {
            _builder.Create(TempestBloomMasteryQuestId, "The Final Bloom")
                .PrerequisiteQuest(TempestBloomCircleQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Tempest Bloom master in the deepest room of the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa. No one has survived his final bloom alone; bring allies. No proof is required beyond his defeat.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_TempestBloom_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Bloom master is defeated and the backroom bill is finished. Return to Iven Brask at the Tilted Visor on Nar Shaddaa.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TempestBloom);
                });
        }

        private void RedBloomFoundation()
        {
            _builder.Create(RedBloomFoundationQuestId, "First Blood")
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
                    "Nyra Tane wants the backroom school teaching her form shut down. Use her key on the sealed door behind the main pit of the Nar Shaddaa fight club, defeat six Red Bloom adepts in the Smuggler's Moon Fight Club Backrooms, and recover the Red Bloom Backroom Bout Ledger.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are dead and the Red Bloom Backroom Bout Ledger is in hand. Return it to Nyra Tane at the Nar Shaddaa casino.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void RedBloomMeasure()
        {
            _builder.Create(RedBloomMeasureQuestId, "Clean Cuts")
                .PrerequisiteQuest(RedBloomFoundationQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomRingShockRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "Defeat the five Red Bloom specialists in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa, then tear the rewired Red Bloom Ring Shock Regulator off the wall above the champion's pit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "The specialists are dead and the Red Bloom Ring Shock Regulator has been pulled. Bring it to Nyra Tane at the Nar Shaddaa casino.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void RedBloomBreach()
        {
            _builder.Create(RedBloomBreachQuestId, "The Doorman")
                .PrerequisiteQuest(RedBloomMeasureQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomCrackedPitSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Red Bloom warden at the deep gate in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and take the Red Bloom Cracked Pit Sigil. Nyra Tane's condition stands: do not face him alone; bring allies.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is dead and the Red Bloom Cracked Pit Sigil is in hand. Return it to Nyra Tane at the Nar Shaddaa casino.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void RedBloomCircle()
        {
            _builder.Create(RedBloomCircleQuestId, "Bad Blood")
                .PrerequisiteQuest(RedBloomBreachQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRedBloomPromotersPayoutChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRedBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four fighters of the Red Bloom inner circle beyond the warden's gate in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and recover the Red Bloom Promoter's Payout Chit from the last of them.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRedBloomPromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "The inner circle is dead and the Red Bloom Promoter's Payout Chit is in hand. Deliver it to Nyra Tane at the Nar Shaddaa casino.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void RedBloomMastery()
        {
            _builder.Create(RedBloomMasteryQuestId, "Title Bout")
                .PrerequisiteQuest(RedBloomCircleQuestId)
                .PrerequisiteSkill(SkillType.TwinBlade, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Red Bloom master holding Nyra Tane's old title in the champion's room of the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa. No one has left her ring alone; bring allies. No proof is required beyond her defeat.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RedBloom_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Red Bloom master is defeated and the backroom school is finished. Return to Nyra Tane at the Nar Shaddaa casino.")
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
