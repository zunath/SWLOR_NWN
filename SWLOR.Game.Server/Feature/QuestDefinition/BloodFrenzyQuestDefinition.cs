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
    public class BloodFrenzyQuestDefinition : IQuestListDefinition
    {
        public const string FinalQuestId = "blood_frenzy_mastery";

        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            ABladeThatDoesNotWaste();
            TheThirteenBeatCut();
            GlassInTheBlood();
            TheHandOnTheHilt();
            TheBloodFrenzyKing();

            return _builder.Build();
        }

        private void ABladeThatDoesNotWaste()
        {
            _builder.Create("blood_frenzy_blade", "A Blade That Does Not Waste")
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.ViscaraSewersDepthsKey);
                })
                .OnAbandonAction(player =>
                {
                    var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
                    if (!dbPlayer.Quests.TryGetValue("blood_frenzy_blade", out var quest) ||
                        quest.TimesCompleted <= 0)
                    {
                        KeyItem.RemoveKeyItem(player, KeyItemType.ViscaraSewersDepthsKey);
                    }
                })
                .RemoveKeyItemOnAbandon(KeyItemType.BloodFrenzyRedVeinCodex)
                .RemoveKeyItemOnComplete(KeyItemType.BloodFrenzyRedVeinCodex)

                .AddState()
                .SetStateJournalText("Sera Vonn has opened the first Blood Frenzy lesson in the sealed Viscara Sewers Depths. Defeat the scavengers studying her Red Vein Codex and recover it from their camp.")
                .AddKillObjective(NPCGroupType.Viscara_RedVeinScavenger, 6)
                .GrantKeyItemOnAdvance(KeyItemType.BloodFrenzyRedVeinCodex)

                .AddState()
                .SetStateJournalText("You recovered the Red Vein Codex. Return it to Sera Vonn in Veles Colony.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void TheThirteenBeatCut()
        {
            _builder.Create("blood_frenzy_beat", "The Thirteen-Beat Cut")
                .PrerequisiteQuest("blood_frenzy_blade")
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.BloodFrenzyPulseMetronome)
                .RemoveKeyItemOnComplete(KeyItemType.BloodFrenzyPulseMetronome)

                .AddState()
                .SetStateJournalText("Sera has marked the next lesson with a pulse metronome. Destroy the Pulse-Frame Training Droids in the Viscara Sewers Depths and recover their timing core.")
                .AddKillObjective(NPCGroupType.Viscara_PulseFrameTrainingDroid, 5)
                .GrantKeyItemOnAdvance(KeyItemType.BloodFrenzyPulseMetronome)

                .AddState()
                .SetStateJournalText("You recovered the pulse metronome from the training droids. Return it to Sera Vonn.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void GlassInTheBlood()
        {
            _builder.Create("blood_frenzy_glass", "Glass in the Blood")
                .PrerequisiteQuest("blood_frenzy_beat")
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.BloodFrenzyAdrenalGlass)
                .RemoveKeyItemOnComplete(KeyItemType.BloodFrenzyAdrenalGlass)

                .AddState()
                .SetStateJournalText("Sera has traced a corrupted Blood Frenzy practice to a black-market stim lab in the Viscara Sewers Depths. Defeat the Blood Frenzy Butcher and secure a shard of clotted adrenal glass from the lab.")
                .AddKillObjective(NPCGroupType.Viscara_BloodFrenzyButcher, 1)
                .GrantKeyItemOnAdvance(KeyItemType.BloodFrenzyAdrenalGlass)

                .AddState()
                .SetStateJournalText("The Blood Frenzy Butcher is dead and the adrenal glass is yours. Return to Sera Vonn.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void TheHandOnTheHilt()
        {
            _builder.Create("blood_frenzy_restraint", "The Hand on the Hilt")
                .PrerequisiteQuest("blood_frenzy_glass")
                .PrerequisiteSkill(SkillType.Vibroblade, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.BloodFrenzyCharmFragments)
                .RemoveKeyItemOnComplete(KeyItemType.BloodFrenzyCharmFragments)

                .AddState()
                .SetStateJournalText("Sera has prepared a restraint charm from the adrenal glass. Defeat the Blood Frenzy Duelists carrying matching charm fragments in the Viscara Sewers Depths before the corrupted school can rebuild.")
                .AddKillObjective(NPCGroupType.Viscara_BloodFrenzyDuelist, 4)
                .GrantKeyItemOnAdvance(KeyItemType.BloodFrenzyCharmFragments)

                .AddState()
                .SetStateJournalText("You recovered the Blood Frenzy charm fragments from the duelists. Return them to Sera Vonn.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void TheBloodFrenzyKing()
        {
            _builder.Create(FinalQuestId, "The Blood Frenzy King")
                .PrerequisiteQuest("blood_frenzy_restraint")
                .PrerequisiteSkill(SkillType.Vibroblade, 50)

                .AddState()
                .SetStateJournalText("Kess Draavo, the Blood Frenzy King, is drawing duelists into a killing rhythm in the Viscara Sewers Depths. Find him there, break his circle, and end the corrupted Blood Frenzy school.")
                .AddKillObjective(NPCGroupType.Viscara_BloodFrenzyKing, 1)

                .AddState()
                .SetStateJournalText("Kess Draavo is dead. Return to Sera Vonn and claim the final lesson of Blood Frenzy.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.BloodFrenzy);
                });
        }
    }
}
