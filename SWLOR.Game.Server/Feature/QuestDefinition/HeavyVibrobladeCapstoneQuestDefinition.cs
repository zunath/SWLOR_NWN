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
            _builder.Create(AbsoluteDefenseFoundationQuestId, "Proving the Billet")
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
                    "Enter the Korriban Forge Caverns on Korriban and defeat 6 Absolute Defense adepts. Secure the Absolute Defense Forge Heat Ledger they keep near the crucibles.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "The Absolute Defense Forge Heat Ledger is in hand. Return it to Valis Korr at the Korriban Sith Academy.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void AbsoluteDefenseMeasure()
        {
            _builder.Create(AbsoluteDefenseMeasureQuestId, "Drawing the Temper")
                .PrerequisiteQuest(AbsoluteDefenseFoundationQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseSithTemperingMatrix)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "Defeat 5 Absolute Defense specialists in the Korriban Forge Caverns on Korriban and recover the Absolute Defense Sith Tempering Matrix from the quench racks.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Absolute Defense Sith Tempering Matrix. Bring it to Valis Korr at the Korriban Sith Academy.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void AbsoluteDefenseBreach()
        {
            _builder.Create(AbsoluteDefenseBreachQuestId, "The Quench Test")
                .PrerequisiteQuest(AbsoluteDefenseMeasureQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseCrackedAnvilSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Absolute Defense warden in the Korriban Forge Caverns on Korriban and take the Absolute Defense Cracked Anvil Sigil he wears. The warden is a deadly opponent; bring allies.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is defeated and the Absolute Defense Cracked Anvil Sigil is yours. Return to Valis Korr at the Korriban Sith Academy.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void AbsoluteDefenseCircle()
        {
            _builder.Create(AbsoluteDefenseCircleQuestId, "Ring True")
                .PrerequisiteQuest(AbsoluteDefenseBreachQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAbsoluteDefenseOverseersClearanceToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAbsoluteDefenseOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat 4 members of the Absolute Defense inner circle in the Korriban Forge Caverns on Korriban and secure an Absolute Defense Overseer's Clearance Token.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAbsoluteDefenseOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "You took the Absolute Defense Overseer's Clearance Token from the inner circle. Deliver it to Valis Korr at the Korriban Sith Academy.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void AbsoluteDefenseMastery()
        {
            _builder.Create(AbsoluteDefenseMasteryQuestId, "The Final Temper")
                .PrerequisiteQuest(AbsoluteDefenseCircleQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Absolute Defense master in the innermost ring of the Korriban Forge Caverns on Korriban. He is far too strong to face alone; bring trusted allies.")
                .AddKillObjective(NPCGroupType.Korriban_AbsoluteDefense_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Absolute Defense master is defeated. Return to Valis Korr at the Korriban Sith Academy.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.AbsoluteDefense);
                });
        }

        private void SoulAscensionFoundation()
        {
            _builder.Create(SoulAscensionFoundationQuestId, "Let the Blade Drink")
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
                    "Enter the Korriban Forge Caverns on Korriban and defeat 6 Soul Ascension adepts. Secure the Soul Ascension Forge Heat Ledger kept near the crucibles.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "The Soul Ascension Forge Heat Ledger is in hand. Return it to Senn Dralok at the Korriban wasteland outpost.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SoulAscensionMeasure()
        {
            _builder.Create(SoulAscensionMeasureQuestId, "The Offering, Weighed")
                .PrerequisiteQuest(SoulAscensionFoundationQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionSithTemperingMatrix)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "Defeat 5 Soul Ascension specialists in the Korriban Forge Caverns on Korriban and recover the Soul Ascension Sith Tempering Matrix from the quench troughs.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Soul Ascension Sith Tempering Matrix. Bring it to Senn Dralok at the Korriban wasteland outpost.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SoulAscensionBreach()
        {
            _builder.Create(SoulAscensionBreachQuestId, "The Vessel Breaks")
                .PrerequisiteQuest(SoulAscensionMeasureQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionCrackedAnvilSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Soul Ascension warden in the Korriban Forge Caverns on Korriban and take the Soul Ascension Cracked Anvil Sigil he wears. The warden is a deadly opponent; bring allies.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is defeated and the Soul Ascension Cracked Anvil Sigil is yours. Return to Senn Dralok at the Korriban wasteland outpost.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SoulAscensionCircle()
        {
            _builder.Create(SoulAscensionCircleQuestId, "A Congregation of Thirst")
                .PrerequisiteQuest(SoulAscensionBreachQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSoulAscensionOverseersClearanceToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSoulAscensionOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat 4 members of the Soul Ascension inner circle in the Korriban Forge Caverns on Korriban and secure a Soul Ascension Overseer's Clearance Token.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSoulAscensionOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "You took the Soul Ascension Overseer's Clearance Token from the inner circle. Deliver it to Senn Dralok at the Korriban wasteland outpost.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SoulAscensionMastery()
        {
            _builder.Create(SoulAscensionMasteryQuestId, "So the Soul Ascends")
                .PrerequisiteQuest(SoulAscensionCircleQuestId)
                .PrerequisiteSkill(SkillType.HeavyVibroblade, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Soul Ascension master in his sanctum in the Korriban Forge Caverns on Korriban. He is far too strong to face alone; bring trusted allies.")
                .AddKillObjective(NPCGroupType.Korriban_SoulAscension_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Soul Ascension master is defeated. Return to Senn Dralok at the Korriban wasteland outpost.")
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
