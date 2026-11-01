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
    public class ForceCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        private const string LastStandOfTheLightFoundationQuestId = "last_stand_of_the_light_foundation";
        private const string LastStandOfTheLightMeasureQuestId = "last_stand_of_the_light_measure";
        private const string LastStandOfTheLightBreachQuestId = "last_stand_of_the_light_breach";
        private const string LastStandOfTheLightCircleQuestId = "last_stand_of_the_light_circle";
        internal const string LastStandOfTheLightMasteryQuestId = "last_stand_of_the_light_mastery";
        internal const string LastStandOfTheLightAdeptResref = "cp_lightstand_ad";
        internal const string LastStandOfTheLightSpecialistResref = "cp_lightstand_sp";
        internal const string LastStandOfTheLightInnerCircleResref = "cp_lightstand_ic";
        private const string HungerOfTheDarkFoundationQuestId = "hunger_of_the_dark_foundation";
        private const string HungerOfTheDarkMeasureQuestId = "hunger_of_the_dark_measure";
        private const string HungerOfTheDarkBreachQuestId = "hunger_of_the_dark_breach";
        private const string HungerOfTheDarkCircleQuestId = "hunger_of_the_dark_circle";
        internal const string HungerOfTheDarkMasteryQuestId = "hunger_of_the_dark_mastery";
        internal const string HungerOfTheDarkAdeptResref = "cp_darkhung_ad";
        internal const string HungerOfTheDarkSpecialistResref = "cp_darkhung_sp";
        internal const string HungerOfTheDarkInnerCircleResref = "cp_darkhung_ic";
        private const string EclipseOfResolveFoundationQuestId = "eclipse_of_resolve_foundation";
        private const string EclipseOfResolveMeasureQuestId = "eclipse_of_resolve_measure";
        private const string EclipseOfResolveBreachQuestId = "eclipse_of_resolve_breach";
        private const string EclipseOfResolveCircleQuestId = "eclipse_of_resolve_circle";
        internal const string EclipseOfResolveMasteryQuestId = "eclipse_of_resolve_mastery";
        internal const string EclipseOfResolveAdeptResref = "cp_eclipse_ad";
        internal const string EclipseOfResolveSpecialistResref = "cp_eclipse_sp";
        internal const string EclipseOfResolveInnerCircleResref = "cp_eclipse_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            LastStandOfTheLightFoundation();
            LastStandOfTheLightMeasure();
            LastStandOfTheLightBreach();
            LastStandOfTheLightCircle();
            LastStandOfTheLightMastery();
            HungerOfTheDarkFoundation();
            HungerOfTheDarkMeasure();
            HungerOfTheDarkBreach();
            HungerOfTheDarkCircle();
            HungerOfTheDarkMastery();
            EclipseOfResolveFoundation();
            EclipseOfResolveMeasure();
            EclipseOfResolveBreach();
            EclipseOfResolveCircle();
            EclipseOfResolveMastery();

            return _builder.Build();
        }

        private void LastStandOfTheLightFoundation()
        {
            _builder.Create(LastStandOfTheLightFoundationQuestId, "Lower Your Voice")
                .PrerequisiteSkill(SkillType.Force, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneMonCalaSithCryptDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveMonCalaSithCryptDepthsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "Seris Nahl gave you the Mon Cala Sith Crypt Depths Key. On Mon Cala, travel from Dac City east through the Coral Isles, then north-east through the Sharptooth Jungle to the cave mouth on the north-east side of the northern jungle. In the first cavern of the Sharptooth Jungle Caves, enter the Sith Crypt Depths through the sealed door in the purple stone on the north side. Defeat six Expedition Raiders, the thieves nearest the entrance, and recover the Last Stand of the Light Crypt Trial Tablet. Any Expedition Raider counts, whichever crew it rides with, but only kills made while you shield an ally with Force Intercept.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_CryptRaider, "last_stand_of_the_light", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Last Stand of the Light Crypt Trial Tablet. Return it to Seris Nahl at the Promenade on Smuggler's Moon.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void LastStandOfTheLightMeasure()
        {
            _builder.Create(LastStandOfTheLightMeasureQuestId, "What the Shard Remembers")
                .PrerequisiteQuest(LastStandOfTheLightFoundationQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Expedition Marauders deeper in the Sith Crypt Depths on Mon Cala and recover the Last Stand of the Light Ritual Focus Shard. Any Expedition Marauder counts, whichever crew it rides with, but only kills made while you shield an ally with Force Intercept.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_CryptMarauder, "last_stand_of_the_light", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Last Stand of the Light Ritual Focus Shard. Return it to Seris Nahl at the Promenade on Smuggler's Moon.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void LastStandOfTheLightBreach()
        {
            _builder.Create(LastStandOfTheLightBreachQuestId, "Three Blades at the Breach")
                .PrerequisiteQuest(LastStandOfTheLightMeasureQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Expedition Captain Rhaan Tolek at the trial altar on the west side of the Sith Crypt Depths on Mon Cala. Recover the Last Stand of the Light Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.MonCala_CryptBreachCaptain, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Last Stand of the Light Splintered Tomb Sigil. Return it to Seris Nahl at the Promenade on Smuggler's Moon.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void LastStandOfTheLightCircle()
        {
            _builder.Create(LastStandOfTheLightCircleQuestId, "The Rite Unravels")
                .PrerequisiteQuest(LastStandOfTheLightBreachQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat four Expedition Reavers in the deepest part of the Sith Crypt Depths on Mon Cala and recover the Last Stand of the Light Keeper's Rite Token. Any Expedition Reaver counts, whichever crew it rides with, but only kills made while you shield an ally with Force Intercept.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_CryptReaver, "last_stand_of_the_light", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Last Stand of the Light Keeper's Rite Token. Return it to Seris Nahl at the Promenade on Smuggler's Moon.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void LastStandOfTheLightMastery()
        {
            _builder.Create(LastStandOfTheLightMasteryQuestId, "The Light's Last Stand")
                .PrerequisiteQuest(LastStandOfTheLightCircleQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Expedition Leader Kaelen Dorr in the Final Ritual Chamber, through the door at the north end of the Sith Crypt Depths on Mon Cala.")
                .AddKillObjective(NPCGroupType.MonCala_CryptExpeditionLeader, 1)

                .AddState()
                .SetStateJournalText(
                    "Expedition Leader Kaelen Dorr is dead. Return to Seris Nahl at the Promenade on Smuggler's Moon.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.LastStandOfTheLight);
                });
        }

        private void HungerOfTheDarkFoundation()
        {
            _builder.Create(HungerOfTheDarkFoundationQuestId, "An Appetite Worth Teaching")
                .PrerequisiteSkill(SkillType.Force, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneMonCalaSithCryptDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveMonCalaSithCryptDepthsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "Neth Kyr gave you the Mon Cala Sith Crypt Depths Key. On Mon Cala, travel from Dac City east through the Coral Isles, then north-east through the Sharptooth Jungle to the cave mouth on the north-east side of the northern jungle. In the first cavern of the Sharptooth Jungle Caves, enter the Sith Crypt Depths through the sealed door in the purple stone on the north side. Defeat six Expedition Raiders, the hirelings nearest the entrance, and recover the Hunger of the Dark Crypt Trial Tablet. Any Expedition Raider counts, whichever crew it rides with, but only kills made with Force Drain, Force Lightning or Force Spark.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_VitaeReaverInitiate, "hunger_of_the_dark", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Hunger of the Dark Crypt Trial Tablet. Return it to Neth Kyr at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void HungerOfTheDarkMeasure()
        {
            _builder.Create(HungerOfTheDarkMeasureQuestId, "Second Course")
                .PrerequisiteQuest(HungerOfTheDarkFoundationQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Expedition Marauders deeper in the Sith Crypt Depths on Mon Cala and recover the Hunger of the Dark Ritual Focus Shard. Any Expedition Marauder counts, whichever crew it rides with, but only kills made with Force Drain, Force Lightning or Force Spark.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_VitaeReaverAdept, "hunger_of_the_dark", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Hunger of the Dark Ritual Focus Shard. Return it to Neth Kyr at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void HungerOfTheDarkBreach()
        {
            _builder.Create(HungerOfTheDarkBreachQuestId, "A Full Table")
                .PrerequisiteQuest(HungerOfTheDarkMeasureQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Expedition Captain Zarrik Thane at the trial altar on the east side of the Sith Crypt Depths on Mon Cala. Recover the Hunger of the Dark Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.MonCala_VitaeReaverEnforcer, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Hunger of the Dark Splintered Tomb Sigil. Return it to Neth Kyr at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void HungerOfTheDarkCircle()
        {
            _builder.Create(HungerOfTheDarkCircleQuestId, "Small Enough to Swallow")
                .PrerequisiteQuest(HungerOfTheDarkBreachQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat four Expedition Reavers in the deepest part of the Sith Crypt Depths on Mon Cala and recover the Hunger of the Dark Keeper's Rite Token. Any Expedition Reaver counts, whichever crew it rides with, but only kills made with Force Drain, Force Lightning or Force Spark.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_VitaeReaverBinder, "hunger_of_the_dark", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Hunger of the Dark Keeper's Rite Token. Return it to Neth Kyr at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void HungerOfTheDarkMastery()
        {
            _builder.Create(HungerOfTheDarkMasteryQuestId, "Too Much")
                .PrerequisiteQuest(HungerOfTheDarkCircleQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Expedition Leader Mordek Sull in the Final Ritual Chamber, through the door at the north end of the Sith Crypt Depths on Mon Cala.")
                .AddKillObjective(NPCGroupType.MonCala_VitaeReaverChief, 1)

                .AddState()
                .SetStateJournalText(
                    "Expedition Leader Mordek Sull is dead. Return to Neth Kyr at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.HungerOfTheDark);
                });
        }

        private void EclipseOfResolveFoundation()
        {
            _builder.Create(EclipseOfResolveFoundationQuestId, "Provisional Pass")
                .PrerequisiteSkill(SkillType.Force, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneMonCalaSithCryptDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveMonCalaSithCryptDepthsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "Acolyte Varn gave you the Mon Cala Sith Crypt Depths Key. On Mon Cala, travel from Dac City east through the Coral Isles, then north-east through the Sharptooth Jungle to the cave mouth on the north-east side of the northern jungle. In the first cavern of the Sharptooth Jungle Caves, enter the Sith Crypt Depths through the sealed door in the purple stone on the north side. Defeat six Expedition Raiders, the hirelings nearest the entrance, and recover the Eclipse of Resolve Crypt Trial Tablet. Any Expedition Raider counts, whichever crew it rides with, but only kills made after weakening their resolve.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_UmbralRaiderScout, "eclipse_of_resolve", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Eclipse of Resolve Crypt Trial Tablet. Return it to Acolyte Varn in the Shipping District on Smuggler's Moon.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void EclipseOfResolveMeasure()
        {
            _builder.Create(EclipseOfResolveMeasureQuestId, "Doctrinal Review")
                .PrerequisiteQuest(EclipseOfResolveFoundationQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Expedition Marauders deeper in the Sith Crypt Depths on Mon Cala and recover the Eclipse of Resolve Ritual Focus Shard. Any Expedition Marauder counts, whichever crew it rides with, but only kills made after weakening their resolve.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_UmbralRaiderAdept, "eclipse_of_resolve", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Eclipse of Resolve Ritual Focus Shard. Return it to Acolyte Varn in the Shipping District on Smuggler's Moon.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void EclipseOfResolveBreach()
        {
            _builder.Create(EclipseOfResolveBreachQuestId, "Adequate Work")
                .PrerequisiteQuest(EclipseOfResolveMeasureQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Expedition Captain Tavik Orrun at the trial altar in the north-west of the Sith Crypt Depths on Mon Cala. Recover the Eclipse of Resolve Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.MonCala_UmbralRaiderCaptain, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Eclipse of Resolve Splintered Tomb Sigil. Return it to Acolyte Varn in the Shipping District on Smuggler's Moon.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void EclipseOfResolveCircle()
        {
            _builder.Create(EclipseOfResolveCircleQuestId, "The Retraction")
                .PrerequisiteQuest(EclipseOfResolveBreachQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat four Expedition Reavers in the deepest part of the Sith Crypt Depths on Mon Cala and recover the Eclipse of Resolve Keeper's Rite Token. Any Expedition Reaver counts, whichever crew it rides with, but only kills made after weakening their resolve.")
                .AddCapstoneProofKillObjective(NPCGroupType.MonCala_UmbralRaiderSaboteur, "eclipse_of_resolve", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Eclipse of Resolve Keeper's Rite Token. Return it to Acolyte Varn in the Shipping District on Smuggler's Moon.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void EclipseOfResolveMastery()
        {
            _builder.Create(EclipseOfResolveMasteryQuestId, "What Eclipsed His Resolve")
                .PrerequisiteQuest(EclipseOfResolveCircleQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Expedition Leader Lorrek Syn in the Final Ritual Chamber, through the door at the north end of the Sith Crypt Depths on Mon Cala.")
                .AddKillObjective(NPCGroupType.MonCala_UmbralRaiderCommander, 1)

                .AddState()
                .SetStateJournalText(
                    "Expedition Leader Lorrek Syn is dead. Return to Acolyte Varn in the Shipping District on Smuggler's Moon.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.EclipseOfResolve);
                });
        }

        private static void RemoveMonCalaSithCryptDepthsAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                ForceCapstoneQuestDefinition.LastStandOfTheLightFoundationQuestId,
                ForceCapstoneQuestDefinition.LastStandOfTheLightMeasureQuestId,
                ForceCapstoneQuestDefinition.LastStandOfTheLightBreachQuestId,
                ForceCapstoneQuestDefinition.LastStandOfTheLightCircleQuestId,
                ForceCapstoneQuestDefinition.LastStandOfTheLightMasteryQuestId,
                ForceCapstoneQuestDefinition.HungerOfTheDarkFoundationQuestId,
                ForceCapstoneQuestDefinition.HungerOfTheDarkMeasureQuestId,
                ForceCapstoneQuestDefinition.HungerOfTheDarkBreachQuestId,
                ForceCapstoneQuestDefinition.HungerOfTheDarkCircleQuestId,
                ForceCapstoneQuestDefinition.HungerOfTheDarkMasteryQuestId,
                ForceCapstoneQuestDefinition.EclipseOfResolveFoundationQuestId,
                ForceCapstoneQuestDefinition.EclipseOfResolveMeasureQuestId,
                ForceCapstoneQuestDefinition.EclipseOfResolveBreachQuestId,
                ForceCapstoneQuestDefinition.EclipseOfResolveCircleQuestId,
                ForceCapstoneQuestDefinition.EclipseOfResolveMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneMonCalaSithCryptDepthsKey, questIds);
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
