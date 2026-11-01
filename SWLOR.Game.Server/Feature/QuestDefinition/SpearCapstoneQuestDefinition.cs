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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneTatooineForgeCavernsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveTatooineForgeCavernsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForcebaneForgeHeatLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForcebaneForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "Maar Veth gave you the Tatooine Forge Caverns Key. On Tatooine, leave the Southern Entrance of Anchorhead by its east side into the Rocky Desert, then head north into the North Mine Cliffs and take the ramp down into the Forge Caverns under the arched mine tunnel entrance a short walk ahead, beside an old mine cart. Kill six Forgebound Initiates, who drill at the outer forges nearest the entrance, and secure the Forcebane Forge Heat Ledger they keep by the crucibles. Any Forgebound Initiate counts, whatever form it fights in, but only kills made after clouding them with Disabling Strike or Disruption Field.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_ForgeboundInitiates, "forcebane", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneForgeHeatLedger)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Forge Heat Ledger is secured. Return it to Maar Veth at the Anchorhead - Southern Entrance on Tatooine.")
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
                    "Kill five Forgebound Journeymen along the tempering line deeper in the Forge Caverns on Tatooine and secure the Forcebane Sith Tempering Matrix. Any Forgebound Journeyman counts, whatever form it fights in, but only kills made after clouding them with Disabling Strike or Disruption Field.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_ForgeboundJourneymans, "forcebane", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneSithTemperingMatrix)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Sith Tempering Matrix is secured. Return it to Maar Veth at the Anchorhead - Southern Entrance on Tatooine.")
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
                    "With at least two allies, kill the warden, Forge Warden Orbek Tal, at the trial altar on the east side of the Forge Caverns on Tatooine and secure the Forcebane Cracked Anvil Sigil.")
                .AddKillObjective(NPCGroupType.Tatooine_ForgeWardenOrbekTal, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneCrackedAnvilSigil)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Cracked Anvil Sigil is secured. Return it to Maar Veth at the Anchorhead - Southern Entrance on Tatooine.")
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
                    "Kill four Forgebound Overseers in the deepest part of the Forge Caverns on Tatooine and secure the Forcebane Overseer's Clearance Token. Any Forgebound Overseer counts, whatever form it fights in, but only kills made after clouding them with Disabling Strike or Disruption Field.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_ForgeboundOverseers, "forcebane", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForcebaneOverseersClearanceToken)

                .AddState()
                .SetStateJournalText(
                    "The Forcebane Overseer's Clearance Token is secured. Return it to Maar Veth at the Anchorhead - Southern Entrance on Tatooine.")
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
                    "With at least two allies, defeat the master, Forge Master Kovath Sull, in the Champion Forge, through the door at the north end of the Forge Caverns on Tatooine. His defeat is the only proof required.")
                .AddKillObjective(NPCGroupType.Tatooine_ForgeMasterKovathSull, 1)

                .AddState()
                .SetStateJournalText(
                    "Forge Master Kovath Sull is defeated. Return to Maar Veth at the Anchorhead - Southern Entrance on Tatooine.")
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
                    "Dax Rell gave you the Smuggler's Moon Fight Club Backrooms Key. Take the elevator down from The Slums to the Fight Club on Smuggler's Moon and use the key on the Back Rooms door on its far west wall. Kill six Stable Brawlers, who work the floor nearest the Back Rooms door in the Smuggler's Moon Fight Club Backrooms, and secure the Crippling Defense Backroom Bout Ledger. Any Stable Brawler counts, whatever form it fights in, but only kills made with your heaviest spear techniques.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_StableBrawlers, "crippling_defense", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefenseBackroomBoutLedger)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Backroom Bout Ledger is secured. Return it to Dax Rell at the Hyper Dive Cantina on Smuggler's Moon.")
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
                    "Kill five Stable Pit Fighters deeper in the Smuggler's Moon Fight Club Backrooms, behind the Fight Club on Smuggler's Moon, and secure the Crippling Defense Ring Shock Regulator that runs their rigged rings. Any Stable Pit Fighter counts, whatever form it fights in, but only kills made with your heaviest spear techniques.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_StablePitFighters, "crippling_defense", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefenseRingShockRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Ring Shock Regulator is secured. Return it to Dax Rell at the Hyper Dive Cantina on Smuggler's Moon.")
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
                    "Kill the warden, Pit Boss Ssarok, at the champion pit gate in the Smuggler's Moon Fight Club Backrooms, behind the Fight Club on Smuggler's Moon, and secure the Crippling Defense Cracked Pit Sigil. Dax Rell's rule stands: bring two fighters you trust.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_PitBossSsarok, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefenseCrackedPitSigil)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Cracked Pit Sigil is secured. Return it to Dax Rell at the Hyper Dive Cantina on Smuggler's Moon.")
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
                    "Kill four Stable Promoters in the deepest part of the Smuggler's Moon Fight Club Backrooms, behind the Fight Club on Smuggler's Moon, and secure the Crippling Defense Promoter's Payout Chit. Any Stable Promoter counts, whatever form it fights in, but only kills made with your heaviest spear techniques.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_StablePromoters, "crippling_defense", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneCripplingDefensePromotersPayoutChit)

                .AddState()
                .SetStateJournalText(
                    "The Crippling Defense Promoter's Payout Chit is secured. Return it to Dax Rell at the Hyper Dive Cantina on Smuggler's Moon.")
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
                    "Defeat the master, Stable Champion Thrask, past the last gate of the Smuggler's Moon Fight Club Backrooms, behind the Fight Club on Smuggler's Moon. Nobody faces him alone; bring two fighters you trust. His defeat is the only proof required.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_StableChampionThrask, 1)

                .AddState()
                .SetStateJournalText(
                    "Stable Champion Thrask is defeated. Return to Dax Rell at the Hyper Dive Cantina on Smuggler's Moon.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.CripplingDefense);
                });
        }

        private static void RemoveTatooineForgeCavernsAccessIfNoLongerNeeded(uint player)
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

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneTatooineForgeCavernsKey, questIds);
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
