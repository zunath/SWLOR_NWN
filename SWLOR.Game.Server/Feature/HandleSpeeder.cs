using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;


namespace SWLOR.Game.Server.Feature
{
    public class HandleSpeeder
    {
        /// <summary>
        /// Before an item is used, check if the area is an interior and return if it is. 
        /// Check player's pheno: 
        /// Pheno = normal: change tail to speederbike, set pheno to speederbike and movement rate to DMfast.
        /// Pheno = SpeederBike: change tail to none, set pheno and speed to normal.
        /// </summary>
        [NWNEventHandler("item_use_bef")]
        public static void SpeederMount()
        {
            var player = OBJECT_SELF;
            var area = GetArea(player);
            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            string itemTag = GetTag(item);

           if (GetIsAreaInterior(area))
           {
               return;
           }
           EventsPlugin.SkipEvent();

            if (itemTag == "sped_model1")
            {
                if (GetPhenoType(player) == PhenoType.SpeederBike)
                {
                    SetPhenoType(PhenoType.Normal, player);
                    SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None,player);
                    CreaturePlugin.SetMovementRate(player, MovementRate.Normal);
                    SendMessageToPC(player,"You dismount your speeder.");
                }
                else
                {
                    SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.SpeederBike,player);
                    SetPhenoType(PhenoType.SpeederBike, player);
                    CreaturePlugin.SetMovementRate(player, MovementRate.DMFast);
                    SendMessageToPC(player, "You mount your speeder.");
                }
            }
          
        }
        /// <summary>
        /// On creature damaged if mounted, 25% chance for player to be dazed while getting knocked off the bike. 
        /// Play a matching animation that lasts the duration of the stun.
        /// Set pheno to normal, tailtype to none and movement rate back to normal after.
        /// </summary>
        [NWNEventHandler("crea_damaged")]
        public static void AttackedDismount()
        {
            var player = OBJECT_SELF;;

                if (GetPhenoType(player) == PhenoType.SpeederBike)
                {
                    Effect stun = EffectStunned();
                    int dazeChance = Random(100);
                    if (dazeChance < 25)
                    {
                        ApplyEffectToObject(DurationType.Temporary, stun, player, 2.0f);
                    }
                    AssignCommand(player, () =>
                    {
                        ActionPlayAnimation(Animation.LoopingDeadBack, 1, 2.0f);
                    });

                    FloatingTextStringOnCreature("You have been dismounted.", player, false);
                    SetPhenoType(PhenoType.Normal, player);
                    SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None,player);
                    CreaturePlugin.SetMovementRate(player, MovementRate.Normal);
                }
            }
        /// <summary>
        /// When a creature acquires emnity and is mounted, dismount. 
        /// Set pheno to normal, tail to none and movement rate to normal.
        /// </summary>
        [NWNEventHandler("enmity_acquired")]
        public static void AttackDismount()
        {
            var player = OBJECT_SELF;

            if (GetPhenoType(player) == PhenoType.SpeederBike)
            {
                SendMessageToPC(player, "You have been dismounted.");
                FloatingTextStringOnCreature("You have been dismounted.", player, false);
                SetPhenoType(PhenoType.Normal, player);
                SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None, player);
                CreaturePlugin.SetMovementRate(player, MovementRate.Normal);
            }
        }
        /// <summary>
        /// When a creature transitions into an interior and is mounted, dismount. 
        /// Set pheno to normal, tail to none and movement rate to normal.
        /// Warning: This is not currently working. Need to hook the right script. *****!
        /// </summary>
        [NWNEventHandler("speeder_hook")]
        public static void AreaTransitionDismount()
        {
            var player = OBJECT_SELF;
            var area = GetArea(player);
            string targetAreaTag = GetLocalString(player, "spdr_hook_t_tag");
            var targetArea = GetObjectByTag(targetAreaTag);

            if (GetPhenoType(player) == PhenoType.SpeederBike && GetIsAreaInterior(targetArea))
            {
                FloatingTextStringOnCreature("You have been dismounted for entering an area with a speeder.", player, false);
                ExecuteScript("mounted_enter", player);
                SetPhenoType(PhenoType.Normal, player);
                SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None, player);
                CreaturePlugin.SetMovementRate(player, MovementRate.Normal);
            }
        }

    }
}
