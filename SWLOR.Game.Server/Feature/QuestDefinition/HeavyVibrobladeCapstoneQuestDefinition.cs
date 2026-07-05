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
    public class HeavyVibrobladeCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string AbsoluteDefenseFoundationQuestId = "absolute_defense_foundation";
        internal const string AbsoluteDefenseMeasureQuestId = "absolute_defense_measure";
        internal const string AbsoluteDefenseBreachQuestId = "absolute_defense_breach";
        internal const string AbsoluteDefenseCircleQuestId = "absolute_defense_circle";
        internal const string AbsoluteDefenseMasteryQuestId = "absolute_defense_mastery";
        internal const string AbsoluteDefenseAdeptResref = "cp_absdef_ad";
        internal const string AbsoluteDefenseSpecialistResref = "cp_absdef_sp";
        internal const string AbsoluteDefenseInnerCircleResref = "cp_absdef_ic";
        internal const string SoulAscensionFoundationQuestId = "soul_ascension_foundation";
        internal const string SoulAscensionMeasureQuestId = "soul_ascension_measure";
        internal const string SoulAscensionBreachQuestId = "soul_ascension_breach";
        internal const string SoulAscensionCircleQuestId = "soul_ascension_circle";
        internal const string SoulAscensionMasteryQuestId = "soul_ascension_mastery";
        internal const string SoulAscensionAdeptResref = "cp_soulasc_ad";
        internal const string SoulAscensionSpecialistResref = "cp_soulasc_sp";
        internal const string SoulAscensionInnerCircleResref = "cp_soulasc_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            AbsoluteDefenseFoundation();
            AbsoluteDefenseMeasure();
            AbsoluteDefenseBreach();
            AbsoluteDefenseCircle();
            AbsoluteDefenseMastery();
            SoulAscensionFoundation();
            SoulAscensionMeasure();
            SoulAscensionBreach();
            SoulAscensionCircle();
            SoulAscensionMastery();

            return _builder.Build();
        }

        private void AbsoluteDefenseFoundation()
        {
            _builder.Create(AbsoluteDefenseFoundationQuestId, "First Principle: Absolute Defense")
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneKorribanForgeCavernsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveKorribanForgeCavernsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseForgeHeatLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "The Absolute Defense capstone line continues in Korriban Forge Caverns. Defeat Absolute Defense adepts and secure the Absolute Defense Forge Heat Ledger.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Absolute Defense Forge Heat Ledger from Korriban Forge Caverns. Return to Valis Korr for the next Absolute Defense lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void AbsoluteDefenseMeasure()
        {
            _builder.Create(AbsoluteDefenseMeasureQuestId, "The Measure of Absolute Defense")
                .PrerequisiteQuest(AbsoluteDefenseFoundationQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseSithTemperingMatrix)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "The Absolute Defense capstone line continues in Korriban Forge Caverns. Defeat Absolute Defense specialists and secure the Absolute Defense Sith Tempering Matrix.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Absolute Defense Sith Tempering Matrix from Korriban Forge Caverns. Return to Valis Korr for the next Absolute Defense lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void AbsoluteDefenseBreach()
        {
            _builder.Create(AbsoluteDefenseBreachQuestId, "Fault Line: Absolute Defense")
                .PrerequisiteQuest(AbsoluteDefenseMeasureQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseCrackedAnvilSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "The Absolute Defense capstone line continues in Korriban Forge Caverns. Defeat the Absolute Defense warden and secure the Absolute Defense Cracked Anvil Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Absolute Defense Cracked Anvil Sigil from Korriban Forge Caverns. Return to Valis Korr for the next Absolute Defense lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void AbsoluteDefenseCircle()
        {
            _builder.Create(AbsoluteDefenseCircleQuestId, "Circle of Proof: Absolute Defense")
                .PrerequisiteQuest(AbsoluteDefenseBreachQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseOverseersClearanceToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "The Absolute Defense capstone line continues in Korriban Forge Caverns. Defeat the Absolute Defense inner circle and secure the Absolute Defense Overseer's Clearance Token.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Absolute Defense Overseer's Clearance Token from Korriban Forge Caverns. Return to Valis Korr for the next Absolute Defense lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void AbsoluteDefenseMastery()
        {
            _builder.Create(AbsoluteDefenseMasteryQuestId, "Absolute Defense Mastery")
                .PrerequisiteQuest(AbsoluteDefenseCircleQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Absolute Defense master is waiting in Korriban Forge Caverns. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Absolute Defense master is defeated. Return to Valis Korr and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.AbsoluteDefense);
                });
        }

        private void SoulAscensionFoundation()
        {
            _builder.Create(SoulAscensionFoundationQuestId, "First Principle: Soul Ascension")
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneKorribanForgeCavernsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveKorribanForgeCavernsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionForgeHeatLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "The Soul Ascension capstone line continues in Korriban Forge Caverns. Defeat Soul Ascension adepts and secure the Soul Ascension Forge Heat Ledger.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Soul Ascension Forge Heat Ledger from Korriban Forge Caverns. Return to Senn Dralok for the next Soul Ascension lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SoulAscensionMeasure()
        {
            _builder.Create(SoulAscensionMeasureQuestId, "The Measure of Soul Ascension")
                .PrerequisiteQuest(SoulAscensionFoundationQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionSithTemperingMatrix)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "The Soul Ascension capstone line continues in Korriban Forge Caverns. Defeat Soul Ascension specialists and secure the Soul Ascension Sith Tempering Matrix.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Soul Ascension Sith Tempering Matrix from Korriban Forge Caverns. Return to Senn Dralok for the next Soul Ascension lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SoulAscensionBreach()
        {
            _builder.Create(SoulAscensionBreachQuestId, "Fault Line: Soul Ascension")
                .PrerequisiteQuest(SoulAscensionMeasureQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionCrackedAnvilSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "The Soul Ascension capstone line continues in Korriban Forge Caverns. Defeat the Soul Ascension warden and secure the Soul Ascension Cracked Anvil Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Soul Ascension Cracked Anvil Sigil from Korriban Forge Caverns. Return to Senn Dralok for the next Soul Ascension lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SoulAscensionCircle()
        {
            _builder.Create(SoulAscensionCircleQuestId, "Circle of Proof: Soul Ascension")
                .PrerequisiteQuest(SoulAscensionBreachQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionOverseersClearanceToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "The Soul Ascension capstone line continues in Korriban Forge Caverns. Defeat the Soul Ascension inner circle and secure the Soul Ascension Overseer's Clearance Token.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Soul Ascension Overseer's Clearance Token from Korriban Forge Caverns. Return to Senn Dralok for the next Soul Ascension lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SoulAscensionMastery()
        {
            _builder.Create(SoulAscensionMasteryQuestId, "Soul Ascension Mastery")
                .PrerequisiteQuest(SoulAscensionCircleQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Soul Ascension master is waiting in Korriban Forge Caverns. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Soul Ascension master is defeated. Return to Senn Dralok and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.SoulAscension);
                });
        }

        private static void RemoveKorribanForgeCavernsAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                HeavyVibrobladeCapstoneQuestDefinition.AbsoluteDefenseFoundationQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.AbsoluteDefenseMeasureQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.AbsoluteDefenseBreachQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.AbsoluteDefenseCircleQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.AbsoluteDefenseMasteryQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.SoulAscensionFoundationQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.SoulAscensionMeasureQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.SoulAscensionBreachQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.SoulAscensionCircleQuestId,
                HeavyVibrobladeCapstoneQuestDefinition.SoulAscensionMasteryQuestId,
                SpearCapstoneQuestDefinition.ForcebaneFoundationQuestId,
                SpearCapstoneQuestDefinition.ForcebaneMeasureQuestId,
                SpearCapstoneQuestDefinition.ForcebaneBreachQuestId,
                SpearCapstoneQuestDefinition.ForcebaneCircleQuestId,
                SpearCapstoneQuestDefinition.ForcebaneMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneKorribanForgeCavernsKey, questIds);
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
