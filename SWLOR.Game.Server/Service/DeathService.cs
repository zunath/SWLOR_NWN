using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class DeathService : IDeathService
    {
        private readonly IDataService _data;
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IDurabilityService _durability;

        public DeathService(IDataService data, 
            INWScript script,
            IRandomService random,
            IDurabilityService durability)
        {
            _data = data;
            _ = script;
            _random = random;
            _durability = durability;
        }
        

        // Message which displays on the Respawn pop up menu
        private const string RespawnMessage = "You have died. You can wait for another player to revive you or respawn to go to your last respawn point.";
        
        public void OnPlayerDeath()
        {
            NWPlayer player = (_.GetLastPlayerDied());
            NWObject hostile = (_.GetLastHostileActor(player.Object));

            var factionMember = _.GetFirstFactionMember(hostile.Object, FALSE);
            while (_.GetIsObjectValid(factionMember) == TRUE)
            {
                _.ClearPersonalReputation(player.Object, factionMember);
                factionMember = _.GetNextFactionMember(hostile.Object, FALSE);
            }

            for (int index = 0; index < NUM_INVENTORY_SLOTS; index++)
            {
                NWItem equipped = _.GetItemInSlot(index, player);
                _durability.RunItemDecay(player, equipped, _random.RandomFloat(0.02f, 0.07f));
            }

            foreach (var item in player.InventoryItems)
            {
                _durability.RunItemDecay(player, item, _random.RandomFloat(0.02f, 0.07f));
            }

            _.PopUpDeathGUIPanel(player.Object, TRUE, TRUE, 0, RespawnMessage);
        }
        
        public void OnPlayerRespawn()
        {
            NWPlayer oPC = (_.GetLastRespawnButtonPresser());

            int amount = oPC.MaxHP / 2;
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectResurrection(), oPC.Object);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(amount), oPC.Object);

            TeleportPlayerToBindPoint(oPC);
        }
        

        public void SetRespawnLocation(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player), nameof(player) + " cannot be null.");
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object), nameof(player.Object) + " cannot be null.");

            PlayerCharacter pc = _data.Single<PlayerCharacter>(x => x.ID == player.GlobalID);
            pc.RespawnLocationX = player.Position.m_X;
            pc.RespawnLocationY = player.Position.m_Y;
            pc.RespawnLocationZ = player.Position.m_Z;
            pc.RespawnLocationOrientation = player.Facing;
            pc.RespawnAreaResref = player.Area.Resref;
            _data.SubmitDataChange(pc, DatabaseActionType.Update);
            _.FloatingTextStringOnCreature("You will return to this location the next time you die.", player.Object, FALSE);
        }


        public void TeleportPlayerToBindPoint(NWPlayer pc)
        {
            PlayerCharacter entity = _data.Single<PlayerCharacter>(x => x.ID == pc.GlobalID);
            TeleportPlayerToBindPoint(pc, entity);
        }

        private void TeleportPlayerToBindPoint(NWObject pc, PlayerCharacter entity)
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
                NWObject defaultRespawn = (_.GetWaypointByTag("DEFAULT_RESPAWN_POINT"));
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
