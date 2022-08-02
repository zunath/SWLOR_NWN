using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class SpeederItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new ItemBuilder();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Speeder();
            return _builder.Build();
        }
        /// <summary>
        /// Check player's pheno: 
        /// Pheno = normal: change tail to speederbike, set pheno to speederbike and movement rate to DMfast.
        /// Pheno = SpeederBike: change tail to none, set pheno and speed to normal.
        /// </summary>
        private void Speeder()
        {
            _builder.Create("speeder")
                .Delay(2f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ApplyAction((user, item, target, location) =>
                {
                    var area = GetArea(user);
                    var itemTag = GetTag(item);

                    if (GetIsAreaInterior(area))
                    {
                        return;
                    }

                    if (GetPhenoType(user) == PhenoType.SpeederBike)
                    {
                        SetPhenoType(PhenoType.Normal, user);
                        SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None, user);
                        CreaturePlugin.SetMovementRate(user, MovementRate.PC);
                        SendMessageToPC(user, "You dismount your speeder.");
                    }
                    else
                    {
                        SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.SpeederBike, user);
                        SetPhenoType(PhenoType.SpeederBike, user);
                        CreaturePlugin.SetMovementRate(user, MovementRate.DMFast);
                        SendMessageToPC(user, "You mount your speeder.");
                    }
                    
                });
        }
        /// <summary>
        /// On creature damaged if mounted, 25% chance for player to be dazed while getting knocked off the bike. 
        /// Play a matching animation that lasts the duration of the stun.
        /// Set pheno to normal, tailtype to none and movement rate back to normal after.
        /// </summary>
        [NWNEventHandler("crea_damaged_bef")]
        public static void AttackedDismount()
        {
            var player = OBJECT_SELF; ;

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
                SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None, player);
                CreaturePlugin.SetMovementRate(player, MovementRate.PC);
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
                CreaturePlugin.SetMovementRate(player, MovementRate.PC);
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
            var targetAreaTag = GetLocalString(player, "spdr_hook_t_tag");
            var targetArea = GetObjectByTag(targetAreaTag);

            if (GetPhenoType(player) == PhenoType.SpeederBike && GetIsAreaInterior(targetArea))
            {
                FloatingTextStringOnCreature("You have been dismounted for entering an area with a speeder.", player, false);
                SetPhenoType(PhenoType.Normal, player);
                SetCreatureTailType(Core.NWScript.Enum.Creature.TailType.None, player);
                CreaturePlugin.SetMovementRate(player, MovementRate.PC);
            }
        }


    }
}
