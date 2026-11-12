using System.Text;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Hands state that no adoption step can read back from the world (the spawn registry, the weather front, the property
    /// loader's bookkeeping) from the build a hot reload retires to the build it activates, through a local string on the
    /// module: the old build's carry step puts it there while it quiesces, the new build's adoption step takes it (and deletes
    /// it). No frame runs in between (the host holds the engine thread from quiescence to activation), and nothing of it is
    /// persistent: a restart starts without it. The text is stored as base64 of its UTF-8 bytes, so the engine's string
    /// encoding cannot change it.
    /// </summary>
    public static class ReloadCarry
    {
        /// <summary>The prefix of the module's local strings that hold carried state.</summary>
        public const string VariablePrefix = "SWLOR_RELOAD_CARRY_";

        /// <summary>Leaves <paramref name="text"/> on the module under <paramref name="key"/>, replacing what was there.</summary>
        public static void Put(string key, string text)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A carry needs a key.", nameof(key));

            SetLocalString(GetModule(), VariablePrefix + key, Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty)));
        }

        /// <summary>
        /// The text the old build left under <paramref name="key"/>, which this removes from the module, or null when there is
        /// none (the old build did not carry it).
        /// </summary>
        public static string Take(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A carry needs a key.", nameof(key));

            var module = GetModule();
            var stored = GetLocalString(module, VariablePrefix + key);
            DeleteLocalString(module, VariablePrefix + key);
            return string.IsNullOrEmpty(stored) ? null : Encoding.UTF8.GetString(Convert.FromBase64String(stored));
        }
    }
}
