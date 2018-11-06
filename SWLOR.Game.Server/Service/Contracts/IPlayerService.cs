using System;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPlayerService
    {
        PlayerCharacter GetPlayerEntity(NWPlayer player);
        PlayerCharacter GetPlayerEntity(Guid playerID);
        void InitializePlayer(NWPlayer player);
        void LoadCharacter(NWPlayer player);
        void OnAreaEnter();
        void SaveCharacter(NWPlayer player);
        void SaveLocation(NWPlayer player);
        void ShowMOTD(NWPlayer player);
        void OnModuleUseFeat();
    }
}
