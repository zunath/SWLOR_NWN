using System;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>The native operations required for a temporary per-creature animation replacement.</summary>
public interface INamedAnimationRuntime
{
    bool IsValid(uint creature);
    string GetToken(uint creature);
    void SetToken(uint creature, string token);
    string Replacement(uint creature, string source);
    void Replace(uint creature, string source, string replacement);
    ActionType CurrentAction(uint creature);
    void ClearActions(uint creature);
    void Schedule(float seconds, Action callback);
}

