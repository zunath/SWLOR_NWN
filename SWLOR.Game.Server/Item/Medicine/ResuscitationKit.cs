using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class ResuscitationKit : IActionItem
    {
        public string CustomKey => "Medicine.ResuscitationKit";

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin resuscitating " + target.Name + "...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = user.Object;
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int skillRank = SkillService.GetPCSkillRank(player, SkillType.Medicine);
            int perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.ResuscitationDevices);
            int rank = item.GetLocalInt("RANK");
            int baseHeal;

            switch (rank)
            {
                case 1:
                    baseHeal = 1;
                    break;
                case 2:
                    baseHeal = 11;
                    break;
                case 3:
                    baseHeal = 31;
                    break;
                case 4:
                    baseHeal = 51;
                    break;
                default: return;
            }

            baseHeal += perkLevel * 2;
            baseHeal += effectiveStats.Medicine / 2;
            baseHeal += item.MedicineBonus / 2;

            int delta = item.RecommendedLevel - skillRank;
            float effectivenessPercent = 1.0f;

            if (delta > 0)
            {
                effectivenessPercent = effectivenessPercent - (delta * 0.1f);
            }
            if (target.IsPlayer)
            {
                baseHeal = (int)(baseHeal * effectivenessPercent);
                Player dbPlayer = DataService.Player.GetByID(target.GlobalID);
                int fpRecover = (int)(dbPlayer.MaxFP * (0.01f * baseHeal));
                int hpRecover = (int)(target.MaxHP * (0.01f * baseHeal));

                _.PlaySound("use_bacta");
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectResurrection(), target);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(hpRecover), target);
                dbPlayer.CurrentFP = fpRecover;
                DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
                player.SendMessage("You successfully resuscitate " + target.Name + "!");

                int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(600, item.RecommendedLevel, skillRank);
                SkillService.GiveSkillXP(player, SkillType.Medicine, xp);
            }
            else
            {
                baseHeal = (int)(baseHeal * effectivenessPercent);
                int hpRecover = (int)(target.MaxHP * (0.01f * baseHeal));
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectResurrection(), target);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(hpRecover), target);
                player.SendMessage("You successfully resuscitate " + target.Name + "!");
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if (RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = SkillService.GetPCSkillRank((NWPlayer)user, SkillType.Medicine);
            return 12.0f - (rank * 0.1f);
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return ANIMATION_LOOPING_GET_LOW;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 3.5f + PerkService.GetCreaturePerkLevel(user.Object, PerkType.RangedHealing);
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int consumeChance = PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.FrugalMedic) * 10;
            return RandomService.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsCreature || target.IsDM)
            {
                return "Only creatures may be targeted with this item.";
            }

            if (target.CurrentHP > -11)
            {
                return "Your target is not dead.";
            }

            if (user.IsInCombat)
            {
                return "You are in combat.";
            }

            int perkLevel = PerkService.GetCreaturePerkLevel(user.Object, PerkType.ResuscitationDevices);
            int requiredLevel = item.GetLocalInt("RANK");

            if (perkLevel < requiredLevel)
            {
                return "You must have the Resuscitation Devices perk at level " + requiredLevel + " to use this item.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
