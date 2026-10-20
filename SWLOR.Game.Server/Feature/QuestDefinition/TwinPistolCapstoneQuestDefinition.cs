using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public sealed class TwinPistolCapstoneQuestDefinition : IQuestListDefinition
    {
        internal const string TwinSunsMasteryQuestId = "twin_suns_mastery";
        internal const string FinalSalvoMasteryQuestId = "final_salvo_mastery";
        internal const string TrialAreaResref = "pw_sc_canyonrng";
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            TwinSunsMastery();
            FinalSalvoMastery();
            var quests = _builder.Build();
            quests[TwinSunsMasteryQuestId].States[1].AddObjective(new TwinPistolTrialObjective(
                KeyItemType.TwinSunsCanyonFiringTrace, "Land twelve ordinary shots on one canyon-range enemy within 30 seconds, at least six with each gun."));
            quests[FinalSalvoMasteryQuestId].States[1].AddObjective(new TwinPistolTrialObjective(
                KeyItemType.FinalSalvoCanyonRouteTrace, "Land a moving Blaster Slide and Pistol Rush, then a Grapple Shot spending at least two Flow charges within 18 seconds."));
            return quests;
        }

        private void TwinSunsMastery()
        {
            _builder.Create(TwinSunsMasteryQuestId, "Two Suns, One Horizon")
                .PrerequisiteSkill(SkillType.TwinPistol, 50)
                .OnAcceptAction((player, _) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.TwinSunsCanyonFiringTrace);
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneAnchorheadCanyonRangeKey);
                    TwinPistolMastery.ClearTrialState(player);
                })
                .OnAbandonAction(player =>
                {
                    TwinPistolMastery.ClearTrialState(player);
                    PistolCapstoneQuestDefinition.RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.TwinSunsCanyonFiringTrace)
                .RemoveKeyItemOnComplete(KeyItemType.TwinSunsCanyonFiringTrace)
                .AddState()
                .SetStateJournalText("Jek Talin gave you access to the Anchorhead Canyon Range on Tatooine. Enter through the sealed door in the Rocky Pass, south of Anchorhead's Southern Entrance. Against one hostile range enemy, land twelve Twin Pistol weapon auto-attacks within 30 seconds, including at least six shots from each gun. Switching enemies or equipment resets the attempt. Activated abilities do not count.")
                .AddState()
                .SetStateJournalText("Your paired firing trace is complete. Report to Jek Talin by the west trail at Anchorhead - Southern Entrance. His approval lets you purchase Twin Suns for 6 SP; it does not grant the perk automatically.")
                .OnCompleteAction((player, _) => Achievement.GiveAchievement(player, AchievementType.TwinSuns));
        }

        private void FinalSalvoMastery()
        {
            _builder.Create(FinalSalvoMasteryQuestId, "The Last Open Lane")
                .PrerequisiteSkill(SkillType.TwinPistol, 50)
                .OnAcceptAction((player, _) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.FinalSalvoCanyonRouteTrace);
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneAnchorheadCanyonRangeKey);
                    TwinPistolMastery.ClearTrialState(player);
                })
                .OnAbandonAction(player =>
                {
                    TwinPistolMastery.ClearTrialState(player);
                    PistolCapstoneQuestDefinition.RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.FinalSalvoCanyonRouteTrace)
                .RemoveKeyItemOnComplete(KeyItemType.FinalSalvoCanyonRouteTrace)
                .AddState()
                .SetStateJournalText("Pavo Orrel gave you access to the Anchorhead Canyon Range on Tatooine. Enter through the sealed door in the Rocky Pass, south of Anchorhead's Southern Entrance. Land both a Blaster Slide and a Pistol Rush after actually moving at least 2m with each, then land Grapple Shot while spending at least two Flow charges. Complete the sequence within 18 seconds. Empty dashes, failed movement and Second Wind charges alone do not satisfy the trial.")
                .AddState()
                .SetStateJournalText("Your canyon route trace records both repositioning attacks and the successful Flow spender. Report to Pavo Orrel in Anchorhead - Junix's Joint. His approval lets you purchase Final Salvo for 6 SP; it does not grant the perk automatically.")
                .OnCompleteAction((player, _) => Achievement.GiveAchievement(player, AchievementType.FinalSalvo));
        }
    }
}
