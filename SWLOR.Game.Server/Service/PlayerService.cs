using System;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Service
{
    public class PlayerService : IPlayerService
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly IErrorService _error;
        private readonly INWNXCreature _nwnxCreature;
        private readonly INWNXPlayer _player;
        private readonly INWNXPlayerQuickBarSlot _qbs;
        private readonly IDialogService _dialog;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IBackgroundService _background;
        private readonly IRaceService _race;
        private readonly IDurabilityService _durability;
        private readonly IPlayerStatService _stat;
        private readonly ILanguageService _language;

        public PlayerService(
            INWScript script,
            IColorTokenService color,
            IDataService data, 
            IErrorService error,
            INWNXCreature nwnxCreature,
            INWNXPlayer player,
            INWNXPlayerQuickBarSlot qbs,
            IDialogService dialog,
            INWNXEvents nwnxEvents,
            IBackgroundService background,
            IRaceService race,
            IDurabilityService durability,
            IPlayerStatService stat,
            ILanguageService language)
        {
            _ = script;
            _color = color;
            _data = data;
            _error = error;
            _nwnxCreature = nwnxCreature;
            _player = player;
            _qbs = qbs;
            _dialog = dialog;
            _nwnxEvents = nwnxEvents;
            _background = background;
            _race = race;
            _durability = durability;
            _stat = stat;
            _language = language;
        }

        public void InitializePlayer(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));
            if (!player.IsPlayer) return;

            // Player is initialized but not in the DB. Wipe the tag and rerun them through initialization - something went wrong before.
            if (player.IsInitializedAsPlayer)
            {
                if (_data.GetAll<Player>().SingleOrDefault(x => x.ID == player.GlobalID) == null)
                {
                    _.SetTag(player, string.Empty);
                }
            }

            if (!player.IsInitializedAsPlayer)
            {
                player.DestroyAllInventoryItems();
                player.InitializePlayer();
                _.AssignCommand(player, () => _.TakeGoldFromCreature(_.GetGold(player), player, 1));

                _.DelayCommand(0.5f, () =>
                {
                    _.GiveGoldToCreature(player, 100);
                });

                // Capture original stats before we level up the player.
                int str = _nwnxCreature.GetRawAbilityScore(player, ABILITY_STRENGTH);
                int con = _nwnxCreature.GetRawAbilityScore(player, ABILITY_CONSTITUTION);
                int dex = _nwnxCreature.GetRawAbilityScore(player, ABILITY_DEXTERITY);
                int @int = _nwnxCreature.GetRawAbilityScore(player, ABILITY_INTELLIGENCE);
                int wis = _nwnxCreature.GetRawAbilityScore(player, ABILITY_WISDOM);
                int cha = _nwnxCreature.GetRawAbilityScore(player, ABILITY_CHARISMA);

                // Take player to level 5 in NWN levels so that we have access to more HP slots
                _.GiveXPToCreature(player, 10000);

                for (int level = 1; level <= 5; level++)
                {
                    _.LevelUpHenchman(player, player.Class1);
                }

                // Set stats back to how they were on entry.
                _nwnxCreature.SetRawAbilityScore(player, ABILITY_STRENGTH, str);
                _nwnxCreature.SetRawAbilityScore(player, ABILITY_CONSTITUTION, con);
                _nwnxCreature.SetRawAbilityScore(player, ABILITY_DEXTERITY, dex);
                _nwnxCreature.SetRawAbilityScore(player, ABILITY_INTELLIGENCE, @int);
                _nwnxCreature.SetRawAbilityScore(player, ABILITY_WISDOM, wis);
                _nwnxCreature.SetRawAbilityScore(player, ABILITY_CHARISMA, cha);

                NWItem knife = (_.CreateItemOnObject("survival_knife", player));
                knife.Name = player.Name + "'s Survival Knife";
                knife.IsCursed = true;
                _durability.SetMaxDurability(knife, 5);
                _durability.SetDurability(knife, 5);
                
                NWItem book = (_.CreateItemOnObject("player_guide", player));
                book.Name = player.Name + "'s Player Guide";
                book.IsCursed = true;

                NWItem dyeKit = (_.CreateItemOnObject("tk_omnidye", player));
                dyeKit.IsCursed = true;
                
                int numberOfFeats = _nwnxCreature.GetFeatCount(player);
                for (int currentFeat = numberOfFeats; currentFeat >= 0; currentFeat--)
                {
                    _nwnxCreature.RemoveFeat(player, _nwnxCreature.GetFeatByIndex(player, currentFeat - 1));
                }

                _nwnxCreature.AddFeatByLevel(player, FEAT_ARMOR_PROFICIENCY_LIGHT, 1);
                _nwnxCreature.AddFeatByLevel(player, FEAT_ARMOR_PROFICIENCY_MEDIUM, 1);
                _nwnxCreature.AddFeatByLevel(player, FEAT_ARMOR_PROFICIENCY_HEAVY, 1);
                _nwnxCreature.AddFeatByLevel(player, FEAT_SHIELD_PROFICIENCY, 1);
                _nwnxCreature.AddFeatByLevel(player, FEAT_WEAPON_PROFICIENCY_EXOTIC, 1);
                _nwnxCreature.AddFeatByLevel(player, FEAT_WEAPON_PROFICIENCY_MARTIAL, 1);
                _nwnxCreature.AddFeatByLevel(player, FEAT_WEAPON_PROFICIENCY_SIMPLE, 1);
                _nwnxCreature.AddFeatByLevel(player, (int) CustomFeatType.StructureManagementTool, 1);
                _nwnxCreature.AddFeatByLevel(player, (int) CustomFeatType.OpenRestMenu, 1);
                _nwnxCreature.AddFeatByLevel(player, (int) CustomFeatType.RenameCraftedItem, 1);
                _nwnxCreature.AddFeatByLevel(player, (int) CustomFeatType.ChatCommandTargeter, 1);

                for (int iCurSkill = 1; iCurSkill <= 27; iCurSkill++)
                {
                    _nwnxCreature.SetSkillRank(player, iCurSkill - 1, 0);
                }
                _.SetFortitudeSavingThrow(player, 0);
                _.SetReflexSavingThrow(player, 0);
                _.SetWillSavingThrow(player, 0);

                int classID = _.GetClassByPosition(1, player);

                for (int index = 0; index <= 255; index++)
                {
                    _nwnxCreature.RemoveKnownSpell(player, classID, 0, index);
                }

                Player entity = CreateDBPCEntity(player);
                _data.SubmitDataChange(entity, DatabaseActionType.Insert);
                
                var skills = _data.GetAll<Skill>();
                foreach (var skill in skills)
                {
                    var pcSkill = new PCSkill
                    {
                        IsLocked = false,
                        SkillID = skill.ID,
                        PlayerID = entity.ID,
                        Rank = 0,
                        XP = 0
                    };
                    
                    _data.SubmitDataChange(pcSkill, DatabaseActionType.Insert);
                }

                _race.ApplyDefaultAppearance(player);
                _nwnxCreature.SetAlignmentLawChaos(player, 50);
                _nwnxCreature.SetAlignmentGoodEvil(player, 50);
                _background.ApplyBackgroundBonuses(player);

                _stat.ApplyStatChanges(player, null, true);
                _language.InitializePlayerLanguages(player);

                _.DelayCommand(1.0f, () => _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(999), player));

                InitializeHotBar(player);
            }

        }
        
        private Player CreateDBPCEntity(NWPlayer player)
        {
            CustomRaceType race = (CustomRaceType)player.RacialType;
            AssociationType assType; 
            int goodEvil = _.GetAlignmentGoodEvil(player);
            int lawChaos = _.GetAlignmentLawChaos(player);
            
            // Jedi Order -- Mandalorian -- Sith Empire
            if(goodEvil == ALIGNMENT_GOOD && lawChaos == ALIGNMENT_LAWFUL)
            {
                assType = AssociationType.JediOrder;
            }
            else if(goodEvil == ALIGNMENT_GOOD && lawChaos == ALIGNMENT_NEUTRAL)
            {
                assType = AssociationType.Mandalorian;
            }
            else if(goodEvil == ALIGNMENT_GOOD && lawChaos == ALIGNMENT_CHAOTIC)
            {
                assType = AssociationType.SithEmpire;
            }

            // Smugglers -- Unaligned -- Hutt Cartel
            else if(goodEvil == ALIGNMENT_NEUTRAL && lawChaos == ALIGNMENT_LAWFUL)
            {
                assType = AssociationType.Smugglers;
            }
            else if(goodEvil == ALIGNMENT_NEUTRAL && lawChaos == ALIGNMENT_NEUTRAL)
            {
                assType = AssociationType.Unaligned;
            }
            else if(goodEvil == ALIGNMENT_NEUTRAL && lawChaos == ALIGNMENT_CHAOTIC)
            {
                assType = AssociationType.HuttCartel;
            }

            // Republic -- Czerka -- Sith Order
            else if(goodEvil == ALIGNMENT_EVIL && lawChaos == ALIGNMENT_LAWFUL)
            {
                assType = AssociationType.Republic;
            }
            else if(goodEvil == ALIGNMENT_EVIL && lawChaos == ALIGNMENT_NEUTRAL)
            {
                assType = AssociationType.Czerka;
            }
            else if(goodEvil == ALIGNMENT_EVIL && lawChaos == ALIGNMENT_CHAOTIC)
            {
                assType = AssociationType.SithOrder;
            }
            else
            {
                throw new Exception("Association type not found. GoodEvil = " + goodEvil + ", LawChaos = " + lawChaos);
            }

            int sp = 5;
            if (race == CustomRaceType.Human)
                sp++;

            Player entity = new Player
            {
                ID = player.GlobalID,
                CharacterName = player.Name,
                HitPoints = player.CurrentHP,
                LocationAreaResref = _.GetResRef(_.GetAreaFromLocation(player.Location)),
                LocationX = player.Position.m_X,
                LocationY = player.Position.m_Y,
                LocationZ = player.Position.m_Z,
                LocationOrientation = player.Facing,
                CreateTimestamp = DateTime.UtcNow,
                UnallocatedSP = sp,
                HPRegenerationAmount = 1,
                RegenerationTick = 20,
                RegenerationRate = 0,
                VersionNumber = 1,
                MaxFP = 0,
                CurrentFP = 0,
                CurrentFPTick = 20,
                RespawnAreaResref = string.Empty,
                RespawnLocationX = 0.0f,
                RespawnLocationY = 0.0f,
                RespawnLocationZ = 0.0f,
                RespawnLocationOrientation = 0.0f,
                DateSanctuaryEnds = DateTime.UtcNow + TimeSpan.FromDays(3),
                IsSanctuaryOverrideEnabled = false,
                STRBase = _nwnxCreature.GetRawAbilityScore(player, ABILITY_STRENGTH),
                DEXBase = _nwnxCreature.GetRawAbilityScore(player, ABILITY_DEXTERITY),
                CONBase = _nwnxCreature.GetRawAbilityScore(player, ABILITY_CONSTITUTION),
                INTBase = _nwnxCreature.GetRawAbilityScore(player, ABILITY_INTELLIGENCE),
                WISBase = _nwnxCreature.GetRawAbilityScore(player, ABILITY_WISDOM),
                CHABase = _nwnxCreature.GetRawAbilityScore(player, ABILITY_CHARISMA),
                TotalSPAcquired = 0,
                DisplayHelmet = true,
                PrimaryResidencePCBaseStructureID = null,
                PrimaryResidencePCBaseID = null,
                AssociationID = (int)assType,
                DisplayHolonet = true,
                DisplayDiscord = true, 
                XPBonus = 0,
                LeaseRate = 0
            };

            return entity;
        }

        public Player GetPlayerEntity(NWPlayer player)
        {
            if(player == null) throw new ArgumentNullException(nameof(player));
            if(!player.IsPlayer) throw new ArgumentException(nameof(player) + " must be a player.", nameof(player));

            return _data.Get<Player>(player.GlobalID);
        }

        public Player GetPlayerEntity(Guid playerID)
        {
            if (playerID == null) throw new ArgumentException("Invalid player ID.", nameof(playerID));
            return _data.Get<Player>(playerID);
        }

        public void OnAreaEnter()
        {
            NWPlayer player = (_.GetEnteringObject());

            SaveLocation(player);
            if(player.IsPlayer)
                _.ExportSingleCharacter(player);
        }

        public void LoadCharacter(NWPlayer player)
        {
            if (!player.IsPlayer) return;

            Player entity = GetPlayerEntity(player.GlobalID);

            if (entity == null) return;

            int hp = player.CurrentHP;
            int damage;
            if (entity.HitPoints < 0)
            {
                damage = hp + Math.Abs(entity.HitPoints);
            }
            else
            {
                damage = hp - entity.HitPoints;
            }

            if (damage != 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage), player);
            }

            player.IsBusy = false; // Just in case player logged out in the middle of an action.

            // Cleanup code in case people log out as spaceships.
            int appearance = player.Chest.GetLocalInt("APPEARANCE");
            if (appearance > 0 && appearance != _.GetAppearanceType(player))
            {
                _.SetCreatureAppearanceType(player, appearance);
                _.SetObjectVisualTransform(player, OBJECT_VISUAL_TRANSFORM_SCALE, 1.0f);
            }
        }

        public void ShowMOTD(NWPlayer player)
        {
            ServerConfiguration config = _data.GetAll<ServerConfiguration>().First();
            string message = _color.Green("Welcome to " + config.ServerName + "!\n\nMOTD: ") + _color.White(config.MessageOfTheDay);

            _.DelayCommand(6.5f, () =>
            {
                player.SendMessage(message);
            });
        }

        public void SaveCharacter(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            Player entity = GetPlayerEntity(player);
            entity.CharacterName = player.Name;
            entity.HitPoints = player.CurrentHP;

            _data.SubmitDataChange(entity, DatabaseActionType.Update);
        }

        public void SaveLocation(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            if (player.GetLocalInt("IS_SHIP") == 1) return;
            if (player.GetLocalInt("IS_GUNNER") == 1) return;
            
            NWArea area = player.Area;
            if (area.IsValid && area.Tag != "ooc_area" && area.Tag != "tutorial" && !area.IsInstance)
            {
                _error.Trace("SPACE", "Saving location in area " + _.GetName(area));
                Player entity = GetPlayerEntity(player.GlobalID);
                entity.LocationAreaResref = area.Resref;
                entity.LocationX = player.Position.m_X;
                entity.LocationY = player.Position.m_Y;
                entity.LocationZ = player.Position.m_Z;
                entity.LocationOrientation = (player.Facing);
                entity.LocationInstanceID = null;

                if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
                {
                    NWObject waypoint = _.GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT");
                    entity.RespawnAreaResref = waypoint.Area.Resref;
                    entity.RespawnLocationOrientation = waypoint.Facing;
                    entity.RespawnLocationX = waypoint.Position.m_X;
                    entity.RespawnLocationY = waypoint.Position.m_Y;
                    entity.RespawnLocationZ = waypoint.Position.m_Z;
                }

                _data.SubmitDataChange(entity, DatabaseActionType.Update);
            }
            else if (area.IsInstance)
            {
                _error.Trace("SPACE", "Saving location in instance area " + _.GetName(area));
                string instanceID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    instanceID = area.GetLocalString("PC_BASE_ID");
                }

                _error.Trace("SPACE", "Saving character in instance ID: " + instanceID);

                if (!string.IsNullOrWhiteSpace(instanceID))
                {
                    Player entity = GetPlayerEntity(player.GlobalID);
                    entity.LocationAreaResref = area.Resref;
                    entity.LocationX = player.Position.m_X;
                    entity.LocationY = player.Position.m_Y;
                    entity.LocationZ = player.Position.m_Z;
                    entity.LocationOrientation = (player.Facing);
                    entity.LocationInstanceID = new Guid(instanceID);

                    _data.SubmitDataChange(entity, DatabaseActionType.Update);
                }
            }
        }
                
        private void InitializeHotBar(NWPlayer player)
        {
            var openRestMenu = _qbs.UseFeat((int)CustomFeatType.OpenRestMenu);
            var structure = _qbs.UseFeat((int) CustomFeatType.StructureManagementTool);
            var renameCraftedItem = _qbs.UseFeat((int)CustomFeatType.RenameCraftedItem);
            var chatCommandTargeter = _qbs.UseFeat((int)CustomFeatType.ChatCommandTargeter);

            _player.SetQuickBarSlot(player, 0, openRestMenu);
            _player.SetQuickBarSlot(player, 1, structure);
            _player.SetQuickBarSlot(player, 2, renameCraftedItem);
            _player.SetQuickBarSlot(player, 3, chatCommandTargeter);
        }

        public void OnModuleUseFeat()
        {
            NWPlayer pc = (Object.OBJECT_SELF);
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();

            if (featID != (int)CustomFeatType.OpenRestMenu) return;
            pc.ClearAllActions();
            _dialog.StartConversation(pc, pc, "RestMenu");
        }
    }
}
