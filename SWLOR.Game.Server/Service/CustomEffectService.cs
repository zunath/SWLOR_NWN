using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Processor;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class CustomEffectService
    {
        private static readonly Dictionary<CustomEffectType, ICustomEffectHandler> _customEffectHandlers;

        static CustomEffectService()
        {
            _customEffectHandlers = new Dictionary<CustomEffectType, ICustomEffectHandler>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => ProcessCustomEffects());
        }

        public static void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level, string data)
        {
            ApplyCustomEffect(caster, target, (int) effectType, ticks, level, data);
        }

        public static int CalculateEffectAC(NWCreature creature)
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

        public static float CalculateEffectHPBonusPercent(NWCreature creature)
        {
            int effectLevel = GetCustomEffectLevel(creature, CustomEffectType.ShieldBoost);
            return effectLevel * 0.05f;
        }

        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var pcEffect = DataService.PCCustomEffect.GetByPlayerStanceOrDefault(player.GlobalID);
            if (pcEffect?.StancePerkID == null) return;
            ICustomEffectHandler handler = GetCustomEffectHandler(pcEffect.CustomEffectID);
            handler?.Apply(player, player, pcEffect.EffectiveLevel);
        }

        private static void OnModuleLoad()
        {
            RegisterCustomEffectHandlers();
        }

        private static void RegisterCustomEffectHandlers()
        {
            // Use reflection to get all of CustomEffectBehaviour implementations.
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ICustomEffectHandler).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                ICustomEffectHandler instance = Activator.CreateInstance(type) as ICustomEffectHandler;
                if(instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _customEffectHandlers.Add(instance.CustomEffectType, instance);
            }
        }

        public static ICustomEffectHandler GetCustomEffectHandler(CustomEffectType type)
        {
            if(!_customEffectHandlers.ContainsKey(type))
            {
                throw new Exception("Unable to locate a ICustomEffectBehavior implementation for type " + type);
            }

            return _customEffectHandlers[type];
        }

        public static ICustomEffectHandler GetCustomEffectHandler(int typeID)
        {
            CustomEffectType type = (CustomEffectType) typeID;
            return GetCustomEffectHandler(type);
        }
        
        public static void ApplyCustomEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
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

        private static void ApplyPCEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
        {
            Data.Entity.CustomEffect customEffect = DataService.CustomEffect.GetByID(customEffectID);
            PCCustomEffect pcEffect = DataService.PCCustomEffect.GetByPlayerIDAndCustomEffectIDOrDefault(target.GlobalID, customEffectID);
            ICustomEffectHandler handler = GetCustomEffectHandler(customEffectID);
            CustomEffectCategoryType category = handler.CustomEffectCategoryType;

            if (category == CustomEffectCategoryType.FoodEffect)
            {
                var customEffectPC = DataService.CustomEffect.GetByID(pcEffect.CustomEffectID);
                if (customEffectPC != null)
                {
                    var foodHandler = GetCustomEffectHandler(customEffectPC.ID);
                    if (foodHandler.CustomEffectCategoryType == category)
                    {
                        caster.SendMessage("You are not hungry.");
                    }
                    return;
                }
            }

            DatabaseActionType action = DatabaseActionType.Update;
            if (pcEffect == null)
            {
                pcEffect = new PCCustomEffect { PlayerID = target.GlobalID };
                action = DatabaseActionType.Insert;
            }

            if (pcEffect.EffectiveLevel > effectiveLevel)
            {
                caster.SendMessage("A more powerful effect already exists on your target.");
                return;
            }

            pcEffect.CustomEffectID = customEffectID;
            pcEffect.EffectiveLevel = effectiveLevel;
            pcEffect.Ticks = ticks;
            pcEffect.CasterNWNObjectID = _.ObjectToString(caster);
            DataService.SubmitDataChange(pcEffect, action);

            target.SendMessage(handler.StartMessage);
            if (string.IsNullOrWhiteSpace(data))
                data = handler?.Apply(caster, target, effectiveLevel);

            if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
            pcEffect.Data = data;
            DataService.SubmitDataChange(pcEffect, DatabaseActionType.Update);

            // Was already queued for removal, but got cast again. Take it out of the list to be removed.
            if (AppCache.PCEffectsForRemoval.Contains(pcEffect.ID))
                AppCache.PCEffectsForRemoval.Remove(pcEffect.ID);
        }
        
        private static void ApplyNPCEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
        {
            Data.Entity.CustomEffect effectEntity = DataService.CustomEffect.GetByID(customEffectID);
            // Look for existing effect.
            var spellModel = AppCache.NPCEffects.SingleOrDefault(x => x.Key.Caster.Equals(caster) &&
                                                                    x.Key.CustomEffectID == customEffectID &&
                                                                    x.Key.Target.Equals(target)).Key;

            if (spellModel == null)
            {
                spellModel = new CasterSpellVO
                {
                    Caster = caster,
                    CustomEffectID = customEffectID,
                    EffectName = effectEntity.Name,
                    Target = target,
                    EffectiveLevel = effectiveLevel
                };

                AppCache.NPCEffects.Add(spellModel, 0);
            }

            if (spellModel.EffectiveLevel > effectiveLevel)
            {
                caster.SendMessage("A more powerful effect already exists on your target.");
                return;
            }
            ICustomEffectHandler handler = GetCustomEffectHandler(customEffectID);
            if (string.IsNullOrWhiteSpace(data))
                data = handler?.Apply(caster, target, effectiveLevel);
            if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
            spellModel.Data = data;

            AppCache.NPCEffects[spellModel] = ticks;
        }

        public static CustomEffectType GetCurrentStanceType(NWPlayer player)
        {
            var stanceEffect = DataService.PCCustomEffect.GetByPlayerStanceOrDefault(player.GlobalID);
            if (stanceEffect == null) return CustomEffectType.None;

            return (CustomEffectType) stanceEffect.CustomEffectID;
        }

        public static bool RemoveStance(NWCreature creature, PCCustomEffect stanceEffect = null, bool sendMessage = true)
        {
            // Can't process NPC stances at the moment. Need to do some more refactoring before this is possible.
            // todo: handle NPC stances.
            if (!creature.IsPlayer) return false;

            if (stanceEffect == null)
                stanceEffect = DataService.PCCustomEffect.GetByPlayerStanceOrDefault(creature.GlobalID);
            if (stanceEffect == null) return false;
            
            if(sendMessage)
                creature.SendMessage("You return to your normal stance.");
            
            int effectiveLevel = stanceEffect.EffectiveLevel;
            string data = stanceEffect.Data;
            DataService.SubmitDataChange(stanceEffect, DatabaseActionType.Delete);
            ICustomEffectHandler handler = GetCustomEffectHandler(stanceEffect.CustomEffectID);
            handler?.WearOff(creature, creature, effectiveLevel, data);
            
            return true;
        }

        public static void ApplyStance(NWCreature creature, CustomEffectType customEffect, PerkType perkType, int effectiveLevel, string data)
        {
            // Can't process NPC stances at the moment. Need to do some more refactoring before this is possible.
            // todo: handle NPC stances.
            if (!creature.IsPlayer) return;

            var pcStanceEffect = DataService.PCCustomEffect.GetByPlayerStanceOrDefault(creature.GlobalID);
            int customEffectID = (int) customEffect;
            
            // Player selected to cancel their stance. Cancel it and end.
            if (pcStanceEffect != null && pcStanceEffect.CustomEffectID == customEffectID && pcStanceEffect.EffectiveLevel == effectiveLevel)
            {
                RemoveStance(creature, pcStanceEffect);
                return;
            }
            // Otherwise remove existing stance
            else if (pcStanceEffect != null)
            {
                RemoveStance(creature, pcStanceEffect, false);
            }

            // Player selected to switch stances
            pcStanceEffect = new PCCustomEffect
            {
                PlayerID = creature.GlobalID,
                Ticks = -1,
                CustomEffectID = customEffectID,
                CasterNWNObjectID = _.ObjectToString(creature),
                EffectiveLevel = effectiveLevel,
                StancePerkID = (int)perkType
            };
            DataService.SubmitDataChange(pcStanceEffect, DatabaseActionType.Insert);
            ICustomEffectHandler handler = GetCustomEffectHandler(customEffect);
            if (string.IsNullOrWhiteSpace(data))
                data = handler.Apply(creature, creature, effectiveLevel);
            
            if (!string.IsNullOrWhiteSpace(handler.StartMessage))
                creature.SendMessage(handler.StartMessage);

            if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
            pcStanceEffect.Data = data;
            DataService.SubmitDataChange(pcStanceEffect, DatabaseActionType.Update);

            // Was already queued for removal, but got cast again. Take it out of the list to be removed.
            if (AppCache.PCEffectsForRemoval.Contains(pcStanceEffect.ID))
                AppCache.PCEffectsForRemoval.Remove(pcStanceEffect.ID);
        }

        public static bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID)
        {
            PCCustomEffect effect = DataService.PCCustomEffect.GetByPlayerIDAndCustomEffectIDOrDefault(oPC.GlobalID, customEffectID);

            if (effect == null) return false;
            else if (AppCache.PCEffectsForRemoval.Contains(effect.ID))
                return false;

            return true;
        }

        public static bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            return DoesPCHaveCustomEffect(oPC, (int) customEffectType);
        }
        
        public static void RemovePCCustomEffect(NWPlayer oPC, int customEffectID)
        {
            PCCustomEffect effect = DataService.PCCustomEffect.GetByPlayerIDAndCustomEffectIDOrDefault(oPC.GlobalID, customEffectID);
            oPC.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + customEffectID);

            // Doesn't exist in DB or is already marked for removal
            if (effect == null ||
                AppCache.PCEffectsForRemoval.Contains(effect.ID)) return;
            var handler = GetCustomEffectHandler(customEffectID);

            oPC.SendMessage(handler.WornOffMessage);

            AppCache.PCEffectsForRemoval.Add(effect.ID);
        }

        public static void RemovePCCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            RemovePCCustomEffect(oPC, (int) customEffectType);
        }

        public static int GetCustomEffectLevel(NWCreature creature, CustomEffectType customEffectType)
        {
            int effectLevel = 0;
            if (creature.IsNPC)
            {
                var effect = AppCache.NPCEffects.SingleOrDefault(x => x.Key.Target.Equals(creature) && x.Key.CustomEffectID == (int)customEffectType);

                if (effect.Key != null)
                    effectLevel = effect.Key.EffectiveLevel;
            }
            else if (creature.IsPlayer)
            {
                PCCustomEffect dbEffect = DataService.PCCustomEffect.GetByPlayerIDAndCustomEffectIDOrDefault(creature.GlobalID, (int)customEffectType);
                if (dbEffect != null)
                {
                    if (!AppCache.PCEffectsForRemoval.Contains(dbEffect.ID))
                    {
                        effectLevel = dbEffect.EffectiveLevel;
                    }
                }
            }
            else return 0;
            return effectLevel;
        }

        private static void ProcessCustomEffects()
        {
            using(new Profiler(nameof(CustomEffectService) + "." + nameof(ProcessCustomEffects)))
            {
                ProcessPCCustomEffects();
                ProcessNPCCustomEffects();
                ClearRemovedPCEffects();
            }
        }

        private static void ProcessPCCustomEffects()
        {
            foreach (var player in NWModule.Get().Players)
            {
                if (!player.IsInitializedAsPlayer) continue; // Ignored to prevent a timing issue where new characters would be included in this processing.

                List<PCCustomEffect> effects = DataService.PCCustomEffect.GetAllByPlayerID(player.GlobalID).Where(x => x.StancePerkID == null).ToList();

                foreach (var effect in effects)
                {
                    if (player.CurrentHP <= -11)
                    {
                        CustomEffectService.RemovePCCustomEffect(player, effect.CustomEffectID);
                        return;
                    }

                    PCCustomEffect result = RunPCCustomEffectProcess(player, effect);
                    if (result == null)
                    {
                        ICustomEffectHandler handler = CustomEffectService.GetCustomEffectHandler(effect.CustomEffectID);
                        string message = handler.WornOffMessage;
                        player.SendMessage(message);
                        player.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + effect.CustomEffectID);
                        DataService.SubmitDataChange(effect, DatabaseActionType.Delete);
                        handler.WearOff(null, player, effect.EffectiveLevel, effect.Data);

                    }
                    else
                    {
                        DataService.SubmitDataChange(effect, DatabaseActionType.Update);
                    }
                }
            }
        }

        private static void ProcessNPCCustomEffects()
        {
            for (int index = AppCache.NPCEffects.Count - 1; index >= 0; index--)
            {
                var entry = AppCache.NPCEffects.ElementAt(index);
                CasterSpellVO casterModel = entry.Key;
                AppCache.NPCEffects[entry.Key] = entry.Value - 1;
                Data.Entity.CustomEffect entity = DataService.CustomEffect.GetByID(casterModel.CustomEffectID);
                ICustomEffectHandler handler = CustomEffectService.GetCustomEffectHandler(casterModel.CustomEffectID);

                try
                {
                    handler?.Tick(casterModel.Caster, casterModel.Target, AppCache.NPCEffects[entry.Key], casterModel.EffectiveLevel, casterModel.Data);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError(ex, "CustomEffectService processor was unable to run specific effect script for custom effect ID: " + entity.ID);
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

                    AppCache.NPCEffects.Remove(entry.Key);
                }
            }
        }

        private static PCCustomEffect RunPCCustomEffectProcess(NWPlayer oPC, PCCustomEffect effect)
        {
            NWCreature caster = oPC;
            if (!string.IsNullOrWhiteSpace(effect.CasterNWNObjectID))
            {
                var obj = NWNXObject.StringToObject(effect.CasterNWNObjectID);
                if (obj.IsValid)
                {
                    caster = obj.Object;
                }
            }

            if (effect.Ticks > 0)
                effect.Ticks = effect.Ticks - 1;

            if (effect.Ticks == 0) return null;
            ICustomEffectHandler handler = CustomEffectService.GetCustomEffectHandler(effect.CustomEffectID);

            if (!string.IsNullOrWhiteSpace(handler.ContinueMessage) &&
                effect.Ticks % 6 == 0) // Only show the message once every six seconds
            {
                oPC.SendMessage(handler.ContinueMessage);
            }
            handler?.Tick(caster, oPC, effect.Ticks, effect.EffectiveLevel, effect.Data);

            return effect;
        }

        private static void ClearRemovedPCEffects()
        {
            var records = DataService.PCCustomEffect.GetAllByPCCustomEffectID(AppCache.PCEffectsForRemoval.ToList());

            foreach (var record in records)
            {
                DataService.SubmitDataChange(record, DatabaseActionType.Delete);
            }
            AppCache.PCEffectsForRemoval.Clear();
        }
    }
}
