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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneKorribanSithCryptDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveKorribanSithCryptDepthsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "Sith adepts rehearse a corrupted last-stand rite in the Korriban Sith Crypt Depths. Defeat 6 Last Stand of the Light adepts and recover the Last Stand of the Light Crypt Trial Tablet.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light Crypt Trial Tablet has been recovered. Return to Seris Nahl at the Korriban Starport.")
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
                    "Five Last Stand of the Light specialists guard the rite's focus deeper in the Korriban Sith Crypt Depths. Defeat them and recover the Last Stand of the Light Ritual Focus Shard.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light Ritual Focus Shard has been recovered. Return to Seris Nahl at the Korriban Starport.")
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
                    "The Last Stand of the Light warden holds the breach into the deep tombs of the Korriban Sith Crypt Depths. Defeat the warden and recover the Last Stand of the Light Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light Splintered Tomb Sigil has been recovered. Return to Seris Nahl at the Korriban Starport.")
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
                    "Four members of the Last Stand of the Light inner circle hold the deep chambers of the Korriban Sith Crypt Depths. Defeat them and recover the Last Stand of the Light Keeper's Rite Token.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light Keeper's Rite Token has been recovered. Return to Seris Nahl at the Korriban Starport.")
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
                    "The Last Stand of the Light master waits at the bottom of the Korriban Sith Crypt Depths. Defeat the master.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light master has been defeated. Return to Seris Nahl at the Korriban Starport.")
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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneKorribanSithCryptDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveKorribanSithCryptDepthsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "A coven of Hunger of the Dark adepts feeds on stolen power in the Korriban Sith Crypt Depths. Defeat 6 of them and recover the Hunger of the Dark Crypt Trial Tablet.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark Crypt Trial Tablet has been recovered. Return to Neth Kyr at the Korriban Starport cantina.")
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
                    "Five Hunger of the Dark specialists guard the coven's ritual focus deeper in the Korriban Sith Crypt Depths. Defeat them and recover the Hunger of the Dark Ritual Focus Shard.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark Ritual Focus Shard has been recovered. Return to Neth Kyr at the Korriban Starport cantina.")
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
                    "The Hunger of the Dark warden holds the breach into the deep tombs of the Korriban Sith Crypt Depths. Defeat the warden and recover the Hunger of the Dark Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark Splintered Tomb Sigil has been recovered. Return to Neth Kyr at the Korriban Starport cantina.")
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
                    "Four members of the Hunger of the Dark inner circle hold the deep chambers of the Korriban Sith Crypt Depths. Defeat them and recover the Hunger of the Dark Keeper's Rite Token.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark Keeper's Rite Token has been recovered. Return to Neth Kyr at the Korriban Starport cantina.")
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
                    "The Hunger of the Dark master waits at the bottom of the Korriban Sith Crypt Depths. Defeat the master.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark master has been defeated. Return to Neth Kyr at the Korriban Starport cantina.")
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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneKorribanSithCryptDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveKorribanSithCryptDepthsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "A failed cell of Eclipse of Resolve adepts drills a corrupted doctrine in the Korriban Sith Crypt Depths. Defeat 6 of them and recover the Eclipse of Resolve Crypt Trial Tablet.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve Crypt Trial Tablet has been recovered. Return to Acolyte Varn at the Korriban wasteland tunnels.")
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
                    "Five Eclipse of Resolve specialists guard the cell's ritual focus deeper in the Korriban Sith Crypt Depths. Defeat them and recover the Eclipse of Resolve Ritual Focus Shard.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve Ritual Focus Shard has been recovered. Return to Acolyte Varn at the Korriban wasteland tunnels.")
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
                    "The Eclipse of Resolve warden holds the breach into the deep tombs of the Korriban Sith Crypt Depths. Defeat the warden and recover the Eclipse of Resolve Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve Splintered Tomb Sigil has been recovered. Return to Acolyte Varn at the Korriban wasteland tunnels.")
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
                    "Four members of the Eclipse of Resolve inner circle hold the deep chambers of the Korriban Sith Crypt Depths. Defeat them and recover the Eclipse of Resolve Keeper's Rite Token.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve Keeper's Rite Token has been recovered. Return to Acolyte Varn at the Korriban wasteland tunnels.")
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
                    "The Eclipse of Resolve master waits at the bottom of the Korriban Sith Crypt Depths. Defeat the master.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve master has been defeated. Return to Acolyte Varn at the Korriban wasteland tunnels.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.EclipseOfResolve);
                });
        }

        private static void RemoveKorribanSithCryptDepthsAccessIfNoLongerNeeded(uint player)
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

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneKorribanSithCryptDepthsKey, questIds);
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
