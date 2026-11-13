using System;

namespace SWLOR.Game.Server.Service;

public interface IWeaponDrawRuntime
{
    bool IsValid(uint creature);
    string Replacement(uint creature, string source);
    void Replace(uint creature, string source, string target);
    void SendMappings(uint creature);
    void Schedule(float seconds, Action callback);
}
