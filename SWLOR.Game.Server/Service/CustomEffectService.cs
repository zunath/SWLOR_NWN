using System;
using System.Linq;
using System.Runtime.InteropServices;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class CustomEffectService : ICustomEffectService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly AppState _state;
        private readonly IObjectProcessingService _ops;

        public CustomEffectService(IDataContext db,
            INWScript script,
            AppState state,
            IObjectProcessingService ops)
        {
            _db = db;
            _ = script;
            _state = state;
            _ops = ops;
        }
        
        public void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level, string data)
        {
            ApplyCustomEffect(caster, target, (int) effectType, ticks, level, data);
        }

        public int CalculateEffectAC(NWCreature creature)
        {
            int effectLevel = GetCustomEffectLevel(creature, CustomEffectType.ForceAura);
            
            int auraAC = 0;

            if (effectLevel > 0)
            {
                switch (effectLevel)
                {
                    case 1:
                        auraAC = 2;
                        break;
                    case 2:
                        auraAC = 3;
                        break;
                    case 3:
                        auraAC = 4;
                        break;
                    case 4:
                        auraAC = 5;
                        break;
                    case 5:
                        auraAC = 5;
                        break;
                    default: return 0;
                }
            }

            return auraAC;
        }

        public float CalculateEffectHPBonusPercent(NWCreature creature)
        {
            int effectLevel = GetCustomEffectLevel(creature, CustomEffectType.ShieldBoost);
            return effectLevel * 0.05f;
        }

        public void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var stance = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.CustomEffect.IsStance);
            if (stance?.StancePerkID == null) return;

            App.ResolveByInterface<ICustomEffect>("CustomEffect." + stance.CustomEffect.ScriptHandler, handler =>
            {
                handler?.Apply(player, player, stance.EffectiveLevel);
            });
        }

        public void OnModuleLoad()
        {
            _ops.RegisterProcessingEvent<CustomEffectProcessor>();
        }
        
        public void ApplyCustomEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
        {   
            // PC custom effects are tracked in the database.
            if (target.IsPlayer)
            {
                ApplyPCEffect(caster, target, customEffectID, ticks, effectiveLevel, data);
            }
            // NPCs custom effects are tracked in server memory.
            else
            {
                ApplyNPCEffect(caster, target, customEffectID, ticks, effectiveLevel, data);
            }
        }

        private void ApplyPCEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
        {
            Data.Entities.CustomEffect effectEntity = _db.CustomEffects.Single(x => x.CustomEffectID == customEffectID);
            PCCustomEffect entity = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == target.GlobalID && x.CustomEffectID == customEffectID);

            if (entity == null)
            {
                entity = new PCCustomEffect { PlayerID = target.GlobalID };
                _db.PCCustomEffects.Add(entity);
            }

            if (entity.EffectiveLevel > effectiveLevel)
            {
                caster.SendMessage("A more powerful effect already exists on your target.");
                return;
            }

            entity.CustomEffectID = customEffectID;
            entity.EffectiveLevel = effectiveLevel;
            entity.Ticks = ticks;
            entity.CasterNWNObjectID = _.ObjectToString(caster);
            _db.SaveChanges();

            target.SendMessage(effectEntity.StartMessage);
            
            App.ResolveByInterface<ICustomEffect>("CustomEffect." + effectEntity.ScriptHandler, handler =>
            {
                if (string.IsNullOrWhiteSpace(data))
                    data = handler?.Apply(caster, target, effectiveLevel);

                if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
                entity.Data = data;
                _db.SaveChanges();

                // Was already queued for removal, but got cast again. Take it out of the list to be removed.
                if (_state.PCEffectsForRemoval.Contains(entity.PCCustomEffectID))
                    _state.PCEffectsForRemoval.Remove(entity.PCCustomEffectID);
            });
        }
        
        private void ApplyNPCEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
        {
            Data.Entities.CustomEffect effectEntity = _db.CustomEffects.Single(x => x.CustomEffectID == customEffectID);
            // Look for existing effect.
            var spellModel = _state.NPCEffects.SingleOrDefault(x => x.Key.Caster.Equals(caster) &&
                                                                    x.Key.CustomEffectID == customEffectID &&
                                                                    x.Key.Target.Equals(target)).Key;

            if (spellModel == null)
            {
                spellModel = new CasterSpellVO
                {
                    Caster = caster,
                    CustomEffectID = customEffectID,
                    EffectName = effectEntity.Name,
                    Target = target
                };

                _state.NPCEffects.Add(spellModel, 0);
            }

            if (spellModel.EffectiveLevel > effectiveLevel)
            {
                caster.SendMessage("A more powerful effect already exists on your target.");
                return;
            }
            
            App.ResolveByInterface<ICustomEffect>("CustomEffect." + effectEntity.ScriptHandler, handler =>
            {
                if (string.IsNullOrWhiteSpace(data))
                    data = handler?.Apply(caster, target, effectiveLevel);
                if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
                spellModel.Data = data;

                _state.NPCEffects[spellModel] = ticks;
            });
            
        }

        public CustomEffectType GetCurrentStanceType(NWPlayer player)
        {
            var stanceEffect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.CustomEffect.IsStance);
            if (stanceEffect == null) return CustomEffectType.None;

            return (CustomEffectType) stanceEffect.CustomEffectID;
        }

        public bool RemoveStance(NWPlayer player, PCCustomEffect stanceEffect = null, bool sendMessage = true)
        {
            if (stanceEffect == null)
                stanceEffect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.CustomEffect.IsStance);
            if (stanceEffect == null) return false;
            
            if(sendMessage)
                player.SendMessage("You return to your normal stance.");
            
            int effectiveLevel = stanceEffect.EffectiveLevel;
            string data = stanceEffect.Data;
            string scriptHandler = stanceEffect.CustomEffect.ScriptHandler;

            _db.PCCustomEffects.Remove(stanceEffect);
            _db.SaveChanges();

            App.ResolveByInterface<ICustomEffect>("CustomEffect." + scriptHandler, handler =>
            {
                handler?.WearOff(player, player, effectiveLevel, data);
            });


            return true;
        }

        public void ApplyStance(NWPlayer player, CustomEffectType customEffect, PerkType perkType, int effectiveLevel, string data)
        {
            var dbEffect = _db.CustomEffects.Single(x => x.CustomEffectID == (int) customEffect);
            var pcStanceEffect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.CustomEffect.IsStance);
            int customEffectID = (int) customEffect;
            
            // Player selected to cancel their stance. Cancel it and end.
            if (pcStanceEffect != null && pcStanceEffect.CustomEffectID == customEffectID && pcStanceEffect.EffectiveLevel == effectiveLevel)
            {
                RemoveStance(player, pcStanceEffect);
                return;
            }
            // Otherwise remove existing stance
            else if (pcStanceEffect != null)
            {
                RemoveStance(player, pcStanceEffect, false);
            }

            pcStanceEffect = new PCCustomEffect
            {
                PlayerID = player.GlobalID,
                Ticks = -1
            };

            _db.PCCustomEffects.Add(pcStanceEffect);
            
            // Player selected to switch stances
            
            pcStanceEffect.CustomEffectID = customEffectID;
            pcStanceEffect.CasterNWNObjectID = _.ObjectToString(player);
            pcStanceEffect.EffectiveLevel = effectiveLevel;
            pcStanceEffect.StancePerkID = (int) perkType;

            _db.SaveChanges();
            
            App.ResolveByInterface<ICustomEffect>("CustomEffect." + dbEffect.ScriptHandler, handler =>
            {
                if (string.IsNullOrWhiteSpace(data))
                    data = handler?.Apply(player, player, effectiveLevel);
                
                if(!string.IsNullOrWhiteSpace(pcStanceEffect.CustomEffect.StartMessage))
                    player.SendMessage(pcStanceEffect.CustomEffect.StartMessage);

                if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
                pcStanceEffect.Data = data;
                _db.SaveChanges();
                
                // Was already queued for removal, but got cast again. Take it out of the list to be removed.
                if (_state.PCEffectsForRemoval.Contains(pcStanceEffect.PCCustomEffectID))
                    _state.PCEffectsForRemoval.Remove(pcStanceEffect.PCCustomEffectID);
            });
        }

        public bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID)
        {
            PCCustomEffect effect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.CustomEffectID == customEffectID);

            if (effect == null) return false;
            else if (_state.PCEffectsForRemoval.Contains(effect.PCCustomEffectID))
                return false;

            return true;
        }

        public bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            return DoesPCHaveCustomEffect(oPC, (int) customEffectType);
        }

        public void RemovePCCustomEffect(NWPlayer oPC, long customEffectID)
        {
            PCCustomEffect effect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.CustomEffectID == customEffectID);
            oPC.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + customEffectID);

            // Doesn't exist in DB or is already marked for removal
            if (effect == null ||
                _state.PCEffectsForRemoval.Contains(effect.PCCustomEffectID)) return;
            
            oPC.SendMessage(effect.CustomEffect.WornOffMessage);

            _state.PCEffectsForRemoval.Add(effect.PCCustomEffectID);
        }

        public void RemovePCCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            RemovePCCustomEffect(oPC, (long) customEffectType);
        }

        public int GetCustomEffectLevel(NWCreature creature, CustomEffectType customEffectType)
        {
            int effectLevel = 0;
            if (creature.IsNPC)
            {
                var effect = _state.NPCEffects.SingleOrDefault(x => x.Key.Target.Equals(creature) && x.Key.CustomEffectID == (int)customEffectType);

                if (effect.Key != null)
                    effectLevel = effect.Key.EffectiveLevel;
            }
            else if (creature.IsPlayer)
            {
                PCCustomEffect dbEffect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == creature.GlobalID && x.CustomEffectID == (int)customEffectType);
                if (dbEffect != null)
                {
                    if (!_state.PCEffectsForRemoval.Contains(dbEffect.PCCustomEffectID))
                    {
                        effectLevel = dbEffect.EffectiveLevel;
                    }
                }
            }
            else return 0;
            return effectLevel;
        }

    }
}
