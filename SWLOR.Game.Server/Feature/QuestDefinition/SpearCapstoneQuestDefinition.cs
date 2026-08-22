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
    public class SpearCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string ForcebaneFoundationQuestId = "forcebane_foundation";
        internal const string ForcebaneMeasureQuestId = "forcebane_measure";
        internal const string ForcebaneBreachQuestId = "forcebane_breach";
        internal const string ForcebaneCircleQuestId = "forcebane_circle";
        internal const string ForcebaneMasteryQuestId = "forcebane_mastery";
        internal const string ForcebaneAdeptResref = "cp_forcebane_ad";
        internal const string ForcebaneSpecialistResref = "cp_forcebane_sp";
        internal const string ForcebaneInnerCircleResref = "cp_forcebane_ic";
        internal const string CripplingDefenseFoundationQuestId = "crippling_defense_foundation";
        internal const string CripplingDefenseMeasureQuestId = "crippling_defense_measure";
        internal const string CripplingDefenseBreachQuestId = "crippling_defense_breach";
        internal const string CripplingDefenseCircleQuestId = "crippling_defense_circle";
        internal const string CripplingDefenseMasteryQuestId = "crippling_defense_mastery";
        internal const string CripplingDefenseAdeptResref = "cp_cripdef_ad";
        internal const string CripplingDefenseSpecialistResref = "cp_cripdef_sp";
        internal const string CripplingDefenseInnerCircleResref = "cp_cripdef_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            ForcebaneFoundation();
            ForcebaneMeasure();
            ForcebaneBreach();
            ForcebaneCircle();
            ForcebaneMastery();
            CripplingDefenseFoundation();
            CripplingDefenseMeasure();
            CripplingDefenseBreach();
            CripplingDefenseCircle();
            CripplingDefenseMastery();

            return _builder.Build();
        }

        private void ForcebaneFoundation()
        {
            _builder.Create(ForcebaneFoundationQuestId, "Reach Ends Ritual")
                .PrerequisiteSkill(SkillType.Spear, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneKorribanForgeCavernsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveKorribanForgeCavernsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForcebaneForgeHeatLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForcebaneForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "Kill six Forcebane adepts at the outer forges of the Korriban Forge Caverns on Korriban and secure the Forcebane Forge Heat Ledger.")
                .AddKillObjective(NPCGroupType.Korriban_Forcebane_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Forge Heat Ledger is secured. Return it to Maar Veth at the Valley of the Dark Lords on Korriban.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ForcebaneMeasure()
        {
            _builder.Create(ForcebaneMeasureQuestId, "Test the Iron, Not the Word")
                .PrerequisiteQuest(ForcebaneFoundationQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForcebaneSithTemperingMatrix)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForcebaneSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "Kill five Forcebane specialists along the tempering line of the Korriban Forge Caverns on Korriban and secure the Forcebane Sith Tempering Matrix.")
                .AddKillObjective(NPCGroupType.Korriban_Forcebane_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Sith Tempering Matrix is secured. Return it to Maar Veth at the Valley of the Dark Lords on Korriban.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ForcebaneBreach()
        {
            _builder.Create(ForcebaneBreachQuestId, "Never Duel a Warden Fairly")
                .PrerequisiteQuest(ForcebaneMeasureQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForcebaneCrackedAnvilSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForcebaneCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "Kill the Forcebane warden in the deep forge of the Korriban Forge Caverns on Korriban and secure the Forcebane Cracked Anvil Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_Forcebane_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Cracked Anvil Sigil is secured. Return it to Maar Veth at the Valley of the Dark Lords on Korriban.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ForcebaneCircle()
        {
            _builder.Create(ForcebaneCircleQuestId, "Leave No Circle Standing")
                .PrerequisiteQuest(ForcebaneBreachQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForcebaneOverseersClearanceToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForcebaneOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "Kill the four Forcebane inner circle overseers in the Korriban Forge Caverns on Korriban and secure the Forcebane Overseer's Clearance Token.")
                .AddKillObjective(NPCGroupType.Korriban_Forcebane_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Overseer's Clearance Token is secured. Return it to Maar Veth at the Valley of the Dark Lords on Korriban.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ForcebaneMastery()
        {
            _builder.Create(ForcebaneMasteryQuestId, "Finish What You Teach")
                .PrerequisiteQuest(ForcebaneCircleQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Forcebane master at the heart of the Korriban Forge Caverns on Korriban. His defeat is the only proof required.")
                .AddKillObjective(NPCGroupType.Korriban_Forcebane_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane master is defeated. Return to Maar Veth at the Valley of the Dark Lords on Korriban to complete the lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.Forcebane);
                });
        }

        private void CripplingDefenseFoundation()
        {
            _builder.Create(CripplingDefenseFoundationQuestId, "Open Trials, No Purse")
                .PrerequisiteSkill(SkillType.Spear, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneSmugglersMoonFightClubBackroomsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveSmugglersMoonFightClubBackroomsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneCripplingDefenseBackroomBoutLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneCripplingDefenseBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "Kill six Crippling Defense adepts in the sign-up rooms of the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and secure the Crippling Defense Backroom Bout Ledger.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_CripplingDefense_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefenseBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Backroom Bout Ledger is secured. Return it to Dax Rell at the Hyper Dive Cantina on Nar Shaddaa.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void CripplingDefenseMeasure()
        {
            _builder.Create(CripplingDefenseMeasureQuestId, "Undercard, Live Steel")
                .PrerequisiteQuest(CripplingDefenseFoundationQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneCripplingDefenseRingShockRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneCripplingDefenseRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "Kill five Crippling Defense specialists in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and secure the Crippling Defense Ring Shock Regulator from the main pit's control box.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_CripplingDefense_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefenseRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Ring Shock Regulator is secured. Return it to Dax Rell at the Hyper Dive Cantina on Nar Shaddaa.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void CripplingDefenseBreach()
        {
            _builder.Create(CripplingDefenseBreachQuestId, "Eliminator at the Cage Door")
                .PrerequisiteQuest(CripplingDefenseMeasureQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneCripplingDefenseCrackedPitSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneCripplingDefenseCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "Kill the Crippling Defense warden at the champion pit gate in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and secure the Crippling Defense Cracked Pit Sigil.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_CripplingDefense_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefenseCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Cracked Pit Sigil is secured. Return it to Dax Rell at the Hyper Dive Cantina on Nar Shaddaa.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void CripplingDefenseCircle()
        {
            _builder.Create(CripplingDefenseCircleQuestId, "The Promoters' Table")
                .PrerequisiteQuest(CripplingDefenseBreachQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneCripplingDefensePromotersPayoutChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneCripplingDefensePromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "Kill the four Crippling Defense inner circle promoters in the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa and secure the Crippling Defense Promoter's Payout Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_CripplingDefense_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefensePromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Promoter's Payout Chit is secured. Return it to Dax Rell at the Hyper Dive Cantina on Nar Shaddaa.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void CripplingDefenseMastery()
        {
            _builder.Create(CripplingDefenseMasteryQuestId, "Last Bout on the Book")
                .PrerequisiteQuest(CripplingDefenseCircleQuestId)
                .PrerequisiteSkill(SkillType.Spear, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Crippling Defense master past the last gate of the Smuggler's Moon Fight Club Backrooms on Nar Shaddaa. His defeat is the only proof required.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_CripplingDefense_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense master is defeated. Return to Dax Rell at the Hyper Dive Cantina on Nar Shaddaa to complete the lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.CripplingDefense);
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
