using Nwn.Authoring.Documents.NimGff;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>
    /// The shared door value store bound to SWLOR's door conventions: its self-closing scripts, the
    /// stock death script, and the server's ordered required-key-item locals.
    /// </summary>
    public sealed class DoorValueStore : DoorBehaviorValueStore
    {
        public const string RequiredKeyItemPrefix = "REQUIRED_KEY_ITEM_ID_";
        public const string DefaultCloser = "dt_refermeporte";

        /// <summary>SWLOR's door conventions.</summary>
        public static DoorScriptConventions SwlorConventions { get; } = new(
            DefaultCloser,
            new[]
            {
                DefaultCloser,
                "pug_closedoor8s",
                "gy_2minlockclose",
                "gy_2minclosedoor",
                "relock"
            },
            DoorBehaviorCatalog.DefaultDeathScript,
            RequiredKeyItemPrefix);

        public DoorValueStore(JsonGffStruct door)
            : base(door, SwlorConventions)
        {
        }

        public static bool IsKnownCloser(string? script) => SwlorConventions.IsKnownCloser(script);
    }
}
