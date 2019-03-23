using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class CustomEffectService
    {   
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

        public static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var stance = DataService.SingleOrDefault<PCCustomEffect>(x =>
            {
                var customEffect = DataService.Get<Data.Entity.CustomEffect>(x.CustomEffectID);
                return x.PlayerID == player.GlobalID &&
                       customEffect.CustomEffectCategoryID == (int) CustomEffectCategoryType.Stance;
            });
            if (stance?.StancePerkID == null) return;
            var stanceEffect = DataService.Get<Data.Entity.CustomEffect>(stance.CustomEffectID);

            App.ResolveByInterface<ICustomEffect>("CustomEffect." + stanceEffect.ScriptHandler, handler =>
            {
                handler?.Apply(player, player, stance.EffectiveLevel);
            });
        }

        public static void OnModuleLoad()
        {
            ObjectProcessingService.RegisterProcessingEvent<CustomEffectProcessor>();
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
            Data.Entity.CustomEffect customEffect = DataService.Single<Data.Entity.CustomEffect>(x => x.ID == customEffectID);
            PCCustomEffect pcEffect = DataService.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == target.GlobalID && x.CustomEffectID == customEffectID);
            CustomEffectCategoryType category = (CustomEffectCategoryType) customEffect.CustomEffectCategoryID;

            if(category == CustomEffectCategoryType.FoodEffect)
            {
                var customEffectPC = DataService.Get<Data.Entity.CustomEffect>(pcEffect.CustomEffectID);
                if (pcEffect != null && customEffectPC.CustomEffectCategoryID == (int) category)
                {
                    caster.SendMessage("You are not hungry.");
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

            target.SendMessage(customEffect.StartMessage);
            
            App.ResolveByInterface<ICustomEffect>("CustomEffect." + customEffect.ScriptHandler, handler =>
            {
                if (string.IsNullOrWhiteSpace(data))
                    data = handler?.Apply(caster, target, effectiveLevel);

                if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
                pcEffect.Data = data;
                DataService.SubmitDataChange(pcEffect, DatabaseActionType.Update);

                // Was already queued for removal, but got cast again. Take it out of the list to be removed.
                if (AppCache.PCEffectsForRemoval.Contains(pcEffect.ID))
                    AppCache.PCEffectsForRemoval.Remove(pcEffect.ID);
            });
        }
        
        private static void ApplyNPCEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data)
        {
            Data.Entity.CustomEffect effectEntity = DataService.Single<Data.Entity.CustomEffect>(x => x.ID == customEffectID);
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

            App.ResolveByInterface<ICustomEffect>("CustomEffect." + effectEntity.ScriptHandler, handler =>
            {
                if (string.IsNullOrWhiteSpace(data))
                    data = handler?.Apply(caster, target, effectiveLevel);
                if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
                spellModel.Data = data;

                AppCache.NPCEffects[spellModel] = ticks;
            });
            
        }

        public static CustomEffectType GetCurrentStanceType(NWPlayer player)
        {
            var stanceEffect = DataService.SingleOrDefault<PCCustomEffect>(x =>
            {
                if (x.PlayerID != player.GlobalID) return false;

                var customEffect = DataService.Get<Data.Entity.CustomEffect>(x.CustomEffectID);
                return customEffect.CustomEffectCategoryID == (int) CustomEffectCategoryType.Stance;
            });
            if (stanceEffect == null) return CustomEffectType.None;

            return (CustomEffectType) stanceEffect.CustomEffectID;
        }

        public static bool RemoveStance(NWPlayer player, PCCustomEffect stanceEffect = null, bool sendMessage = true)
        {
            if (stanceEffect == null)
                stanceEffect = DataService.SingleOrDefault<PCCustomEffect>(x =>
                {
                    var customEffect = DataService.Get<Data.Entity.CustomEffect>(x.CustomEffectID);
                    return x.PlayerID == player.GlobalID &&
                           customEffect.CustomEffectCategoryID == (int) CustomEffectCategoryType.Stance;
                });
            if (stanceEffect == null) return false;
            
            if(sendMessage)
                player.SendMessage("You return to your normal stance.");
            
            int effectiveLevel = stanceEffect.EffectiveLevel;
            string data = stanceEffect.Data;
            var stanceCustomEffect = DataService.Get<Data.Entity.CustomEffect>(stanceEffect.CustomEffectID);
            string scriptHandler = stanceCustomEffect.ScriptHandler;
            DataService.SubmitDataChange(stanceEffect, DatabaseActionType.Delete);
            
            App.ResolveByInterface<ICustomEffect>("CustomEffect." + scriptHandler, handler =>
            {
                handler?.WearOff(player, player, effectiveLevel, data);
            });
            
            return true;
        }

        public static void ApplyStance(NWPlayer player, CustomEffectType customEffect, PerkType perkType, int effectiveLevel, string data)
        {
            var dbEffect = DataService.Single<Data.Entity.CustomEffect>(x => x.ID == (int) customEffect);
            var pcStanceEffect = DataService.SingleOrDefault<PCCustomEffect>(x =>
            {
                var ce = DataService.Get<Data.Entity.CustomEffect>(x.CustomEffectID);
                return x.PlayerID == player.GlobalID &&
                       ce.CustomEffectCategoryID == (int) CustomEffectCategoryType.Stance;
            });
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

            // Player selected to switch stances
            pcStanceEffect = new PCCustomEffect
            {
                PlayerID = player.GlobalID,
                Ticks = -1,
                CustomEffectID = customEffectID,
                CasterNWNObjectID = _.ObjectToString(player),
                EffectiveLevel = effectiveLevel,
                StancePerkID = (int)perkType
            };
            DataService.SubmitDataChange(pcStanceEffect, DatabaseActionType.Insert);
            
            App.ResolveByInterface<ICustomEffect>("CustomEffect." + dbEffect.ScriptHandler, handler =>
            {
                if (string.IsNullOrWhiteSpace(data))
                    data = handler?.Apply(player, player, effectiveLevel);

                var stanceCustomEffect = DataService.Get<Data.Entity.CustomEffect>(pcStanceEffect.CustomEffectID);
                if(!string.IsNullOrWhiteSpace(stanceCustomEffect.StartMessage))
                    player.SendMessage(stanceCustomEffect.StartMessage);

                if (string.IsNullOrWhiteSpace(data)) data = string.Empty;
                pcStanceEffect.Data = data;
                DataService.SubmitDataChange(pcStanceEffect, DatabaseActionType.Update);
                
                // Was already queued for removal, but got cast again. Take it out of the list to be removed.
                if (AppCache.PCEffectsForRemoval.Contains(pcStanceEffect.ID))
                    AppCache.PCEffectsForRemoval.Remove(pcStanceEffect.ID);
            });
        }

        public static bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID)
        {
            PCCustomEffect effect = DataService.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == oPC.GlobalID && x.CustomEffectID == customEffectID);

            if (effect == null) return false;
            else if (AppCache.PCEffectsForRemoval.Contains(effect.ID))
                return false;

            return true;
        }

        public static bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType)
        {
            return DoesPCHaveCustomEffect(oPC, (int) customEffectType);
        }

        public static bool DoesPCHaveCustomEffectByCategory(NWPlayer player, CustomEffectCategoryType category)
        {
            var pcEffect = DataService.GetAll<PCCustomEffect>().FirstOrDefault(x =>
            {
                var customEffect = DataService.Get<Data.Entity.CustomEffect>(x.CustomEffectID);
                return customEffect.CustomEffectCategoryID == (int) category;
            });

            return pcEffect != null;
        }

        public static void RemovePCCustomEffect(NWPlayer oPC, int customEffectID)
        {
            PCCustomEffect effect = DataService.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == oPC.GlobalID && x.CustomEffectID == customEffectID);
            oPC.DeleteLocalInt("CUSTOM_EFFECT_ACTIVE_" + customEffectID);

            // Doesn't exist in DB or is already marked for removal
            if (effect == null ||
                AppCache.PCEffectsForRemoval.Contains(effect.ID)) return;
            var customEffect = DataService.Get<Data.Entity.CustomEffect>(effect.CustomEffectID);

            oPC.SendMessage(customEffect.WornOffMessage);

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
                PCCustomEffect dbEffect = DataService.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == creature.GlobalID && x.CustomEffectID == (int)customEffectType);
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

        public static ForceSpreadDetails GetForceSpreadDetails(NWPlayer player)
        {
            PCCustomEffect spreadEffect = DataService.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == player.GlobalID && x.CustomEffectID == (int)CustomEffectType.ForceSpread);
            ForceSpreadDetails details = new ForceSpreadDetails();
            string spreadData = spreadEffect?.Data ?? string.Empty;

            details.Level = spreadEffect?.EffectiveLevel ?? 0;
            details.Uses = spreadEffect == null ? 0 : Convert.ToInt32(spreadData.Split(',')[0]);
            details.Range = spreadEffect == null ? 0 : Convert.ToSingle(spreadData.Split(',')[1]);

            return details;
        }

        public static void SetForceSpreadUses(NWPlayer player, int uses)
        {
            PCCustomEffect spreadEffect = DataService.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == player.GlobalID && x.CustomEffectID == (int)CustomEffectType.ForceSpread);
            if (spreadEffect == null) return;

            if (uses <= 0)
            {
                RemovePCCustomEffect(player, CustomEffectType.ForceSpread);
            }

            string spreadData = spreadEffect.Data ?? string.Empty;

            float range = Convert.ToSingle(spreadData.Split(',')[1]);
            spreadEffect.Data = uses + "," + range;
            DataService.SubmitDataChange(spreadEffect, DatabaseActionType.Update);
            player.SendMessage("Force Spread uses remaining: " + uses);

        }

    }
}
