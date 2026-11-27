namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The names of the fields the server writes into a saved player file. <c>PlayerFileCheckpoint</c> (the hook that
    /// writes them) and <c>CharacterFileUpgrade</c> (which reads the version back) both name them; they live here so
    /// Foundation does not read a constant of a hook class (Core is below both). <c>PlayerFileCheckpoint</c> keeps a same-named constant for
    /// each, which master's code still reads.
    /// </summary>
    internal static class PlayerFileFieldNames
    {
        internal const string FileVersion = "PlayerFileVer";
        internal const string CraftSequence = "CraftFileSeq";
    }
}
