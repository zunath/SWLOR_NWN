using System;
using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

internal static class WeaponAppearanceDraw
{
    private static readonly WeaponDrawPlayback Playback = new(new Runtime());
    internal static void Begin(uint creature) => Playback.Begin(creature);

    [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
    [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
    public static void OnEquipmentChanging() => Playback.Release(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void Prune() => Playback.Prune();

    private sealed class Runtime : IWeaponDrawRuntime
    {
        public bool IsValid(uint creature) => GetIsObjectValid(creature);
        public string Replacement(uint creature, string source)
        {
            var obj = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature)?.AsNWSObject();
            return obj == null ? "" : WeaponAttackAnimation.ReadAnimationReplacements(obj).GetValueOrDefault(source, "");
        }
        public void Replace(uint creature, string source, string target) => ReplaceObjectAnimation(creature, source, target);
        public void Schedule(float seconds, Action callback) =>
            Scheduler.Schedule(() => ServerManager.Executor.ExecuteInScriptContext(callback), TimeSpan.FromSeconds(seconds));
        public unsafe void SendMappings(uint creature)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var obj = server.GetGameObject(creature)?.AsNWSObject();
            if (obj == null) return;
            var message = server.GetNWSMessage();
            foreach (var player in server.GetPlayerList())
            {
                var last = player.GetLastUpdateObject(creature);
                if (last == null) continue;
                message.CreateWriteMessage(2048, player.m_nPlayerID, 1);
                message.WriteGameObjUpdate_UpdateObject(player, obj, last, 0x1000000, 0);
                byte* data = null;
                uint size = 0;
                if (message.GetWriteMessage(&data, &size) != 0 && size > 0)
                    message.SendServerToPlayerMessage(player.m_nPlayerID,
                        (byte)MessageMajor.GameObjectUpdate, (byte)MessageGameObjectUpdateMinor.ObjectList, data, size);
            }
        }
    }
}
