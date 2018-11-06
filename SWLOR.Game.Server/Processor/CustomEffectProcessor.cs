﻿using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Processor
{
    public class CustomEffectProcessor: IEventProcessor
    {
        private readonly IDataService _data;
        private readonly IErrorService _error;
        private readonly INWScript _;
        private readonly AppCache _cache;
        private readonly INWNXObject _nwnxObject;
        private readonly ICustomEffectService _customEffect;

        public CustomEffectProcessor(IDataService data,
            IErrorService error,
            INWScript script,
            AppCache cache,
            INWNXObject nwnxObject,
            ICustomEffectService customEffect)
        {
            _data = data;
            _error = error;
            _ = script;
            _cache = cache;
            _nwnxObject = nwnxObject;
            _customEffect = customEffect;
        }


        public void Run(object[] args)
        {
            ProcessPCCustomEffects();
            ProcessNPCCustomEffects();
            ClearRemovedPCEffects();
        }
        private void ProcessPCCustomEffects()
        {
            foreach (var player in NWModule.Get().Players)
            {
                if (!player.IsInitializedAsPlayer) continue; // Ignored to prevent a timing issue where new characters would be included in this processing.

                List<PCCustomEffect> effects = _data.Where<PCCustomEffect>(x =>
                {
                    var customEffect = _data.Get<Data.Entity.CustomEffect>(x.CustomEffectID);
                    return x.PlayerID == player.GlobalID &&
                           customEffect.CustomEffectCategoryID != (int) CustomEffectCategoryType.Stance;
                }).ToList();

                foreach (var effect in effects)
                {
                    if (player.CurrentHP <= -11)
                    {
                        _customEffect.RemovePCCustomEffect(player, effect.CustomEffectID);
                        return;
                    }

                    PCCustomEffect result = RunPCCustomEffectProcess(player, effect);
                    if (result == null)
                    {
                        var customEffect = _data.Get<Data.Entity.CustomEffect>(effect.CustomEffectID);
                        string message = customEffect.WornOffMessage;
                        string scriptHandler = customEffect.ScriptHandler;
                        player.SendMessage(message);
                        player.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + effect.CustomEffectID);
                        _data.SubmitDataChange(effect, DatabaseActionType.Delete);
                        
                        App.ResolveByInterface<ICustomEffect>("CustomEffect." + scriptHandler, (handler) =>
                        {
                            handler?.WearOff(null, player, effect.EffectiveLevel, effect.Data);
                        });
                    }
                    else
                    {
                        _data.SubmitDataChange(effect, DatabaseActionType.Update);
                    }
                }
            }
        }

        private void ProcessNPCCustomEffects()
        {
            for (int index = _cache.NPCEffects.Count - 1; index >= 0; index--)
            {
                var entry = _cache.NPCEffects.ElementAt(index);
                CasterSpellVO casterModel = entry.Key;
                _cache.NPCEffects[entry.Key] = entry.Value - 1;
                Data.Entity.CustomEffect entity = _data.Single<Data.Entity.CustomEffect>(x => x.ID == casterModel.CustomEffectID);
                App.ResolveByInterface<ICustomEffect>("CustomEffect." + entity.ScriptHandler, (handler) =>
                {
                    try
                    {
                        handler?.Tick(casterModel.Caster, casterModel.Target, _cache.NPCEffects[entry.Key], casterModel.EffectiveLevel, casterModel.Data);
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

                        _cache.NPCEffects.Remove(entry.Key);
                    }
                });

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

            if(effect.Ticks > 0)
                effect.Ticks = effect.Ticks - 1;

            if (effect.Ticks == 0) return null;
            var customEffect = _data.Get<Data.Entity.CustomEffect>(effect.CustomEffectID);

            if (!string.IsNullOrWhiteSpace(customEffect.ContinueMessage) &&
                effect.Ticks % 6 == 0) // Only show the message once every six seconds
            {
                oPC.SendMessage(customEffect.ContinueMessage);
            }

            App.ResolveByInterface<ICustomEffect>("CustomEffect." + customEffect.ScriptHandler, (handler) =>
            {
                handler?.Tick(caster, oPC, effect.Ticks, effect.EffectiveLevel, effect.Data);
            });
            
            return effect;
        }

        private void ClearRemovedPCEffects()
        {
            var records = _data.Where<PCCustomEffect>(x => _cache.PCEffectsForRemoval.Contains(x.ID)).ToList();

            foreach (var record in records)
            {
                _data.SubmitDataChange(record, DatabaseActionType.Delete);
            }
            _cache.PCEffectsForRemoval.Clear();
        }
    }
}
