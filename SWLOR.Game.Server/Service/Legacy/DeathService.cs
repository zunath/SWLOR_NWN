using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            NWPlayer player = NWScript.GetLastPlayerDied();
            NWObject hostile = NWScript.GetLastHostileActor(player.Object);

            NWScript.SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            NWScript.SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            NWScript.SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = NWScript.GetFirstFactionMember(hostile.Object, false);
            while (NWScript.GetIsObjectValid(factionMember) == true)
            {
                NWScript.ClearPersonalReputation(player.Object, factionMember);
                factionMember = NWScript.GetNextFactionMember(hostile.Object, false);
            }
            
            const string RespawnMessage = "You have died. You can wait for another player to revive you or respawn to go to your last respawn point.";
            NWScript.PopUpDeathGUIPanel(player.Object, true, true, 0, RespawnMessage);
        }

        private static void ApplyDurabilityLoss(NWPlayer player)
        {
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                NWItem equipped = NWScript.GetItemInSlot((InventorySlot)index, player);
                DurabilityService.RunItemDecay(player, equipped, RandomService.RandomFloat(0.15f, 0.50f));
            }

            foreach (var item in player.InventoryItems)
            {
                DurabilityService.RunItemDecay(player, item, RandomService.RandomFloat(0.10f, 0.25f));
            }
        }

        private static void OnModuleRespawn()
        {
            NWPlayer oPC = NWScript.GetLastRespawnButtonPresser();
            ApplyDurabilityLoss(oPC);

            var amount = oPC.MaxHP / 2;
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectResurrection(), oPC.Object);
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(amount), oPC.Object);

            var area = oPC.Area;
            
            TeleportPlayerToBindPoint(oPC);

            // If player is the last person in an instance, destroy the instance.
            if (area.IsInstance)
            {
                var playersInArea = NWModule.Get().Players.Count(x => x.Area == oPC.Area && x != oPC);

                if (playersInArea <= 0)
                {
                    NWScript.DelayCommand(12.0f, () =>
                    {
                        AreaService.DestroyAreaInstance(area);
                    }); 
                }
            }
        }
        

        public static void SetRespawnLocation(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player), nameof(player) + " cannot be null.");

            var pc = DataService.Player.GetByID(player.GlobalID);
            pc.RespawnLocationX = player.Position.X;
            pc.RespawnLocationY = player.Position.Y;
            pc.RespawnLocationZ = player.Position.Z;
            pc.RespawnLocationOrientation = player.Facing;
            pc.RespawnAreaResref = player.Area.Resref;
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            NWScript.FloatingTextStringOnCreature("You will return to this location the next time you die.", player.Object, false);
        }


        public static void TeleportPlayerToBindPoint(NWPlayer pc)
        {
            var entity = DataService.Player.GetByID(pc.GlobalID);
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
                    NWScript.ActionJumpToLocation(entrance);
                });
            }
            // Send player to default respawn point if no bind point is set.
            else if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
            {
                NWObject defaultRespawn = NWScript.GetWaypointByTag("DEFAULT_RESPAWN_POINT");
                var location = defaultRespawn.Location;

                pc.AssignCommand(() =>
                {
                    NWScript.ActionJumpToLocation(location);
                });
            }
            // Send player to their stored bind point.
            else
            {
                var area = NWModule.Get().Areas.Single(x => x.Resref == entity.RespawnAreaResref);
                var position = NWScript.Vector3((float)entity.RespawnLocationX, (float)entity.RespawnLocationY, (float)entity.RespawnLocationZ);
                var location = NWScript.Location(area.Object, position, (float)entity.RespawnLocationOrientation);
                pc.AssignCommand(() =>
                {
                    NWScript.ActionJumpToLocation(location);
                });
            }
        }
    }
}
