using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class DeathService : IDeathService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IDurabilityService _durability;

        public DeathService(IDataContext db, 
            INWScript script,
            IRandomService random,
            IDurabilityService durability)
        {
            _db = db;
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

            PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            pc.RespawnLocationX = player.Position.m_X;
            pc.RespawnLocationY = player.Position.m_Y;
            pc.RespawnLocationZ = player.Position.m_Z;
            pc.RespawnLocationOrientation = player.Facing;
            pc.RespawnAreaResref = player.Area.Resref;

            _db.SaveChanges();
            _.FloatingTextStringOnCreature("You will respawn at this location the next time you die.", player.Object, FALSE);
        }


        public void TeleportPlayerToBindPoint(NWPlayer pc)
        {
            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == pc.GlobalID);
            TeleportPlayerToBindPoint(pc, entity);
        }

        private void TeleportPlayerToBindPoint(NWObject pc, PlayerCharacter entity)
        {
            if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
            {
                NWObject defaultRespawn = (_.GetWaypointByTag("DEFAULT_RESPAWN_POINT"));
                Location location = defaultRespawn.Location;

                pc.AssignCommand(() =>
                {
                    _.ActionJumpToLocation(location);
                });
            }
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
