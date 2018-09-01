using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class CustomEffectService : ICustomEffectService
    {
        private readonly IDataContext _db;
        private readonly IErrorService _error;
        private readonly INWScript _;
        private readonly AppState _state;

        public CustomEffectService(IDataContext db,
            IErrorService error,
            INWScript script,
            AppState state)
        {
            _db = db;
            _error = error;
            _ = script;
            _state = state;
        }

        public int GetActiveEffectLevel(NWObject target, CustomEffectType effectType)
        {
            return GetActiveEffectLevel(target, (int)effectType);
        }

        public void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level)
        {
            ApplyCustomEffect(caster, target, (int) effectType, ticks, level);
        }

        public int CalculateEffectAC(NWCreature creature)
        {
            int effectLevel = GetActiveEffectLevel(creature, CustomEffectType.Aegis);
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

        public void OnPlayerHeartbeat(NWPlayer oPC)
        {
            List<PCCustomEffect> effects = _db.PCCustomEffects.Where(x => x.PlayerID == oPC.GlobalID).ToList();
            string areaResref = oPC.Area.Resref;

            foreach (PCCustomEffect effect in effects)
            {
                if (oPC.CurrentHP <= -11 || areaResref == "death_realm")
                {
                    RemovePCCustomEffect(oPC, effect.CustomEffectID);
                    return;
                }

                PCCustomEffect result = RunPCCustomEffectProcess(oPC, effect);
                if (result == null)
                {
                    string message = effect.CustomEffect.WornOffMessage;
                    string scriptHandler = effect.CustomEffect.ScriptHandler;
                    oPC.SendMessage(message);
                    oPC.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + effect.CustomEffectID);
                    _db.PCCustomEffects.Remove(effect);
                    _db.SaveChanges();

                    ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + scriptHandler);
                    handler?.WearOff(null, oPC);
                }
                else
                {
                    _db.SaveChanges();
                }
            }
        }


        public void OnModuleHeartbeat()
        {
            foreach (var entry in _state.NPCEffects)
            {
                CasterSpellVO casterModel = entry.Key;
                _state.NPCEffects[entry.Key] = entry.Value - 1;
                Data.Entities.CustomEffect entity = _db.CustomEffects.Single(x => x.CustomEffectID == casterModel.CustomEffectID);
                ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + entity.ScriptHandler);

                try
                {
                    handler?.Tick(casterModel.Caster, casterModel.Target);
                }
                catch (Exception ex)
                {
                    _error.LogError(ex, "OnModuleHeartbeat was unable to run specific effect script: " + entity.ScriptHandler);
                }


                // Kill the effect if it has expired, target is invalid, or target is dead.
                if (entry.Value <= 0 ||
                        !casterModel.Target.IsValid ||
                        casterModel.Target.CurrentHP <= -11)
                {
                    _state.EffectsToRemove.Add(entry.Key);

                    handler?.WearOff(casterModel.Caster, casterModel.Target);

                    if (casterModel.Caster.IsValid && casterModel.Caster.IsPlayer)
                    {
                        casterModel.Caster.SendMessage("Your effect '" + casterModel.EffectName + "' has worn off of " + casterModel.Target.Name);
                    }

                    casterModel.Target.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + casterModel.CustomEffectID);
                }
            }

            foreach (CasterSpellVO entry in _state.EffectsToRemove)
            {
                _state.NPCEffects.Remove(entry);
            }
            _state.EffectsToRemove.Clear();
        }

        private PCCustomEffect RunPCCustomEffectProcess(NWPlayer oPC, PCCustomEffect effect)
        {
            effect.Ticks = effect.Ticks - 1;
            if (effect.Ticks < 0) return null;

            if (!string.IsNullOrWhiteSpace(effect.CustomEffect.ContinueMessage))
            {
                oPC.SendMessage(effect.CustomEffect.ContinueMessage);
            }

            ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + effect.CustomEffect.ScriptHandler);

            handler?.Tick(null, oPC);

            return effect;
        }

        public void ApplyCustomEffect(NWCreature oCaster, NWCreature oTarget, int customEffectID, int ticks, int effectLevel)
        {
            // Can't apply the effect if the existing one is stronger.
            int existingEffectLevel = GetActiveEffectLevel(oTarget, customEffectID);
            if (existingEffectLevel > effectLevel)
            {
                oCaster.SendMessage("A more powerful effect already exists on your target.");
                return;
            }
            
            Data.Entities.CustomEffect effectEntity = _db.CustomEffects.Single(x => x.CustomEffectID == customEffectID);

            // PC custom effects are tracked in the database.
            if (oTarget.IsPlayer)
            {
                PCCustomEffect entity = _db.PCCustomEffects.SingleOrDefault(x => x.PlayerID == oTarget.GlobalID && x.CustomEffectID == customEffectID);

                if (entity == null)
                {
                    entity = new PCCustomEffect
                    {
                        PlayerID = oTarget.GlobalID,
                        CustomEffectID = customEffectID
                    };

                    _db.PCCustomEffects.Add(entity);
                }

                entity.Ticks = ticks;
                _db.SaveChanges();

                oTarget.SendMessage(effectEntity.StartMessage);
            }
            // NPCs custom effects are tracked in server memory.
            else
            {
                // Look for existing effect.
                foreach (var entry in _state.NPCEffects)
                {
                    CasterSpellVO casterSpellModel = entry.Key;

                    if (casterSpellModel.Caster.Equals(oCaster) &&
                       casterSpellModel.CustomEffectID == customEffectID &&
                       casterSpellModel.Target.Equals(oTarget))
                    {
                        _state.NPCEffects[entry.Key] = ticks;
                        return;
                    }
                }

                // Didn't find an existing effect. Create a new one.
                CasterSpellVO spellModel = new CasterSpellVO
                {
                    Caster = oCaster,
                    CustomEffectID = customEffectID,
                    EffectName = effectEntity.Name,
                    Target = oTarget
                };

                _state.NPCEffects[spellModel] = ticks;
            }

            ICustomEffect handler = App.ResolveByInterface<ICustomEffect>("CustomEffect." + effectEntity.ScriptHandler);
            handler?.Apply(oCaster, oTarget);
            oTarget.SetLocalInt("CUSTOM_EFFECT_ACTIVE_" + customEffectID, effectLevel);
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

            _db.PCCustomEffects.Remove(effect);
            oPC.SendMessage(effect.CustomEffect.WornOffMessage);
        }

        public void RemovePCCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            RemovePCCustomEffect(oPC, (long) customEffectType);
        }

        public int GetActiveEffectLevel(NWObject oTarget, int customEffectID)
        {
            string varName = "CUSTOM_EFFECT_ACTIVE_" + customEffectID;
            return oTarget.GetLocalInt(varName);
        }


    }
}
