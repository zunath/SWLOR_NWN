using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Authorizes initial setup only after the engine accepts a new server character.</summary>
    public static unsafe class PlayerCreation
    {
        private static FunctionHook* _creationHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_creationHook != null) return;
            delegate* unmanaged<void*, void*, void*, uint, uint> validate = &Validate;
            _creationHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN21CServerExoAppInternal29ValidateCreateServerCharacterEP10CNWSPlayerPvj"),
                (IntPtr)validate, HookOrder.Early);
        }

        [UnmanagedCallersOnly]
        private static uint Validate(void* server, void* client, void* data, uint size)
        {
            var original = (delegate* unmanaged<void*, void*, void*, uint, uint>)_creationHook->m_trampoline;
            var result = original(server, client, data, size);
            // Zero is success; other results are the engine's rejection-message strrefs.
            if (result != 0) return result;
            try
            {
                var nativePlayer = CNWSPlayer.FromPointer(client);
                var creature = nativePlayer.GetGameObject()?.AsNWSCreature();
                if (creature == null) throw new InvalidOperationException("Accepted character has no creature object.");
                using var uuid = creature.m_pUUID.GetOrAssignRandom();
                var playerId = uuid.ToString();
                RecordValidatedCreation(result, playerId, () => DB.Get<Player>(playerId), () =>
                {
                    // The engine's first save may predate UUID assignment. Persist that
                    // identity before creating the record used to resume initial setup.
                    if (nativePlayer.SaveServerCharacter() == 0)
                        throw new InvalidOperationException("The new character identity could not be saved.");
                }, player => DB.Set(player));
            }
            catch (Exception exception)
            {
                // The entry guard refuses setup if the durable authorization was not saved.
                Log.WriteError(exception, "Could not record accepted character creation");
            }
            return result;
        }

        internal static void RecordValidatedCreation(uint validationResult, string playerId,
            Func<Player> loadPlayer, Action saveCharacter, Action<Player> savePlayer)
        {
            if (validationResult != 0 || string.IsNullOrWhiteSpace(playerId) || loadPlayer() != null) return;
            saveCharacter();
            // Persist before module entry so reconnects and failed initial exports retain intent.
            savePlayer(new Player(playerId) { CharacterInitializationPending = true });
        }
    }
}
