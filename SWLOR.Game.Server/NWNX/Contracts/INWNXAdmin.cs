using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXAdmin
    {
        void AddBannedCDKey(string key);
        void AddBannedIP(string ip);
        void AddBannedPlayerName(string playername);
        void ClearPlayerPassword();
        void DeletePlayerCharacter(NWPlayer pc, int bPreserveBackup);
        string GetBannedList();
        string GetDMPassword();
        string GetPlayerPassword();
        void RemoveBannedCDKey(string key);
        void RemoveBannedIP(string ip);
        void RemoveBannedPlayerName(string playername);
        void SetDMPassword(string password);
        void SetModuleName(string name);
        void SetPlayerPassword(string password);
        void SetServerName(string name);
        void ShutdownServer();
    }
}