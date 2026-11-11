using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>Connects completed, paid techniques to independently accepted mastery trials.</summary>
    public static class UnarmedMastery
    {
        private const int MinimumOpponentLevel = 45;

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Register()
        {
            MeleeTechnique.TechniqueLanded -= OnTechniqueLanded;
            MeleeTechnique.TechniqueLanded += OnTechniqueLanded;
        }

        private static void OnTechniqueLanded(uint player, uint target, AbilityDetail ability, MeleeTechniqueStage stage)
        {
            if (!GetIsPC(player) || GetIsDM(player) ||
                !GetIsObjectValid(target) || GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target) || GetImmortal(target) ||
                GetIsObjectValid(GetMaster(target)) || !GetIsEnemy(target, player) ||
                Stat.GetNPCStats(target).Level < MinimumOpponentLevel)
                return;

            var objective = GetObjectiveKey(ability.MeleeTechnique, stage, ability.EffectiveLevelPerkType);
            if (objective != null)
                Quest.RecordCombatObjective(player, objective, target);
        }

        public static string GetObjectiveKey(MeleeTechniqueDetail detail, MeleeTechniqueStage stage, PerkType abilityPerk = PerkType.Invalid) => (detail.Line, stage) switch
        {
            (MeleeTechniqueLine.MartialArtist, MeleeTechniqueStage.Opening) when detail.CreatedStage == MeleeTechniqueStage.Finish => UnarmedMasteryQuestDefinition.OpeningSpentObjective,
            (MeleeTechniqueLine.MartialArtist, MeleeTechniqueStage.Finish) => UnarmedMasteryQuestDefinition.FinishSpentObjective,
            (MeleeTechniqueLine.Brawler, _) when abilityPerk == PerkType.Uppercut => UnarmedMasteryQuestDefinition.DistractionSpentObjective,
            _ => null
        };
    }
}
