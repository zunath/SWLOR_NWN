using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Player Guide's topic list: one method per topic from the category types, in the order the guide shows them, then the
    /// status effect and combat style topics. Static data with no service.
    /// </summary>
    internal static class PlayerGuideTopicCatalog
    {
        internal static readonly IReadOnlyList<PlayerGuideTopic> Topics = BuildTopics();

        private static IReadOnlyList<PlayerGuideTopic> BuildTopics()
        {
            var topics = new List<PlayerGuideTopic>
            {
                PlayerGuideCoreTopics.CreateCommonQuestions(),
                PlayerGuideSocialTopics.CreateCommunication(),
                PlayerGuideCoreTopics.CreateSkills(),
                PlayerGuideCoreTopics.CreateSkillDecay(),
                PlayerGuideProgressionTopics.CreatePerks(),
                PlayerGuideCombatTopics.CreateForceAffinity(),
                PlayerGuideProgressionTopics.CreatePerkRefunds(),
                PlayerGuideCoreTopics.CreateXpDebt(),
                PlayerGuideCombatTopics.CreateAbilities(),
                PlayerGuideCombatTopics.CreateMimicryAndTechniques(),
                PlayerGuideCharacterTopics.CreateAttributes(),
                PlayerGuideCharacterTopics.CreateRebuilds(),
                PlayerGuideCoreTopics.CreateDeathAndRecovery(),
                PlayerGuideCombatTopics.CreatePvpCombat(),
                PlayerGuideCombatTopics.CreateCombatBasics(),
                PlayerGuideCombatTopics.CreateStatCaps(),
                PlayerGuideActivitiesTopics.CreateEspionage(),
                PlayerGuideSocialTopics.CreateDisguises(),
                PlayerGuideActivitiesTopics.CreateCrafting(),
                PlayerGuideActivitiesTopics.CreateGatheringAndFishing(),
                PlayerGuideProgressionTopics.CreateTrainingStore(),
                PlayerGuideActivitiesTopics.CreateBeastsAndStables(),
                PlayerGuideActivitiesTopics.CreateDroids(),
                PlayerGuideActivitiesTopics.CreateQuestsAndKeyItems(),
                PlayerGuideActivitiesTopics.CreateQuestContracts(),
                PlayerGuideActivitiesTopics.CreateGuildsAndCitizenship(),
                PlayerGuideActivitiesTopics.CreateHousingAndMarkets(),
                PlayerGuideActivitiesTopics.CreateTravelAndNavigation(),
                PlayerGuideActivitiesTopics.CreateShipsAndSpace(),
                PlayerGuideInterfaceTopics.CreateUsefulWindows(),
                StatusEffectGuideTopics.Create()
            };
            topics.AddRange(CombatStyleGuideTopics.Create());
            return topics;
        }
    }
}
