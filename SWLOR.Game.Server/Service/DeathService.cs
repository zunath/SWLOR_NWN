using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class DeathService : IDeathService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly ISerializationService _serialization;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;

        public DeathService(IDataContext db, 
            INWScript script,
            ISerializationService serialization,
            IRandomService random,
            IColorTokenService color)
        {
            _db = db;
            _ = script;
            _serialization = serialization;
            _random = random;
            _color = color;
        }


        // Resref and tag of the player corpse placeable
        private const string CorpsePlaceableResref = "pc_corpse";

        // Message which displays on the Respawn pop up menu
        private const string RespawnMessage = "You have died. You can wait for another player to revive you or give up and permanently go to the death realm.";


        public void OnModuleLoad()
        {
            List<PCCorpse> entities = _db.PCCorpses.ToList();

            foreach (PCCorpse entity in entities)
            {
                NWArea area = NWArea.Wrap(_.GetObjectByTag(entity.AreaTag));
                Vector position = new Vector((float)entity.PositionX, (float)entity.PositionY, (float)entity.PositionZ);
                Location location = _.Location(area.Object, position, (float)entity.Orientation);
                NWPlaceable corpse = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, CorpsePlaceableResref, location));
                corpse.Name = entity.Name;
                corpse.IdentifiedDescription = entity.Name;
                corpse.SetLocalInt("CORPSE_ID", (int)entity.PCCorpseID);

                foreach (PCCorpseItem item in entity.PCCorpseItems)
                {
                    _serialization.DeserializeItem(item.NWItemObject, corpse);
                }
            }
        }

        public void OnPlayerDeath()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastPlayerDied());
            string corpseName = oPC.Name + "'s Corpse";
            NWObject oHostileActor = NWObject.Wrap(_.GetLastHostileActor(oPC.Object));
            Location location = oPC.Location;
            bool hasItems = false;

            var factionMember = _.GetFirstFactionMember(oHostileActor.Object, NWScript.FALSE);
            while(_.GetIsObjectValid(factionMember) == NWScript.TRUE)
            {
                _.ClearPersonalReputation(oPC.Object, factionMember);

                factionMember = _.GetNextFactionMember(oHostileActor.Object, NWScript.FALSE);
            }

            _.PopUpDeathGUIPanel(oPC.Object, NWScript.TRUE, NWScript.TRUE, 0, RespawnMessage);

            NWPlaceable corpse = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, CorpsePlaceableResref, location));
            PCCorpse entity = new PCCorpse
            {
                AreaTag = _.GetTag(_.GetAreaFromLocation(location)),
                Name = corpseName,
                Orientation = _.GetFacingFromLocation(location),
                PositionX = _.GetPositionFromLocation(location).m_X,
                PositionY = _.GetPositionFromLocation(location).m_Y,
                PositionZ = _.GetPositionFromLocation(location).m_Z
            };

            if (oPC.Gold > 0)
            {
                corpse.AssignCommand(() =>
                {
                    _.TakeGoldFromCreature(oPC.Gold, oPC.Object);
                });

                hasItems = true;
            }

            foreach (NWItem item in oPC.InventoryItems)
            {
                if (!item.IsCursed)
                {
                    _.CopyItem(item.Object, corpse.Object, NWScript.TRUE);
                    item.Destroy();
                    hasItems = true;
                }
            }

            if (!hasItems)
            {
                corpse.Destroy();
                return;
            }

            corpse.Name = corpseName;
            corpse.IdentifiedDescription = corpseName;

            foreach (NWItem corpseItem in corpse.InventoryItems)
            {
                PCCorpseItem corpseItemEntity = new PCCorpseItem
                {
                    GlobalID = corpseItem.GlobalID,
                    NWItemObject = _serialization.Serialize(corpseItem),
                    PCCorpseItemID = entity.PCCorpseID,
                    ItemName = corpseItem.Name,
                    ItemTag = corpseItem.Tag,
                    ItemResref = corpseItem.Resref
                };
                entity.PCCorpseItems.Add(corpseItemEntity);
            }

            _db.PCCorpses.Add(entity);
            _db.SaveChanges();
            corpse.SetLocalInt("CORPSE_ID", (int)entity.PCCorpseID);
        }

        public void OnPlayerDying()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastPlayerDying());
            oPC.ClearAllActions();
            DeathFunction(oPC, 8);
        }

        private void DeathFunction(NWPlayer oPC, int nDC)
        {
            if (!oPC.IsValid) return;
            int iHP = oPC.CurrentHP;

            //Player Rolls a random number between 1 and 20+ConMod
            int iRoll = 20 + oPC.ConstitutionModifier;
            iRoll = _random.Random(iRoll) + 1;

            //Sanity Check
            if (nDC > 30) nDC = 30;
            else if (nDC < 4) nDC = 4;

            if (iHP <= 0)
            {
                if (iRoll >= nDC) //Stabilize
                {
                    nDC -= 2;
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(1), oPC.Object);
                    oPC.DelayCommand(() =>
                    {
                        DeathFunction(oPC, nDC);
                    }, 3.0f);
                }
                else  //Failed!
                {
                    if (_random.Random(2) + 1 == 1) nDC++;
                    Effect eResult = _.EffectDamage(1);

                    //Death!
                    if (iHP <= -9)
                    {
                        _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectVisualEffect(NWScript.VFX_IMP_DEATH), oPC.Object);
                        _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectDeath(), oPC.Object);
                        return;
                    }
                    else
                    {
                        oPC.SendMessage(_color.Orange("You failed to stabilize this round."));
                    }
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, eResult, oPC.Object);

                    oPC.DelayCommand(() =>
                    {
                        DeathFunction(oPC, nDC);
                    }, 3.0f);
                }
            }
        }
        public void OnPlayerRespawn()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastRespawnButtonPresser());

            int amount = oPC.MaxHP / 2;
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectResurrection(), oPC.Object);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(amount), oPC.Object);

            TeleportPlayerToBindPoint(oPC);
        }

        public void OnCorpseDisturb(NWPlaceable corpse)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastDisturbed());

            if (!oPC.IsPlayer && !oPC.IsDM) return;
            
            NWItem oItem = NWItem.Wrap(_.GetInventoryDisturbItem());
            int disturbType = _.GetInventoryDisturbType();

            if (disturbType == NWScript.INVENTORY_DISTURB_TYPE_ADDED)
            {
                _.ActionGiveItem(oItem.Object, oPC.Object);
                oPC.FloatingText("You cannot put items into corpses.");
            }
            else
            {
                PCCorpseItem dbItem = _db.PCCorpseItems.SingleOrDefault(x => x.GlobalID == oItem.GlobalID);
                if (dbItem == null) return;
                
                _db.PCCorpseItems.Remove(dbItem);
                _db.SaveChanges();
            }
        }

        public void OnCorpseClose(NWPlaceable corpse)
        {
            var items = corpse.InventoryItems;
            if (items.Count <= 0)
            {
                int corpseID = corpse.GetLocalInt("CORPSE_ID");
                PCCorpse entity = _db.PCCorpses.Single(x => x.PCCorpseID == corpseID);
                _db.PCCorpses.Remove(entity);
                _db.SaveChanges();
                corpse.Destroy();
            }
        }


        public void BindPlayerSoul(NWPlayer player, bool showMessage)
        {
            if (player == null) throw new ArgumentNullException(nameof(player), nameof(player) + " cannot be null.");
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object), nameof(player.Object) + " cannot be null.");

            PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            pc.RespawnLocationX = player.Position.m_X;
            pc.RespawnLocationY = player.Position.m_Y;
            pc.RespawnLocationZ = player.Position.m_Z;
            pc.RespawnLocationOrientation = player.Facing;
            pc.RespawnAreaTag = player.Area.Tag;

            _db.SaveChanges();

            if (showMessage)
            {
                _.FloatingTextStringOnCreature("Your soul has been bound to this location.", player.Object, NWScript.FALSE);
            }
        }


        public void TeleportPlayerToBindPoint(NWPlayer pc)
        {
            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == pc.GlobalID);
            TeleportPlayerToBindPoint(pc, entity);
        }

        private void TeleportPlayerToBindPoint(NWObject pc, PlayerCharacter entity)
        {
            if (entity.CurrentHunger < 50)
                entity.CurrentHunger = 50;

            if (string.IsNullOrWhiteSpace(entity.RespawnAreaTag))
            {
                NWObject defaultRespawn = NWObject.Wrap(_.GetWaypointByTag("DEFAULT_RESPAWN_POINT"));
                Location location = defaultRespawn.Location;

                pc.AssignCommand(() =>
                {
                    _.ActionJumpToLocation(location);
                });
            }
            else
            {
                pc.AssignCommand(() =>
                {
                    NWArea area = NWArea.Wrap(_.GetObjectByTag(entity.RespawnAreaTag));
                    Vector position = _.Vector((float)entity.RespawnLocationX, (float)entity.RespawnLocationY, (float)entity.RespawnLocationZ);
                    Location location = _.Location(area.Object, position, (float)entity.RespawnLocationOrientation);
                    _.ActionJumpToLocation(location);
                });
            }
        }
    }
}
