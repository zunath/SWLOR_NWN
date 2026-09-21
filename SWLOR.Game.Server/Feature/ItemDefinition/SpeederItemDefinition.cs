using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.Engine;

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
        /// Not riding: change tail to speederbike, set the riding pheno for the body type and movement rate to DMfast.
        /// Riding: change tail to none, restore the body type's pheno and normal speed.
        /// </summary>
        private void Speeder()
        {
            _builder.Create("speeder")
                .Delay(2f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var area = GetArea(user);
                    var itemTag = GetTag(item);

                    if (GetIsAreaInterior(area))
                    {
                        return;
                    }

                    if (SpeederPhenotype.IsRiding(GetPhenoType(user)))
                    {
                        Dismount(user);
                        SendMessageToPC(user, "You dismount your speeder.");
                    }
                    else
                    {
                        // An RGB robe swaps in a generated phenotype; ride with the body type underneath it.
                        var body = (PhenoType)RobeModelRenderer.GetBasePhenotype(user);
                        if (!SpeederPhenotype.TryGetRiding(body, out var riding))
                        {
                            SendMessageToPC(user, "You cannot ride a speeder in your current form.");
                            return;
                        }

                        SetCreatureTailType(TailType.SpeederBike, user);
                        SetPhenoType(riding, user);
                        CreaturePlugin.SetMovementRate(user, MovementRate.DMFast);
                        SendMessageToPC(user, "You mount your speeder.");
                    }

                    TintMapService.QueueRefreshAndEditor(user, user);
                });
        }
        /// <summary>
        /// Returns a rider to the body type they mounted with and removes the speeder.
        /// </summary>
        private static void Dismount(uint rider)
        {
            SetPhenoType(SpeederPhenotype.GetDismounted(GetPhenoType(rider)), rider);
            SetCreatureTailType(TailType.None, rider);
            Stat.ApplyCreatureMovementRate(rider);
        }

        /// <summary>
        /// On creature damaged if mounted, 25% chance for player to be dazed while getting knocked off the bike.
        /// Play a matching animation that lasts the duration of the stun.
        /// Set pheno to normal, tailtype to none and movement rate back to normal after.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]
        public static void AttackedDismount()
        {
            var player = OBJECT_SELF; ;

            if (SpeederPhenotype.IsRiding(GetPhenoType(player)))
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
                Dismount(player);
                TintMapService.QueueRefreshAndEditor(player, player);
            }
        }
        /// <summary>
        /// When a creature acquires emnity and is mounted, dismount.
        /// Set pheno to normal, tail to none and movement rate to normal.
        /// </summary>
        [NWNEventHandler(ScriptName.OnEnmityAcquired)]
        public static void AttackDismount()
        {
            var player = OBJECT_SELF;

            if (SpeederPhenotype.IsRiding(GetPhenoType(player)))
            {
                SendMessageToPC(player, "You have been dismounted.");
                PlayerFeedback.ShowDiagnosticFloatingText("You have been dismounted.", player, false);
                Dismount(player);
                TintMapService.QueueRefreshAndEditor(player, player);
            }
        }
        /// <summary>
        /// When a creature transitions into an interior and is mounted, dismount.
        /// Set pheno to normal, tail to none and movement rate to normal.
        /// Warning: This is not currently working. Need to hook the right script. *****!
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpeederHook)]
        public static void AreaTransitionDismount()
        {
            var player = OBJECT_SELF;
            var targetAreaTag = GetLocalString(player, "spdr_hook_t_tag");
            var targetArea = GetObjectByTag(targetAreaTag);

            if (SpeederPhenotype.IsRiding(GetPhenoType(player)) && GetIsAreaInterior(targetArea))
            {
                FloatingTextStringOnCreature("You have been dismounted for entering an area with a speeder.", player, false);
                Dismount(player);
                TintMapService.QueueRefreshAndEditor(player, player);
            }
        }


    }
}
