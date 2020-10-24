using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Enum;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class DeathService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleDeath>(message => OnModuleDeath());
            MessageHub.Instance.Subscribe<OnModuleRespawn>(message => OnModuleRespawn());
        }

        private static void OnModuleDeath()
        {
            NWPlayer player = _.GetLastPlayerDied();
            NWObject hostile = _.GetLastHostileActor(player.Object);

            _.SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            _.SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            _.SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = _.GetFirstFactionMember(hostile.Object, false);
            while (_.GetIsObjectValid(factionMember) == true)
            {
                _.ClearPersonalReputation(player.Object, factionMember);
                factionMember = _.GetNextFactionMember(hostile.Object, false);
            }
            
            const string RespawnMessage = "You have died. You can wait for another player to revive you or respawn to go to your last respawn point.";
            _.PopUpDeathGUIPanel(player.Object, true, true, 0, RespawnMessage);
        }

        private static void ApplyDurabilityLoss(NWPlayer player)
        {
            for (int index = 0; index < NumberOfInventorySlots; index++)
            {
                NWItem equipped = _.GetItemInSlot((InventorySlot)index, player);
                DurabilityService.RunItemDecay(player, equipped, RandomService.RandomFloat(0.10f, 0.50f));
            }

        }

        private static void OnModuleRespawn()
        {
            NWPlayer oPC = _.GetLastRespawnButtonPresser();
            ApplyDurabilityLoss(oPC);

            int amount = oPC.MaxHP / 2;
            _.ApplyEffectToObject(DurationType.Instant, _.EffectResurrection(), oPC.Object);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(amount), oPC.Object);

            NWArea area = oPC.Area;
            
            TeleportPlayerToBindPoint(oPC);

            // If player is the last person in an instance, destroy the instance.
            if (area.IsInstance)
            {
                int playersInArea = NWModule.Get().Players.Count(x => x.Area == oPC.Area && x != oPC);

                if (playersInArea <= 0)
                {
                    _.DelayCommand(12.0f, () =>
                    {
                        AreaService.DestroyAreaInstance(area);
                    }); 
                }
            }
        }
        

        public static void SetRespawnLocation(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player), nameof(player) + " cannot be null.");

            Player pc = DataService.Player.GetByID(player.GlobalID);
            pc.RespawnLocationX = player.Position.X;
            pc.RespawnLocationY = player.Position.Y;
            pc.RespawnLocationZ = player.Position.Z;
            pc.RespawnLocationOrientation = player.Facing;
            pc.RespawnAreaResref = player.Area.Resref;
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            _.FloatingTextStringOnCreature("You will return to this location the next time you die.", player.Object, false);
        }


        public static void TeleportPlayerToBindPoint(NWPlayer pc)
        {
            Player entity = DataService.Player.GetByID(pc.GlobalID);
            TeleportPlayerToBindPoint(pc, entity);
        }

        private static void TeleportPlayerToBindPoint(NWObject pc, Player entity)
        {
            // Instances
            if (pc.Area.IsInstance)
            {
                var area = pc.Area;
                NWLocation entrance = area.GetLocalLocation("INSTANCE_ENTRANCE");
                pc.AssignCommand(() =>
                {
                    _.ActionJumpToLocation(entrance);
                });
            }
            // Send player to default respawn point if no bind point is set.
            else if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
            {
                NWObject defaultRespawn = _.GetWaypointByTag("DEFAULT_RESPAWN_POINT");
                Location location = defaultRespawn.Location;

                pc.AssignCommand(() =>
                {
                    _.ActionJumpToLocation(location);
                });
            }
            // Send player to their stored bind point.
            else
            {
                NWArea area = NWModule.Get().Areas.Single(x => x.Resref == entity.RespawnAreaResref);
                Vector position = _.Vector((float)entity.RespawnLocationX, (float)entity.RespawnLocationY, (float)entity.RespawnLocationZ);
                Location location = _.Location(area.Object, position, (float)entity.RespawnLocationOrientation);
                pc.AssignCommand(() =>
                {
                    _.ActionJumpToLocation(location);
                });
            }
        }
    }
}
