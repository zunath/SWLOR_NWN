using System;

namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>Native operations used by the persistent rifle animation owner.</summary>
public interface IRifleSupportAnimationRuntime
{
    bool IsValid(uint creature);
    bool IsTemporaryPlaybackActive(uint creature);
    string Replacement(uint creature, string source);
    void Replace(uint creature, string source, string replacement);
    void ClearHold(uint creature);
    void Schedule(float seconds, Action callback);
}
