﻿using System;
using System.Linq;
using System.Threading;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleHeartbeat : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IItemService _item;
        private readonly IAbilityService _ability;
        private readonly IPerkService _perk;
        private readonly IBaseService _base;
        private readonly IPlayerStatService _playerStat;
        private readonly IWeatherService _weather;
        
        public OnModuleHeartbeat(INWScript script,
            IDataService data,
            IItemService item,
            IAbilityService ability,
            IPerkService perk,
            IBaseService @base,
            IPlayerStatService playerStat,
            IWeatherService weather)
        {
            _ = script;
            _data = data;
            _item = item;
            _ability = ability;
            _perk = perk;
            _base = @base;
            _playerStat = playerStat;
            _weather = weather;
        }

        public bool Run(params object[] args)
        {
            Guid[] playerIDs = NWModule.Get().Players.Where(x => x.IsPlayer).Select(x => x.GlobalID).ToArray();
            var entities = _data.Where<Data.Entity.Player>(x => playerIDs.Contains(x.ID)).ToList();

            foreach (var player in NWModule.Get().Players)
            {
                var entity = entities.SingleOrDefault(x => x.ID == player.GlobalID);
                if (entity == null) continue;

                HandleRegenerationTick(player, entity);
                HandleFPRegenerationTick(player, entity);

                _data.SubmitDataChange(entity, DatabaseActionType.Update);
            }
            
            SaveCharacters();
            _base.OnModuleHeartbeat();
            _weather.OnModuleHeartbeat();

            // todo: use for debugging the memleak issue. Leave in for now - will remove after it's been fixed.

            //System.Diagnostics.Process ThisProcess = System.Diagnostics.Process.GetCurrentProcess();

            //int nullThreadCount = 0;
            //foreach (System.Diagnostics.ProcessThread OneThread in ThisProcess.Threads)
            //{
            //    if (OneThread != null)
            //    {
            //        Console.WriteLine(OneThread.Id + ": " +
            //                          OneThread.ThreadState + ": " +
            //                          OneThread.StartTime + ": " +
            //                          OneThread.TotalProcessorTime + "<BR>");
            //    }
            //    else
            //    {

            //        nullThreadCount++;
            //    }
            //}

            //ThreadPool.GetMaxThreads(out int workers, out int completionPort);
            //Console.WriteLine("Max threads: " + workers + " workers, " + completionPort + " completionPorts" );
            //Console.WriteLine("Null thread count: " + nullThreadCount);



            return true;
        }
        
        // Export all characters every minute.
        private void SaveCharacters()
        {
            int currentTick = NWModule.Get().GetLocalInt("SAVE_CHARACTERS_TICK") + 1;

            if (currentTick >= 10)
            {
                _.ExportAllCharacters();
                currentTick = 0;
            }

            NWModule.Get().SetLocalInt("SAVE_CHARACTERS_TICK", currentTick);
        }

        private void HandleRegenerationTick(NWPlayer oPC, Data.Entity.Player entity)
        {
            entity.RegenerationTick = entity.RegenerationTick - 1;
            int rate = 20;
            int amount = entity.HPRegenerationAmount;

            if (entity.RegenerationTick <= 0)
            {
                if (oPC.CurrentHP < oPC.MaxHP)
                {
                    var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(oPC);
                    // CON bonus
                    int con = oPC.ConstitutionModifier;
                    if (con > 0)
                    {
                        amount += con;
                    }
                    amount += effectiveStats.HPRegen;
                    
                    if (oPC.Chest.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        int sturdinessLevel = _perk.GetPCPerkLevel(oPC, PerkType.Sturdiness);
                        if (sturdinessLevel > 0)
                        {
                            amount += sturdinessLevel + 1;
                        }
                    }
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(amount), oPC.Object);
                }

                entity.RegenerationTick = rate;
            }
        }

        private void HandleFPRegenerationTick(NWPlayer oPC, Data.Entity.Player entity)
        {
            entity.CurrentFPTick = entity.CurrentFPTick - 1;
            int rate = 20;
            int amount = 1;

            if (entity.CurrentFPTick <= 0)
            {
                if (entity.CurrentFP < entity.MaxFP)
                {
                    var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(oPC);
                    // CHA bonus
                    int cha = oPC.CharismaModifier;
                    if (cha > 0)
                    {
                        amount += cha;
                    }
                    amount += effectiveStats.FPRegen;

                    if (oPC.Chest.CustomItemType == CustomItemType.ForceArmor)
                    {
                        int clarityLevel = _perk.GetPCPerkLevel(oPC, PerkType.Clarity);
                        if (clarityLevel > 0)
                        {
                            amount += clarityLevel + 1;
                        }
                    }

                    entity = _ability.RestoreFP(oPC, amount, entity);
                }

                entity.CurrentFPTick = rate;
            }
        }

    }
}
