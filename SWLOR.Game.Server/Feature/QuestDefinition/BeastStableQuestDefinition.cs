using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    /// <summary>
    /// Dorra Vesk's two-part trial at the Anchorhead stables on Tatooine. Each completed part adds one beast stable
    /// slot (see <see cref="Service.BeastMasteryService.BeastStableCapacity"/>).
    /// </summary>
    public class BeastStableQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        internal const string StableTrialOneQuestId = "stablemaster_trial_1";
        internal const string StableTrialTwoQuestId = "stablemaster_trial_2";

        /// <summary>DNA extracted from a beast of level 40 or higher.</summary>
        internal const string DnaLevel40Objective = "stablemaster_dna_40";

        /// <summary>DNA extracted from a beast of level 45 or higher.</summary>
        internal const string DnaLevel45Objective = "stablemaster_dna_45";

        /// <summary>Every quest whose completion adds one stable slot.</summary>
        internal static IReadOnlyList<string> StableSlotQuestIds { get; } = new[]
        {
            StableTrialOneQuestId,
            StableTrialTwoQuestId,
        };

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            StableTrialOne();
            StableTrialTwo();

            return _builder.Build();
        }

        private void StableTrialOne()
        {
            _builder.Create(StableTrialOneQuestId, "Room in the Pens")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Dorra Vesk, the stablemaster at the Anchorhead stables on Tatooine, will open one more stable slot for you once you prove your beast can work. Defeat eight Sand Demons in the North Hills, Northern Dunes, Flatlands or Southern Pass outside Anchorhead (your own beast must hit each one with a normal attack or damaging ability before it dies; any beast role or level counts, and your beast need not land the killing blow), and extract DNA from three beasts of level 40 or higher anywhere in the galaxy.")
                .AddBeastAssistedKillObjective(NPCGroupType.Tatooine_SandDemon, 8)
                .AddCombatObjective(DnaLevel40Objective, "DNA extracted from beasts of level 40 or higher", 3)

                .AddState()
                .SetStateJournalText(
                    "Your beast has proven itself against the Sand Demons and you have the DNA samples. Return to Dorra Vesk at the Anchorhead stables on Tatooine to claim one more stable slot.")
                .AddXPReward(20000)
                .AddGoldReward(10000)
                .AddItemReward("fnote_2039", 1);
        }

        private void StableTrialTwo()
        {
            _builder.Create(StableTrialTwoQuestId, "The Long Haul")
                .PrerequisiteQuest(StableTrialOneQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Dorra Vesk at the Anchorhead stables on Tatooine will open a final stable slot for you if your beast can stand against the deadliest game she knows. Defeat six Ssurians in the Grottos, Grotto Caverns or Mountain Caves of Dathomir, extract DNA from three beasts of level 45 or higher, and, with allies you trust, bring down the Ancient Sand Worm in the Worm Den beneath the Tusken Raider Cave on Tatooine. Your own beast must hit each Ssurian and the Ancient Sand Worm with a normal attack or damaging ability before it dies; any beast role or level counts, and your beast need not land the killing blow.")
                .AddBeastAssistedKillObjective(NPCGroupType.Dathomir_Ssurian, 6)
                .AddBeastAssistedKillObjective(NPCGroupType.Tatooine_AncientSandworm, 1)
                .AddCombatObjective(DnaLevel45Objective, "DNA extracted from beasts of level 45 or higher", 3)

                .AddState()
                .SetStateJournalText(
                    "The Ancient Sand Worm is dead, the Ssurians are down and you have the DNA samples. Return to Dorra Vesk at the Anchorhead stables on Tatooine to claim your final stable slot.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .AddItemReward("fnote_2091", 1)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.Stablemaster);
                });
        }
    }
}
