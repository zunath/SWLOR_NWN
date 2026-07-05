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
            _builder.Create(LastStandOfTheLightFoundationQuestId, "First Principle: Last Stand of the Light")
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
                    "The Last Stand of the Light capstone line continues in Korriban Sith Crypt Depths. Defeat Last Stand of the Light adepts and secure the Last Stand of the Light Crypt Trial Tablet.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Stand of the Light Crypt Trial Tablet from Korriban Sith Crypt Depths. Return to Seris Nahl for the next Last Stand of the Light lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void LastStandOfTheLightMeasure()
        {
            _builder.Create(LastStandOfTheLightMeasureQuestId, "The Measure of Last Stand of the Light")
                .PrerequisiteQuest(LastStandOfTheLightFoundationQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light capstone line continues in Korriban Sith Crypt Depths. Defeat Last Stand of the Light specialists and secure the Last Stand of the Light Ritual Focus Shard.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Stand of the Light Ritual Focus Shard from Korriban Sith Crypt Depths. Return to Seris Nahl for the next Last Stand of the Light lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void LastStandOfTheLightBreach()
        {
            _builder.Create(LastStandOfTheLightBreachQuestId, "Fault Line: Last Stand of the Light")
                .PrerequisiteQuest(LastStandOfTheLightMeasureQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light capstone line continues in Korriban Sith Crypt Depths. Defeat the Last Stand of the Light warden and secure the Last Stand of the Light Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Stand of the Light Splintered Tomb Sigil from Korriban Sith Crypt Depths. Return to Seris Nahl for the next Last Stand of the Light lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void LastStandOfTheLightCircle()
        {
            _builder.Create(LastStandOfTheLightCircleQuestId, "Circle of Proof: Last Stand of the Light")
                .PrerequisiteQuest(LastStandOfTheLightBreachQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "The Last Stand of the Light capstone line continues in Korriban Sith Crypt Depths. Defeat the Last Stand of the Light inner circle and secure the Last Stand of the Light Keeper's Rite Token.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastStandOfTheLightKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Stand of the Light Keeper's Rite Token from Korriban Sith Crypt Depths. Return to Seris Nahl for the next Last Stand of the Light lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void LastStandOfTheLightMastery()
        {
            _builder.Create(LastStandOfTheLightMasteryQuestId, "Last Stand of the Light Mastery")
                .PrerequisiteQuest(LastStandOfTheLightCircleQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Last Stand of the Light master is waiting in Korriban Sith Crypt Depths. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Korriban_LastStandOfTheLight_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Last Stand of the Light master is defeated. Return to Seris Nahl and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.LastStandOfTheLight);
                });
        }

        private void HungerOfTheDarkFoundation()
        {
            _builder.Create(HungerOfTheDarkFoundationQuestId, "First Principle: Hunger of the Dark")
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
                    "The Hunger of the Dark capstone line continues in Korriban Sith Crypt Depths. Defeat Hunger of the Dark adepts and secure the Hunger of the Dark Crypt Trial Tablet.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hunger of the Dark Crypt Trial Tablet from Korriban Sith Crypt Depths. Return to Neth Kyr for the next Hunger of the Dark lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void HungerOfTheDarkMeasure()
        {
            _builder.Create(HungerOfTheDarkMeasureQuestId, "The Measure of Hunger of the Dark")
                .PrerequisiteQuest(HungerOfTheDarkFoundationQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark capstone line continues in Korriban Sith Crypt Depths. Defeat Hunger of the Dark specialists and secure the Hunger of the Dark Ritual Focus Shard.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hunger of the Dark Ritual Focus Shard from Korriban Sith Crypt Depths. Return to Neth Kyr for the next Hunger of the Dark lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void HungerOfTheDarkBreach()
        {
            _builder.Create(HungerOfTheDarkBreachQuestId, "Fault Line: Hunger of the Dark")
                .PrerequisiteQuest(HungerOfTheDarkMeasureQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark capstone line continues in Korriban Sith Crypt Depths. Defeat the Hunger of the Dark warden and secure the Hunger of the Dark Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hunger of the Dark Splintered Tomb Sigil from Korriban Sith Crypt Depths. Return to Neth Kyr for the next Hunger of the Dark lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void HungerOfTheDarkCircle()
        {
            _builder.Create(HungerOfTheDarkCircleQuestId, "Circle of Proof: Hunger of the Dark")
                .PrerequisiteQuest(HungerOfTheDarkBreachQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "The Hunger of the Dark capstone line continues in Korriban Sith Crypt Depths. Defeat the Hunger of the Dark inner circle and secure the Hunger of the Dark Keeper's Rite Token.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHungerOfTheDarkKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hunger of the Dark Keeper's Rite Token from Korriban Sith Crypt Depths. Return to Neth Kyr for the next Hunger of the Dark lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void HungerOfTheDarkMastery()
        {
            _builder.Create(HungerOfTheDarkMasteryQuestId, "Hunger of the Dark Mastery")
                .PrerequisiteQuest(HungerOfTheDarkCircleQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Hunger of the Dark master is waiting in Korriban Sith Crypt Depths. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Korriban_HungerOfTheDark_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Hunger of the Dark master is defeated. Return to Neth Kyr and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.HungerOfTheDark);
                });
        }

        private void EclipseOfResolveFoundation()
        {
            _builder.Create(EclipseOfResolveFoundationQuestId, "First Principle: Eclipse of Resolve")
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
                    "The Eclipse of Resolve capstone line continues in Korriban Sith Crypt Depths. Defeat Eclipse of Resolve adepts and secure the Eclipse of Resolve Crypt Trial Tablet.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveCryptTrialTablet)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Eclipse of Resolve Crypt Trial Tablet from Korriban Sith Crypt Depths. Return to Acolyte Varn for the next Eclipse of Resolve lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void EclipseOfResolveMeasure()
        {
            _builder.Create(EclipseOfResolveMeasureQuestId, "The Measure of Eclipse of Resolve")
                .PrerequisiteQuest(EclipseOfResolveFoundationQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve capstone line continues in Korriban Sith Crypt Depths. Defeat Eclipse of Resolve specialists and secure the Eclipse of Resolve Ritual Focus Shard.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveRitualFocusShard)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Eclipse of Resolve Ritual Focus Shard from Korriban Sith Crypt Depths. Return to Acolyte Varn for the next Eclipse of Resolve lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void EclipseOfResolveBreach()
        {
            _builder.Create(EclipseOfResolveBreachQuestId, "Fault Line: Eclipse of Resolve")
                .PrerequisiteQuest(EclipseOfResolveMeasureQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve capstone line continues in Korriban Sith Crypt Depths. Defeat the Eclipse of Resolve warden and secure the Eclipse of Resolve Splintered Tomb Sigil.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveSplinteredTombSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Eclipse of Resolve Splintered Tomb Sigil from Korriban Sith Crypt Depths. Return to Acolyte Varn for the next Eclipse of Resolve lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void EclipseOfResolveCircle()
        {
            _builder.Create(EclipseOfResolveCircleQuestId, "Circle of Proof: Eclipse of Resolve")
                .PrerequisiteQuest(EclipseOfResolveBreachQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    "The Eclipse of Resolve capstone line continues in Korriban Sith Crypt Depths. Defeat the Eclipse of Resolve inner circle and secure the Eclipse of Resolve Keeper's Rite Token.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEclipseOfResolveKeepersRiteToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Eclipse of Resolve Keeper's Rite Token from Korriban Sith Crypt Depths. Return to Acolyte Varn for the next Eclipse of Resolve lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void EclipseOfResolveMastery()
        {
            _builder.Create(EclipseOfResolveMasteryQuestId, "Eclipse of Resolve Mastery")
                .PrerequisiteQuest(EclipseOfResolveCircleQuestId)
                .PrerequisiteSkill(SkillType.Force, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Eclipse of Resolve master is waiting in Korriban Sith Crypt Depths. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Korriban_EclipseOfResolve_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Eclipse of Resolve master is defeated. Return to Acolyte Varn and claim the completed lesson.")
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
