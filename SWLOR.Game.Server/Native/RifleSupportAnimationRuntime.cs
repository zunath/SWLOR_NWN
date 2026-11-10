using System;
using System.Collections.Generic;
using System.Linq;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Native;

internal sealed class RifleSupportAnimationRuntime : IRifleSupportAnimationRuntime
{
    public bool IsValid(uint creature) => GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;
    public bool IsTemporaryPlaybackActive(uint creature) => NamedAnimation.IsPlaybackActive(creature);
    public string Replacement(uint creature, string source)
    {
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature)?.AsNWSObject();
        return native == null ? "" : WeaponAttackAnimation.ReadAnimationReplacements(native).GetValueOrDefault(source, "");
    }
    public void Replace(uint creature, string source, string replacement) => ReplaceObjectAnimation(creature, source, replacement);
    public void ClearHold(uint creature) => RifleSupportHoldClear.Begin(creature,
        Replacement(creature, NamedAnimationPlayback.ShoulderHoldSource));
    public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
}
