using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Events;
using SWLOR.Shared.Domain.Inventory.ValueObjects;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Creature;

namespace SWLOR.Component.Inventory.Definitions.ItemDefinition
{
    public class SpeederItemDefinition: IItemListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICreaturePluginService _creaturePlugin;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public SpeederItemDefinition(IServiceProvider serviceProvider, ICreaturePluginService creaturePlugin)
        {
            _serviceProvider = serviceProvider;
            _creaturePlugin = creaturePlugin;
        }

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Speeder();
            return Builder.Build();
        }
        /// <summary>
        /// Check player's pheno: 
        /// Pheno = normal: change tail to speederbike, set pheno to speederbike and movement rate to DMfast.
        /// Pheno = SpeederBike: change tail to none, set pheno and speed to normal.
        /// </summary>
        private void Speeder()
        {
            Builder.Create("speeder")
                .Delay(2f)
                .PlaysAnimation(AnimationType.LoopingGetMid)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
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
                        SetCreatureTailType(CreatureTailType.None, user);
                        _creaturePlugin.SetMovementRate(user, MovementRateType.PC);
                        SendMessageToPC(user, "You dismount your speeder.");
                    }
                    else
                    {
                        SetCreatureTailType(CreatureTailType.SpeederBike, user);
                        SetPhenoType(PhenoType.SpeederBike, user);
                        _creaturePlugin.SetMovementRate(user, MovementRateType.DMFast);
                        SendMessageToPC(user, "You mount your speeder.");
                    }
                    
                });
        }
        /// <summary>
        /// On creature damaged if mounted, 25% chance for player to be dazed while getting knocked off the bike. 
        /// Play a matching animation that lasts the duration of the stun.
        /// Set pheno to normal, tailtype to none and movement rate back to normal after.
        /// </summary>
        [ScriptHandler<OnCreatureDamagedBefore>]
        public void AttackedDismount()
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
                    ActionPlayAnimation(AnimationType.LoopingDeadBack, 1, 2.0f);
                });

                FloatingTextStringOnCreature("You have been dismounted.", player, false);
                SetPhenoType(PhenoType.Normal, player);
                SetCreatureTailType(CreatureTailType.None, player);
                _creaturePlugin.SetMovementRate(player, MovementRateType.PC);
            }
        }
        /// <summary>
        /// When a creature acquires emnity and is mounted, dismount. 
        /// Set pheno to normal, tail to none and movement rate to normal.
        /// </summary>
        [ScriptHandler<OnEnmityAcquired>]
        public void AttackDismount()
        {
            var player = OBJECT_SELF;

            if (GetPhenoType(player) == PhenoType.SpeederBike)
            {
                SendMessageToPC(player, "You have been dismounted.");
                FloatingTextStringOnCreature("You have been dismounted.", player, false);
                SetPhenoType(PhenoType.Normal, player);
                SetCreatureTailType(CreatureTailType.None, player);
                _creaturePlugin.SetMovementRate(player, MovementRateType.PC);
            }
        }
        /// <summary>
        /// When a creature transitions into an interior and is mounted, dismount. 
        /// Set pheno to normal, tail to none and movement rate to normal.
        /// Warning: This is not currently working. Need to hook the right script. *****!
        /// </summary>
        [ScriptHandler<OnSpeederHook>]
        public void AreaTransitionDismount()
        {
            var player = OBJECT_SELF;
            var targetAreaTag = GetLocalString(player, "spdr_hook_t_tag");
            var targetArea = GetObjectByTag(targetAreaTag);

            if (GetPhenoType(player) == PhenoType.SpeederBike && GetIsAreaInterior(targetArea))
            {
                FloatingTextStringOnCreature("You have been dismounted for entering an area with a speeder.", player, false);
                SetPhenoType(PhenoType.Normal, player);
                SetCreatureTailType(CreatureTailType.None, player);
                _creaturePlugin.SetMovementRate(player, MovementRateType.PC);
            }
        }


    }
}
