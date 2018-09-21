using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class CustomEffectService : ICustomEffectService
    {
        private IDataContext _db;
        private readonly IErrorService _error;
        private readonly INWScript _;
        private readonly AppState _state;
        private readonly IObjectProcessingService _ops;
        private readonly INWNXObject _nwnxObject;

        public CustomEffectService(IDataContext db,
            IErrorService error,
            INWScript script,
            AppState state,
            IObjectProcessingService ops,
            INWNXObject nwnxObject)
        {
            _db = db;
            _error = error;
            _ = script;
            _state = state;
            _ops = ops;
            _nwnxObject = nwnxObject;
        }
        
        public void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level, string data)
        {
            ApplyCustomEffect(caster, target, (int) effectType, ticks, level, data);
        }

        public int CalculateEffectAC(NWCreature creature)
        {
            int effectLevel = GetCustomEffectLevel(creature, CustomEffectType.Aegis);

            int aegisAC = 0;

            if (effectLevel > 0)
            {
                switch (effectLevel)
                {
                    case 1:
                        aegisAC = 1;
                        break;
                    case 2:
                    case 3:
                        aegisAC = 2;
                        break;
                    case 4:
                        aegisAC = 3;
                        break;
                    case 5:
                        aegisAC = 4;
                        break;
                    default: return 0;
                }
            }

            return aegisAC;
        }

        public float CalculateEffectHPBonusPercent(NWCreature creature)
        {
            int effectLevel = GetCustomEffectLevel(creature, CustomEffectType.ShieldBoost);
            return effectLevel * 0.05f;
        }

        public void OnModuleLoad()
        {
            _ops.RegisterProcessingEvent(() =>
            {
                ProcessPCCustomEffects();
                ProcessNPCCustomEffects();
            });
        }

        private void ProcessPCCustomEffects()
        {
            foreach (var player in NWModule.Get().Players)
            {
                List<PCCustomEffect> effects = _db.PCCustomEffects.Where(x => x.PlayerID == player.GlobalID).ToList();

                foreach (var effect in effects)
                {
                    if (player.CurrentHP <= -11)
                    {
                        RemovePCCustomEffect(player, effect.CustomEffectID);
                        return;
                    }

                    PCCustomEffect result = RunPCCustomEffectProcess(player, effect);
                    if (result == null)
                    {
                        string message = effect.CustomEffect.WornOffMessage;
                        string scriptHandler = effect.CustomEffect.ScriptHandler;
                        player.SendMessage(message);
                        player.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + effect.CustomEffectID);
                        _db.PCCustomEffects.Remove(effect);
                        _db.SaveChanges();

                        ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + scriptHandler);
                        handler?.WearOff(null, player, effect.EffectiveLevel, effect.Data);
                    }
                    else
                    {
                        _db.SaveChanges();
                    }
                }
            }
        }

        private void ProcessNPCCustomEffects()
        {
            for (int index = _state.NPCEffects.Count-1; index >= 0; index--)
            {
                var entry = _state.NPCEffects.ElementAt(index);
                CasterSpellVO casterModel = entry.Key;
                _state.NPCEffects[entry.Key] = entry.Value - 1;
                Data.Entities.CustomEffect entity = _db.CustomEffects.Single(x => x.CustomEffectID == casterModel.CustomEffectID);
                ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + entity.ScriptHandler);

                try
                {
                    handler?.Tick(casterModel.Caster, casterModel.Target, casterModel.EffectiveLevel, casterModel.Data);
                }
                catch (Exception ex)
                {
                    _error.LogError(ex, "CustomEffectService processor was unable to run specific effect script: " + entity.ScriptHandler);
                }


                // Kill the effect if it has expired, target is invalid, or target is dead.
                if (entry.Value <= 0 ||
                    !casterModel.Target.IsValid ||
                    casterModel.Target.CurrentHP <= -11)
                {
                    handler?.WearOff(casterModel.Caster, casterModel.Target, casterModel.EffectiveLevel, casterModel.Data);

                    if (casterModel.Caster.IsValid && casterModel.Caster.IsPlayer)
                    {
                        casterModel.Caster.SendMessage("Your effect '" + casterModel.EffectName + "' has worn off of " + casterModel.Target.Name);
                    }

                    casterModel.Target.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + casterModel.CustomEffectID);

                    _state.NPCEffects.Remove(entry.Key);
                }
            }
        }

        private PCCustomEffect RunPCCustomEffectProcess(NWPlayer oPC, PCCustomEffect effect)
        {
            NWCreature caster = oPC;
            if (!string.IsNullOrWhiteSpace(effect.CasterNWNObjectID))
            {
                var obj = _nwnxObject.StringToObject(effect.CasterNWNObjectID);
                if (obj.IsValid)
                {
                    caster = obj.Object;
                }
            }

            effect.Ticks = effect.Ticks - 1;
            if (effect.Ticks < 0) return null;

            if (!string.IsNullOrWhiteSpace(effect.CustomEffect.ContinueMessage) && 
                effect.Ticks % 6 == 0) // Only show the message once every six seconds
            {
                oPC.SendMessage(effect.CustomEffect.ContinueMessage);
            }

            ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + effect.CustomEffect.ScriptHandler);

            handler?.Tick(caster, oPC, effect.EffectiveLevel, effect.Data);

            return effect;
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

            ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + effectEntity.ScriptHandler);
            
            if(string.IsNullOrWhiteSpace(data))
                data = handler?.Apply(caster, target, effectiveLevel);
            if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
            entity.Data = data;
            _db.SaveChanges();
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
            
            ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + effectEntity.ScriptHandler);
            
            if(string.IsNullOrWhiteSpace(data))
                data = handler?.Apply(caster, target, effectiveLevel);
            if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
            spellModel.Data = data;

            _state.NPCEffects[spellModel] = ticks;
        }

        public bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID)
        {
            PCCustomEffect effect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.CustomEffectID == customEffectID);

            return effect != null;
        }

        public bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            return DoesPCHaveCustomEffect(oPC, (int) customEffectType);
        }

        public void RemovePCCustomEffect(NWPlayer oPC, long customEffectID)
        {
            PCCustomEffect effect = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.CustomEffectID == customEffectID);
            oPC.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + customEffectID);

            if (effect == null) return;
            
            oPC.SendMessage(effect.CustomEffect.WornOffMessage);
            _db.PCCustomEffects.Remove(effect);
            _db.SaveChanges();
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
                    effectLevel = dbEffect.EffectiveLevel;
                }
            }
            else return 0;
            return effectLevel;
        }

    }
}
