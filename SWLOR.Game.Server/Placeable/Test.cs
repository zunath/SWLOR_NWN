using NWN;
using SWLOR.Game.Server.ChatCommand;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Placeable
{
    public class Test: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable bread = Object.OBJECT_SELF;
            NWCreature caster = _.GetObjectByTag("caster_test");
            NWCreature target = _.GetObjectByTag("target_test");

            int perkID = bread.GetLocalInt("PERK_ID");
            int tier = bread.GetLocalInt("TIER");
            if (tier < 1) tier = 1;

            caster.SetLocalInt("PERK_LEVEL_" + perkID, tier);
            AbilityService.SetMaxFP(caster, 9999);
            AbilityService.SetCurrentFP(caster, 9999);

            PerkFeat perkFeat = DataService.Single<PerkFeat>(x => x.PerkID == perkID && x.PerkLevelUnlocked == 1);
            NWNXCreature.AddFeat(caster, perkFeat.FeatID);

            caster.AssignCommand(() =>
            {
                _.SpeakString("Casting perk ID = " + perkID + ", feat ID = " + perkFeat.FeatID);
                _.ActionUseFeat(perkFeat.FeatID, target);
            });

            return true;
        }
    }
}
