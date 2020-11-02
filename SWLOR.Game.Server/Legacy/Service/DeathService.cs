using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Service
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
            NWPlayer player = GetLastPlayerDied();
            NWObject hostile = GetLastHostileActor(player.Object);

            SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = GetFirstFactionMember(hostile.Object, false);
            while (GetIsObjectValid(factionMember) == true)
            {
                ClearPersonalReputation(player.Object, factionMember);
                factionMember = GetNextFactionMember(hostile.Object, false);
            }
            
            const string RespawnMessage = "You have died. You can wait for another player to revive you or respawn to go to your last respawn point.";
            PopUpDeathGUIPanel(player.Object, true, true, 0, RespawnMessage);
        }

        private static void ApplyDurabilityLoss(NWPlayer player)
        {
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                NWItem equipped = GetItemInSlot((InventorySlot)index, player);
                DurabilityService.RunItemDecay(player, equipped, SWLOR.Game.Server.Service.Random.NextFloat(0.15f, 0.50f));
            }

            foreach (var item in player.InventoryItems)
            {
                DurabilityService.RunItemDecay(player, item, SWLOR.Game.Server.Service.Random.NextFloat(0.10f, 0.25f));
            }
        }

        private static void OnModuleRespawn()
        {
            NWPlayer oPC = GetLastRespawnButtonPresser();
            ApplyDurabilityLoss(oPC);

            var amount = oPC.MaxHP / 2;
            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), oPC.Object);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), oPC.Object);

            var area = oPC.Area;
            
            TeleportPlayerToBindPoint(oPC);

            // If player is the last person in an instance, destroy the instance.
            if (AreaService.IsAreaInstance(area))
            {
                var playersInArea = NWModule.Get().Players.Count(x => x.Area == oPC.Area && x != oPC);

                if (playersInArea <= 0)
                {
                    DelayCommand(12.0f, () =>
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
            pc.RespawnAreaResref = GetResRef(player.Area);
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            FloatingTextStringOnCreature("You will return to this location the next time you die.", player.Object, false);
        }


        public static void TeleportPlayerToBindPoint(NWPlayer pc)
        {
            var entity = DataService.Player.GetByID(pc.GlobalID);
            TeleportPlayerToBindPoint(pc, entity);
        }

        private static void TeleportPlayerToBindPoint(NWObject pc, Player entity)
        {
            var area = GetArea(pc);
            // Instances
            if (AreaService.IsAreaInstance(area))
            {
                NWLocation entrance = GetLocalLocation(area, "INSTANCE_ENTRANCE");
                pc.AssignCommand(() =>
                {
                    ActionJumpToLocation(entrance);
                });
            }
            // Send player to default respawn point if no bind point is set.
            else if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
            {
                NWObject defaultRespawn = GetWaypointByTag("DEFAULT_RESPAWN_POINT");
                var location = defaultRespawn.Location;

                pc.AssignCommand(() =>
                {
                    ActionJumpToLocation(location);
                });
            }
            // Send player to their stored bind point.
            else
            {
                area = NWModule.Get().Areas.Single(x => GetResRef(x) == entity.RespawnAreaResref);
                var position = Vector3((float)entity.RespawnLocationX, (float)entity.RespawnLocationY, (float)entity.RespawnLocationZ);
                var location = Location(area, position, (float)entity.RespawnLocationOrientation);
                pc.AssignCommand(() =>
                {
                    ActionJumpToLocation(location);
                });
            }
        }
    }
}
