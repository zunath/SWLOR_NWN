﻿using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class ForcePack: IActionItem
    {
        public string CustomKey => "Medicine.ForcePack";

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin applying a force pack to " + target.Name + "...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int rank = SkillService.GetPCSkillRank(player, Skill.Medicine);
            int luck = PerkService.GetCreaturePerkLevel(player, PerkType.Lucky);
            int perkDurationBonus = PerkService.GetCreaturePerkLevel(player, PerkType.HealingKitExpert) * 6 + (luck * 2);
            float duration = 30.0f + (rank * 0.4f) + perkDurationBonus;
            int restoreAmount = 1 + item.GetLocalInt("HEALING_BONUS") + effectiveStats.Medicine + item.MedicineBonus;
            int delta = item.RecommendedLevel - rank;
            float effectivenessPercent = 1.0f;

            if (delta > 0)
            {
                effectivenessPercent = effectivenessPercent - (delta * 0.1f);
            }

            restoreAmount = (int)(restoreAmount * effectivenessPercent);

            int perkBlastBonus = PerkService.GetCreaturePerkLevel(player, PerkType.ImmediateForcePack);
            if (perkBlastBonus > 0)
            {
                int blastHeal = restoreAmount * perkBlastBonus;
                if (RandomService.Random(100) + 1 <= luck / 2)
                {
                    blastHeal *= 2;
                }

                AbilityService.RestorePlayerFP(target.Object, blastHeal);
            }

            float interval = 6.0f;
            ClassType background = (ClassType) player.Class1;

            if (background == ClassType.Medic)
                interval *= 0.5f;

            string data = (int)interval + ", " + restoreAmount;
            CustomEffectService.ApplyCustomEffect(user, target.Object, CustomEffectType.ForcePack, (int)duration, restoreAmount, data);

            player.SendMessage("You successfully apply a force pack to " + target.Name + ". The force pack will expire in " + duration + " seconds.");

            _.DelayCommand(duration + 0.5f, () => { player.SendMessage("The force pack that you applied to " + target.Name + " has expired."); });

            int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
            SkillService.GiveSkillXP(player, Skill.Medicine, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if ( RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = SkillService.GetPCSkillRank(user.Object, Skill.Medicine);
            return 12.0f - (rank * 0.1f);
        }

        public bool FaceTarget()
        {
            return true;
        }


        public Animation AnimationType()
        {
            return Animation.Get_Mid;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 3.5f + PerkService.GetCreaturePerkLevel(user.Object, PerkType.RangedHealing);
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int consumeChance = PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.FrugalMedic) * 10;
            ClassType background = (ClassType) user.Class1;

            if (background == ClassType.Medic)
            {
                consumeChance += 5;
            }


            return RandomService.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsPlayer)
            {
                return "Only players may be targeted with this item.";
            }

            var dbTarget = DataService.Player.GetByID(target.GlobalID);
            if (dbTarget.CurrentFP >= dbTarget.MaxFP)
            {
                return "Your target's FP is at their maximum.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
